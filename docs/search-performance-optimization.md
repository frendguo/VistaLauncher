# 搜索性能优化方案

## 背景

当工具数量超过 700 个时，搜索功能出现明显卡顿。本文档分析了性能瓶颈并提出优化方案。

## 性能瓶颈分析

### 1. 图标无缓存，每次搜索都重新提取

**位置**: `ToolItemViewModel.cs:95-111`, `IconExtractor.cs`

```csharp
public ToolItemViewModel(ToolItem toolItem, int index)
{
    ...
    _ = LoadIconAsync();  // 每次创建都加载图标
}
```

**问题**:
- 因为 ViewModel 每次都重新创建，图标也每次都重新从文件系统提取
- `IconExtractor` 没有任何缓存机制
- 700 个工具 = 700 次 Win32 API 调用 (`SHGetFileInfo`) + 700 次位图转换
- **这是最严重的性能问题**

### 2. ViewModel 每次都重建

**位置**: `LauncherViewModel.cs:137-150`

```csharp
private void UpdateFilteredTools(IEnumerable<ToolItem> tools)
{
    var viewModels = tools
        .Select((tool, index) => new ToolItemViewModel(tool, index))
        .ToList();

    FilteredTools = new ObservableCollection<ToolItemViewModel>(viewModels);
}
```

**问题**:
- 每次输入字符，都会为所有匹配结果创建全新的 `ToolItemViewModel` 对象
- 700 个工具 = 700 次对象分配 + GC 压力

### 3. 搜索算法中的字符串操作冗余

**位置**: `TextMatchSearchProvider.cs:39-72`

```csharp
var sortedResults = results.OrderByDescending(tool =>
{
    var lowerName = tool.Name.ToLower();
    if (lowerName == query.ToLower())  // 每次比较都调用
    ...
});
```

**问题**:
- `query.ToLower()` 在排序过程中被调用 N × log(N) 次
- 对于 700 个工具，`ToLower()` 可能被调用数千次

### 4. 排序分数重复计算

`OrderByDescending` 的 lambda 在每次比较时都会重新计算分数，导致分数计算复杂度从 O(n) 变成 O(n log n)。

### 5. ObservableCollection 整体替换触发 UI 重绘

每次搜索都创建新的 `ObservableCollection`，导致 ListView 认为整个数据源变了。

## 问题严重程度排序

| 优先级 | 问题 | 影响程度 |
|--------|------|----------|
| 🔴 高 | 图标无缓存，每次搜索都重新提取 | 700 次文件 I/O + Win32 调用 |
| 🔴 高 | ViewModel 每次都重建 | 700 次对象分配 + GC |
| 🟡 中 | 字符串 ToLower() 重复调用 | 数千次字符串分配 |
| 🟡 中 | 排序分数重复计算 | O(n log n) 次计算 |
| 🟢 低 | ObservableCollection 整体替换 | UI 重绘开销 |

---

## 优化方案一：图标缓存

### 1.1 缓存层级选择

| 层级 | 优点 | 缺点 |
|------|------|------|
| **IconExtractor 服务层** ✅ | 单一职责；所有调用者都受益；易于管理 | 当前是 static class，需要考虑线程安全 |
| ToolItemViewModel 层 | 改动最小 | ViewModel 每次重建，缓存必须用 static；职责混乱 |
| 独立的 IconCacheService | 职责清晰；可 DI 注入 | 多一个服务类 |

**建议**: 在 **IconExtractor 服务层** 实现缓存。

理由:
1. **就近原则**: 缓存逻辑紧贴 I/O 操作
2. **透明性**: 调用方不需要知道缓存的存在
3. **复用性**: 未来其他页面需要图标都能自动受益
4. **改动最小**: 不需要改 ViewModel 和 DI 结构

### 1.2 缓存 Key 设计

**简单方案**（不推荐）:
```csharp
Dictionary<string, BitmapImage> _cache;
// key = filePath
```
问题：文件更新后图标不会刷新。

**推荐方案**:
```csharp
// key = normalizedPath|lastWriteTimeTicks|useLargeIcon
var key = $"{Path.GetFullPath(filePath).ToLowerInvariant()}|{File.GetLastWriteTimeUtc(filePath).Ticks}|{useLargeIcon}";
```

设计优点:
- **路径规范化**: 不同写法的路径会被识别为同一文件
- **大小写不敏感**: Windows 路径不区分大小写
- **自动失效**: 文件被替换后 LastWriteTime 变化，自动成为新的 key
- **区分图标尺寸**: 大图标和小图标分开缓存

### 1.3 缓存失效策略

