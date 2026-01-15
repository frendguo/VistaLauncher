# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

VistaLauncher 是一个使用 WinUI 3 (Windows App SDK) 开发的 Windows 应用启动器，类似于 Alfred 或 Wox。它提供了一个快速启动工具和应用程序的界面，支持全局热键唤醒、搜索过滤和键盘导航。

**目标平台**: .NET 10.0 (net10.0-windows10.0.19041.0)，支持 x86、x64 和 ARM64 平台

## 常用命令

### 构建和运行

```powershell
# 进入解决方案目录
cd src/VistaLauncher

# 构建项目
dotnet build VistaLauncher.slnx

# 构建指定平台 (x64/x86/ARM64)
dotnet build VistaLauncher.slnx -p:Platform=x64

# 发布应用 (Release 配置会自动启用 ReadyToRun 和 Trimming)
dotnet publish VistaLauncher/VistaLauncher.csproj -c Release -p:Platform=x64

# 在 Visual Studio 中运行
# 打开 src/VistaLauncher/VistaLauncher.slnx
# 选择目标平台 (x64/x86/ARM64)
# 按 F5 运行或 Ctrl+F5 无调试运行
```

### 清理和重新构建

```powershell
# 在 src/VistaLauncher 目录下执行
dotnet clean VistaLauncher.slnx

# 重新构建
dotnet build VistaLauncher.slnx --no-incremental
```

## 架构概览

### 核心架构模式

- **MVVM 模式**: 使用 CommunityToolkit.Mvvm 实现 MVVM 架构
  - ViewModels: `LauncherViewModel`, `ToolItemViewModel`
  - Views: `MainWindow.xaml` (主启动器窗口)
  - Models: `ToolItem`, `ToolGroup`, `ToolsData`

- **MVVM 源生成器**:
  - 使用 `[ObservableProperty]` 和 `[RelayCommand]` 属性
  - 编译时自动生成属性和命令代码，避免反射

- **依赖注入**: 手动依赖注入（未使用 DI 容器）
  - Services 在 `MainWindow.xaml.cs:100-115` 中实例化并注入到 ViewModel

- **服务层设计**:
  - `IToolDataService` / `ToolDataService`: 工具数据的加载、保存、CRUD 操作
  - `ISearchProvider` / `TextMatchSearchProvider`: 搜索和过滤工具列表
  - `IProcessLauncher` / `ProcessLauncher`: 启动外部进程
  - `IHotkeyService` / `HotkeyService`: 全局热键注册 (Ctrl+F2)，需先调用 `Initialize(Window)` 关联窗口
  - `IconExtractor`: 使用 System.Drawing.Common 从可执行文件提取图标
  - `ConfigService`: 应用配置管理（预留）

### 数据存储

- **位置**: `%AppData%\Roaming\VistaLauncher\tools.json`
- **格式**: JSON，使用 System.Text.Json 和 Source Generator (JsonContext)
- **结构**: `ToolsData` 包含 `Groups` (工具分组) 和 `Tools` (工具列表)
- **默认数据**: 首次运行时创建，包含记事本和命令提示符示例 (见 `ToolDataService.cs:176-222`)

### 窗口管理

- **无边框窗口**: 使用 Win32 API 移除标题栏和边框 (`MainWindow.xaml.cs:115-135`)
- **圆角效果**: Windows 11 上使用 DWM API 设置窗口圆角 (`MainWindow.xaml.cs:137-142`)
- **自适应高度**: 根据搜索结果动态调整窗口高度 (`MainWindow.xaml.cs:191-209`)
  - 折叠高度: 100px (TopBarHeight 56px + CommandBarHeight 44px)
  - 展开高度: 100px + 列表项数量 * 44px (最多显示 9 项)
  - 固定宽度: 680px

### 热键系统

- **全局热键**: Ctrl+F2 (可在 `MainWindow.xaml.cs:150-152` 修改)
- **实现方式**:
  - 使用 Win32 `RegisterHotKey` API
  - 通过窗口子类化 (Subclassing) 接收 `WM_HOTKEY` 消息
  - `HotkeyService` 需先调用 `Initialize(Window)` 关联窗口
  - 见 `HotkeyService.cs:102-124`

