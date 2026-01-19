using VistaLauncher.Models;

namespace VistaLauncher.Services;

public class ImportService : IImportService
{
    private readonly IToolDataService _toolDataService;

    public ImportService(IToolDataService toolDataService)
    {
        _toolDataService = toolDataService;
    }

    public event EventHandler<ImportProgressEventArgs>? ProgressChanged;

    public bool ValidateNirLauncherDirectory(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return false;
        }

        // 查找 .nlp 文件
        var nlpFile = FindNlpFile(path);
        return nlpFile != null;
    }

    public async Task<ImportResult> ImportFromNirLauncherAsync(string path, CancellationToken cancellationToken)
    {
        var result = new ImportResult();

        try
        {
            // 1. 查找 nlp 文件
            var nlpFile = FindNlpFile(path);
            if (nlpFile == null)
            {
                result.Errors.Add("未找到 NirLauncher 配置文件 (.nlp)");
                return result;
            }

            var nlpDirectory = Path.GetDirectoryName(nlpFile)!;

            // 2. 解析 INI 文件
            ReportProgress(0, 1, "正在解析配置文件...", "");
            var ini = await IniParser.ParseAsync(nlpFile, cancellationToken);

            var groupCount = ini.GetIntValue("General", "GroupCount");
            var softwareCount = ini.GetIntValue("General", "SoftwareCount");

            if (groupCount == 0 || softwareCount == 0)
            {
                result.Errors.Add("配置文件格式无效或为空");
                return result;
            }

            // 3. 获取现有数据用于去重
            var existingTools = (await _toolDataService.GetToolsAsync()).ToList();
            var existingPaths = existingTools
                .Select(t => NormalizePath(t.ExecutablePath))
                .Where(p => !string.IsNullOrEmpty(p))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingGroups = (await _toolDataService.GetGroupsAsync()).ToList();

            // 4. 导入分组（建立索引映射）
            var groupMap = new Dictionary<int, string>(); // oldIndex → newGuid

            for (int i = 0; i < groupCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var section = $"Group{i}";
                var name = ini.GetValueOrDefault(section, "Name");
                var showAll = ini.GetValue(section, "ShowAll");

                // 跳过 "All Utilities" 分组
                if (showAll == "1" || string.IsNullOrEmpty(name))
                {
                    continue;
                }

                // 检查分组是否已存在
                var existingGroup = existingGroups.FirstOrDefault(g =>
                    string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));

                if (existingGroup != null)
                {
                    // 使用已存在的分组
                    groupMap[i] = existingGroup.Id;
                }
                else
                {
                    // 创建新分组
                    var group = new ToolGroup
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = name,
                        Icon = GetGroupIcon(name)
                    };

                    await _toolDataService.AddGroupAsync(group);
                    groupMap[i] = group.Id;
                    existingGroups.Add(group);
                }
            }

            // 5. 导入工具
            for (int i = 0; i < softwareCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var section = $"Software{i}";
                var appName = ini.GetValueOrDefault(section, "AppName");

                ReportProgress(i + 1, softwareCount, "正在导入工具...", appName);

                var exe = ini.GetValueOrDefault(section, "exe");
                var exe64 = ini.GetValue(section, "exe64");
                var groupIndex = ini.GetIntValue(section, "group", -1);

                // 跳过无效的工具或未映射分组的工具
                if (string.IsNullOrEmpty(exe) || string.IsNullOrEmpty(appName))
                {
                    result.SkippedTools++;
                    continue;
                }

                // 跳过 "All Utilities" 分组的工具
                if (!groupMap.TryGetValue(groupIndex, out var groupId))
                {
                    result.SkippedTools++;
                    continue;
                }

                // 解析可执行文件路径
                var executablePath = ResolveExePath(nlpDirectory, exe, exe64);

                // 去重检查
                var normalizedPath = NormalizePath(executablePath);
                if (existingPaths.Contains(normalizedPath))
                {
                    result.SkippedTools++;
                    continue;
                }

                // 判断工具类型
                var groupName = ini.GetValueOrDefault($"Group{groupIndex}", "Name");
                var toolType = DetermineToolType(groupName);

                // 判断架构
                var architecture = DetermineArchitecture(exe64);

                // 创建工具项
                var tool = new ToolItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = appName,
                    ShortDescription = ini.GetValueOrDefault(section, "ShortDesc"),
                    LongDescription = ini.GetValueOrDefault(section, "LongDesc"),
                    ExecutablePath = executablePath,
                    WorkingDirectory = Path.GetDirectoryName(executablePath) ?? nlpDirectory,
                    HomepageUrl = ini.GetValueOrDefault(section, "url"),
                    HelpUrl = ResolveHelpPath(nlpDirectory, ini.GetValue(section, "help")),
                    GroupId = groupId,
                    Type = toolType,
                    Architecture = architecture,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _toolDataService.AddToolAsync(tool);
                existingPaths.Add(normalizedPath);
                result.ImportedTools++;
            }

            // 6. 保存数据
            ReportProgress(softwareCount, softwareCount, "正在保存...", "");
            await _toolDataService.SaveAsync();
        }
        catch (OperationCanceledException)
        {
            result.Errors.Add("导入已取消");
            throw;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"导入失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 查找 NirLauncher 配置文件
    /// </summary>
    private static string? FindNlpFile(string basePath)
    {
        // 直接在目录下查找
        var nlpFiles = Directory.GetFiles(basePath, "*.nlp", SearchOption.TopDirectoryOnly);
        if (nlpFiles.Length > 0)
        {
            return nlpFiles[0];
        }

        // 在子目录中查找（如 NirSoft/）
        var subDirs = Directory.GetDirectories(basePath);
        foreach (var subDir in subDirs)
        {
            nlpFiles = Directory.GetFiles(subDir, "*.nlp", SearchOption.TopDirectoryOnly);
            if (nlpFiles.Length > 0)
            {
                return nlpFiles[0];
            }
        }

        return null;
    }

    /// <summary>
    /// 解析可执行文件路径
    /// </summary>
    private static string ResolveExePath(string nlpDirectory, string exe, string? exe64)
    {
        // 64位系统优先使用 exe64
        if (Environment.Is64BitOperatingSystem && !string.IsNullOrEmpty(exe64))
        {
            var path64 = Path.GetFullPath(Path.Combine(nlpDirectory, exe64));
            if (File.Exists(path64))
            {
                return path64;
            }
        }

        // 回退到 exe
        return Path.GetFullPath(Path.Combine(nlpDirectory, exe));
    }

    /// <summary>
    /// 解析帮助文件路径
    /// </summary>
    private static string ResolveHelpPath(string nlpDirectory, string? help)
    {
        if (string.IsNullOrEmpty(help))
        {
            return string.Empty;
        }

        // 如果是 URL 则直接返回
        if (help.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            help.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return help;
        }

        // 转为绝对路径
        return Path.GetFullPath(Path.Combine(nlpDirectory, help));
    }

    /// <summary>
    /// 标准化路径用于比较
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path;
        }
    }

    /// <summary>
    /// 根据分组名称判断工具类型
    /// </summary>
    private static ToolType DetermineToolType(string groupName)
    {
        if (groupName.Contains("Command-Line", StringComparison.OrdinalIgnoreCase) ||
            groupName.Contains("Console", StringComparison.OrdinalIgnoreCase))
        {
            return ToolType.Console;
        }

        return ToolType.GUI;
    }

    /// <summary>
    /// 判断架构
    /// </summary>
    private static Architecture DetermineArchitecture(string? exe64)
    {
        return string.IsNullOrEmpty(exe64) ? Architecture.x86 : Architecture.x64;
    }

    /// <summary>
    /// 根据分组名称获取图标
    /// </summary>
    private static string GetGroupIcon(string groupName)
    {
        return groupName.ToLowerInvariant() switch
        {
            var n when n.Contains("password") => "\uE8D7",      // 密码图标
            var n when n.Contains("network") => "\uE968",       // 网络图标
            var n when n.Contains("browser") || n.Contains("web") => "\uE774", // 浏览器图标
            var n when n.Contains("video") || n.Contains("audio") => "\uE714", // 媒体图标
            var n when n.Contains("internet") => "\uE909",      // 互联网图标
            var n when n.Contains("command") || n.Contains("console") => "\uE756", // 命令行图标
            var n when n.Contains("desktop") => "\uE7F4",       // 桌面图标
            var n when n.Contains("outlook") || n.Contains("office") => "\uE8A8", // Office 图标
            var n when n.Contains("programmer") || n.Contains("developer") => "\uE943", // 开发者图标
            var n when n.Contains("disk") => "\uEDA2",          // 磁盘图标
            var n when n.Contains("system") => "\uE770",        // 系统图标
            _ => "\uE74C"                                        // 默认图标
        };
    }

    /// <summary>
    /// 报告进度
    /// </summary>
    private void ReportProgress(double current, double total, string status, string currentItem)
    {
        ProgressChanged?.Invoke(this, new ImportProgressEventArgs
        {
            Current = current,
            Total = total,
            Status = status,
            CurrentItem = currentItem
        });
    }
}