| 场景 | 触发频率 | 处理方式 |
|------|----------|----------|
| 应用程序更新（exe 被替换） | 低 | LastWriteTime 变化，自动失效 |
| 用户手动更换图标 | 极低 | 同上 |
| 应用启动 | 每次 | 可选：清空缓存重新加载 |
| 用户点击刷新按钮 | 按需 | 提供 `ClearCache()` 方法 |

### 1.4 内存占用估算

**单个图标**:
```
BitmapImage 对象本身:          ~200 bytes
像素数据 (32×32, BGRA):        32 × 32 × 4 = 4,096 bytes
WinUI 渲染缓冲区:              ~4KB
────────────────────────────────────────────
单个图标总计:                   约 8-10 KB
```

**700 个工具**: 700 × 10KB = 7 MB（完全可接受）

### 1.5 是否需要 LRU

| 工具数量 | 预估内存 | 建议 |
|----------|----------|------|
| < 1000 | < 10 MB | 不需要 LRU，全量缓存 |
| 1000-3000 | 10-30 MB | 可选 LRU，设置 2000 上限 |
| > 3000 | > 30 MB | 建议 LRU 或弱引用 |

**建议**: 对于 700 个工具，不需要 LRU。设置 2000 条硬上限即可。

### 1.6 实现要点

```csharp
public static class IconExtractor
{
    private static readonly ConcurrentDictionary<string, BitmapImage?> _cache = new();
    private const int MaxCacheSize = 2000;

    public static async Task<BitmapImage?> ExtractIconAsync(string filePath, bool useLargeIcon = true)
    {
        var key = GetCacheKey(filePath, useLargeIcon);

        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var icon = await Task.Run(() => ExtractIconCore(filePath, useLargeIcon));

        // 简单的容量控制
        if (_cache.Count >= MaxCacheSize)
        {
            _cache.Clear();
        }

        _cache.TryAdd(key, icon);
        return icon;
    }

    private static string GetCacheKey(string filePath, bool useLargeIcon)
    {
        var normalizedPath = Path.GetFullPath(filePath).ToLowerInvariant();
        var lastWrite = File.GetLastWriteTimeUtc(filePath).Ticks;
        return $"{normalizedPath}|{lastWrite}|{useLargeIcon}";
    }

    public static void ClearCache() => _cache.Clear();
}
```

---

## 优化方案二：ViewModel 复用

### 2.1 实现方案选择

| 方案 | 适用场景 | 复杂度 | 内存效率 |
|------|----------|--------|----------|
| **字典缓存** ✅ | 工具列表相对固定 | 低 | 中（缓存所有） |
| 对象池 | 工具列表频繁变化 | 高 | 高（按需分配） |

**建议**: VistaLauncher 的工具列表相对稳定，用**字典缓存方案**。

### 2.2 字典缓存实现

```
┌─────────────────────────────────────────────────────────┐
│  Dictionary<string, ToolItemViewModel>                  │
│  ┌─────────┬─────────────────────────────────────────┐  │
│  │ Key     │ Value                                   │  │
│  ├─────────┼─────────────────────────────────────────┤  │
│  │ tool-1  │ ToolItemViewModel { Name="Notepad" }    │  │
│  │ tool-2  │ ToolItemViewModel { Name="CMD" }        │  │
│  │ ...     │ ...                                     │  │
│  └─────────┴─────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
          搜索结果: [tool-2, tool-1]
                           │
                           ▼
         从缓存中取出对应的 ViewModel，更新 Index
```

**核心代码**:
```csharp
private readonly Dictionary<string, ToolItemViewModel> _vmCache = new();

private void UpdateFilteredTools(IEnumerable<ToolItem> tools)
{
    var list = new List<ToolItemViewModel>();
    int index = 0;

    foreach (var tool in tools)
    {
        if (!_vmCache.TryGetValue(tool.Id, out var vm))
        {
            vm = new ToolItemViewModel(tool);
            _vmCache[tool.Id] = vm;
        }
        vm.UpdateIndex(index++);
        list.Add(vm);
    }

    FilteredTools = new ObservableCollection<ToolItemViewModel>(list);
}
```

### 2.3 池规模估算

每个 ViewModel 大约占用:
```
对象头:                    24 bytes
字段 (引用+值类型):         ~100 bytes
BitmapImage 引用:          8 bytes
────────────────────────────────────
总计:                      ~150 bytes × 700 = 105 KB
```

**建议**: 不需要限制池大小，全量缓存。

### 2.4 复用 ViewModel 的坑

#### 坑 1: 状态污染