### 搜索和导航

- **搜索延迟**: 100ms 防抖 (`LauncherViewModel.cs:131`)
- **取消机制**: 使用 `CancellationTokenSource` 取消之前的搜索
- **键盘导航**:
  - `Down`: 选择下一个
  - `Up`: 选择上一个
  - `Enter`: 启动选中的工具
  - `Escape`: 收起列表或隐藏窗口
  - `Tab`: 展开列表
  - `Ctrl+1` ~ `Ctrl+9`: 快速启动前 9 个工具

## 目录结构

```
src/VistaLauncher/VistaLauncher/
├── App.xaml / App.xaml.cs          # 应用程序入口
├── MainWindow.xaml / MainWindow.xaml.cs  # 主窗口 UI 和交互逻辑
├── Models/                         # 数据模型
│   ├── ToolItem.cs                 # 工具项
│   ├── ToolGroup.cs                # 工具分组
│   ├── ToolsData.cs                # 数据根对象
│   ├── AppConfig.cs                # 应用配置
│   ├── Enums.cs                    # 枚举定义
│   └── JsonContext.cs              # JSON 序列化上下文
├── Services/                       # 服务层
│   ├── IToolDataService.cs / ToolDataService.cs
│   ├── ISearchProvider.cs / TextMatchSearchProvider.cs
│   ├── ProcessLauncher.cs
│   ├── HotkeyService.cs
│   ├── IconExtractor.cs
│   └── ConfigService.cs
├── ViewModels/                     # 视图模型
│   ├── LauncherViewModel.cs        # 主启动器 ViewModel
│   └── ToolItemViewModel.cs        # 工具项 ViewModel (包装 ToolItem)
└── Styles/                         # 样式资源
    └── LauncherTheme.xaml          # 启动器主题样式
```

## 重要技术细节

### Win32 互操作

项目大量使用 Win32 API 进行窗口操作:
- `user32.dll`: 窗口样式、热键、窗口过程
- `dwmapi.dll`: 桌面窗口管理器 (圆角效果)
- 使用 `[DllImport]` 和 `[LibraryImport]` 导入 API
- `AllowUnsafeBlocks` 已启用以支持指针操作

### WinUI 3 特性

- 使用 Microsoft.WindowsAppSDK 1.6.250108002
- 启用 MSIX 打包 (`EnableMsixTooling`)
- 资源字典在 `Styles/LauncherTheme.xaml` 中定义
- 使用 `x:Bind` 进行数据绑定
- 使用 `CommunityToolkit.WinUI.Animations`、`Controls.Primitives`、`Converters` 扩展控件

### JSON 序列化优化

- 使用 Source Generator (`JsonContext.cs`) 提高性能
- 配置在 `JsonContext.cs:6-13`
- 避免反射，提升启动速度

## 注意事项

- **平台特定**: 仅支持 Windows 10 1809 (Build 17763) 及以上版本
- **C# 12**: 使用了 C# 12 的集合表达式 `[]` 语法
- **Nullable**: 已启用 Nullable 引用类型 (`<Nullable>enable</Nullable>`)
- **ImplicitUsings**: 已启用，常见命名空间自动导入
- **发布优化**: Release 模式自动启用 ReadyToRun 和 Trimming (见 `VistaLauncher.csproj:60-65`)

## 开发工作流建议

1. **修改 UI**: 编辑 `MainWindow.xaml` 或 `Styles/LauncherTheme.xaml`
2. **修改业务逻辑**: 在 `LauncherViewModel.cs` 中添加命令或属性
3. **添加新服务**: 在 `Services/` 下创建接口和实现，然后在 `MainWindow.xaml.cs` 中注入
4. **修改数据模型**: 编辑 `Models/` 中的类，更新 `JsonContext.cs` 如有必要
5. **调试热键**: 在 `HotkeyService.cs` 和 `MainWindow.xaml.cs:144-158` 中修改
6. **调整窗口尺寸**: 修改 `MainWindow.xaml.cs:26-31` 中的常量