当前 `ToolItemViewModel` 有以下可变状态:
```csharp
[ObservableProperty] private BitmapImage? _iconImage;
[ObservableProperty] private bool _isLoadingIcon = true;
[ObservableProperty] private bool _isSelected;
```

**解决方案**: 复用时调用 Reset 方法

```csharp
public void Reset(int newIndex)
{
    _index = newIndex;
    IsSelected = false;

    OnPropertyChanged(nameof(Index));
    OnPropertyChanged(nameof(ShortcutKeyDisplay));
    OnPropertyChanged(nameof(ShowShortcutKey));
}
```

#### 坑 2: Index 是只读的

当前实现:
```csharp
public int Index { get; }  // 构造函数中设置，之后不可变
```

**解决方案**: 改为可变属性

```csharp
public int Index { get; private set; }

public void UpdateIndex(int newIndex)
{
    if (Index != newIndex)
    {
        Index = newIndex;
        OnPropertyChanged(nameof(Index));
        OnPropertyChanged(nameof(ShortcutKeyDisplay));
        OnPropertyChanged(nameof(ShowShortcutKey));
    }
}
```

#### 坑 3: 异步任务竞态

**问题场景**:
1. 复用 VM，开始加载图标 A
2. 快速切换，再次复用，开始加载图标 B
3. 图标 A 加载完成，覆盖了图标 B

**解决方案**: 使用 CancellationToken

```csharp
private CancellationTokenSource? _iconLoadCts;

public async Task LoadIconAsync()
{
    _iconLoadCts?.Cancel();
    _iconLoadCts = new CancellationTokenSource();
    var token = _iconLoadCts.Token;

    try
    {
        var icon = await IconExtractor.ExtractIconAsync(_toolItem.ExecutablePath);
        if (!token.IsCancellationRequested)
        {
            IconImage = icon;
        }
    }
    catch (OperationCanceledException) { }
}
```

---

## 优化方案三：ObservableCollection 增量更新

### 3.1 方案选择

| 方案 | 复杂度 | 适用场景 | UI 性能 |
|------|--------|----------|---------|
| **AdvancedCollectionView** ✅ | 低 | 过滤/排序场景 | 最佳 |
| 差异更新 | 中 | 列表变化较小 | 好 |
| DiffUtil | 高 | 复杂列表动画 | 好 |
| 直接替换 | 最低 | 简单场景 | 一般 |

**建议**: 使用 **AdvancedCollectionView**（CommunityToolkit.WinUI 已引用）。

### 3.2 AdvancedCollectionView 方案

```
┌─────────────────────────────────────────────────────────┐
│  _allToolViewModels (ObservableCollection)              │
│  [VM1] [VM2] [VM3] [VM4] [VM5] [VM6] [VM7] ...         │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
              ┌────────────────────────┐
              │  AdvancedCollectionView │
              │  Filter = (item) =>     │
              │    MatchesQuery(item)   │
              └────────────────────────┘
                           │
                           ▼
              只显示匹配的项，底层集合不变
```

**核心思路**:
- 维护一个包含所有工具的 `ObservableCollection`
- 使用 `AdvancedCollectionView` 包装，设置 `Filter` 属性
- 搜索时只更新 Filter，不替换集合

**优点**:
- 底层集合不变，ViewModel 天然复用
- 支持排序、分组
- ListView 的虚拟化能更好地工作

---

## 优化方案四：搜索算法优化

### 4.1 预计算小写字符串

```csharp
public Task<IEnumerable<ToolItem>> SearchAsync(...)
{
    var lowerQuery = query.ToLower();  // 只计算一次
    var queryTokens = lowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // ... 后续使用 lowerQuery 和 queryTokens
}
```

### 4.2 先计算分数再排序

```csharp
var scoredResults = results
    .Select(tool => (tool, score: CalculateScore(tool, queryTokens, lowerQuery)))
    .OrderByDescending(x => x.score)
    .Select(x => x.tool);
```

### 4.3 考虑建立索引

对于更大规模的数据，可以考虑:
- 倒排索引
- Trie 树（前缀匹配）
- 使用 SQLite FTS5 全文搜索

---

## 实施优先级

| 优先级 | 优化项 | 预期收益 | 实现复杂度 |
|--------|--------|----------|------------|
| 1 | 图标缓存 | 消除 700 次文件 I/O | 低 |
| 2 | ViewModel 字典缓存 | 消除 700 次对象创建 | 低 |
| 3 | 搜索算法字符串优化 | 减少数千次字符串分配 | 低 |
| 4 | AdvancedCollectionView | 更好的 UI 虚拟化 | 中 |

建议按此顺序逐步实施，每完成一项后进行性能测试验证效果。
