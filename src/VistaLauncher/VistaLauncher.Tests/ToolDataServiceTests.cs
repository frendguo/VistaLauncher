using System.Text.Json;
using System.Text.Json.Serialization;
using VistaLauncher.Models;
using VistaLauncher.Services;
using Xunit;

namespace VistaLauncher.Tests;

/// <summary>
/// ToolDataService 双配置源机制测试
/// </summary>
public class ToolDataServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _defaultConfigPath;
    private readonly string _userDataDirectory;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ToolDataServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"VistaLauncher_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _defaultConfigPath = Path.Combine(_testDirectory, "default", "tools.json");
        _userDataDirectory = Path.Combine(_testDirectory, "user");

        Directory.CreateDirectory(Path.GetDirectoryName(_defaultConfigPath)!);
        Directory.CreateDirectory(_userDataDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private ToolDataService CreateService()
    {
        return new ToolDataService(_defaultConfigPath, _userDataDirectory);
    }

    private async Task WriteConfigAsync(string path, ToolsData data)
    {
        var directory = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private ToolsData CreateDefaultConfig()
    {
        return new ToolsData
        {
            Version = "1.0.0",
            Groups =
            [
                new ToolGroup { Id = "default-group", Name = "默认分组", Icon = "📦" }
            ],
            Tools =
            [
                new ToolItem
                {
                    Id = "default-tool-1",
                    Name = "默认工具1",
                    ShortDescription = "默认工具1描述",
                    ExecutablePath = @"C:\Windows\System32\notepad.exe"
                },
                new ToolItem
                {
                    Id = "default-tool-2",
                    Name = "默认工具2",
                    ShortDescription = "默认工具2描述",
                    ExecutablePath = @"C:\Windows\System32\cmd.exe"
                }
            ]
        };
    }

    private ToolsData CreateUserConfig()
    {
        return new ToolsData
        {
            Version = "1.0.0",
            Groups =
            [
                new ToolGroup { Id = "user-group", Name = "用户分组", Icon = "⭐" }
            ],
            Tools =
            [
                new ToolItem
                {
                    Id = "user-tool-1",
                    Name = "用户工具1",
                    ShortDescription = "用户工具1描述",
                    ExecutablePath = @"C:\Program Files\MyTool\tool.exe"
                }
            ]
        };
    }

    #region 配置加载测试

    [Fact]
    public async Task LoadAsync_OnlyUserConfig_LoadsCorrectly()
    {
        // Arrange: 只有用户配置（向后兼容）
        var userConfig = CreateUserConfig();
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        var service = CreateService();

        // Act
        var tools = (await service.GetToolsAsync()).ToList();
        var groups = (await service.GetGroupsAsync()).ToList();

        // Assert
        Assert.Single(tools);
        Assert.Equal("user-tool-1", tools[0].Id);
        Assert.Equal(ToolSource.User, tools[0].Source);

        Assert.Single(groups);
        Assert.Equal("user-group", groups[0].Id);
    }

    [Fact]
    public async Task LoadAsync_OnlyDefaultConfig_LoadsCorrectly()
    {
        // Arrange: 只有默认配置
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        var service = CreateService();

        // Act
        var tools = (await service.GetToolsAsync()).ToList();
        var groups = (await service.GetGroupsAsync()).ToList();

        // Assert
        Assert.Equal(2, tools.Count);
        Assert.All(tools, t => Assert.Equal(ToolSource.Default, t.Source));
        Assert.Contains(tools, t => t.Id == "default-tool-1");
        Assert.Contains(tools, t => t.Id == "default-tool-2");

        Assert.Single(groups);
        Assert.Equal("default-group", groups[0].Id);
    }

    [Fact]
    public async Task LoadAsync_BothConfigs_MergesCorrectly()
    {
        // Arrange: 两个配置都存在
        var defaultConfig = CreateDefaultConfig();
        var userConfig = CreateUserConfig();

        await WriteConfigAsync(_defaultConfigPath, defaultConfig);
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        var service = CreateService();

        // Act
        var tools = (await service.GetToolsAsync()).ToList();
        var groups = (await service.GetGroupsAsync()).ToList();

        // Assert: 应该包含默认工具（2个）和用户工具（1个）
        Assert.Equal(3, tools.Count);

        // 默认工具应标记为 Default
        var defaultTool1 = tools.First(t => t.Id == "default-tool-1");
        var defaultTool2 = tools.First(t => t.Id == "default-tool-2");
        Assert.Equal(ToolSource.Default, defaultTool1.Source);
        Assert.Equal(ToolSource.Default, defaultTool2.Source);

        // 用户工具应标记为 User
        var userTool = tools.First(t => t.Id == "user-tool-1");
        Assert.Equal(ToolSource.User, userTool.Source);

        // 应包含两个分组
        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public async Task LoadAsync_NoConfig_CreatesDefaultData()
    {
        // Arrange: 没有任何配置
        var service = CreateService();

        // Act
        var tools = (await service.GetToolsAsync()).ToList();
        var groups = (await service.GetGroupsAsync()).ToList();

        // Assert: 应创建默认数据
        Assert.NotEmpty(tools);
        Assert.NotEmpty(groups);

        // 验证用户配置文件被创建
        Assert.True(File.Exists(Path.Combine(_userDataDirectory, "tools.json")));
    }

    [Fact]
    public async Task LoadAsync_UserOverridesDefault_CorrectlyMerged()
    {
        // Arrange: 用户覆盖默认工具
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        var userConfig = new ToolsData
        {
            Tools =
            [
                new ToolItem
                {
                    Id = "default-tool-1",  // 与默认工具 ID 相同
                    Name = "用户修改的工具1",
                    ShortDescription = "用户修改的描述",
                    ExecutablePath = @"C:\Custom\notepad.exe"
                }
            ]
        };
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        var service = CreateService();

        // Act
        var tools = (await service.GetToolsAsync()).ToList();

        // Assert
        Assert.Equal(2, tools.Count);

        var overriddenTool = tools.First(t => t.Id == "default-tool-1");
        Assert.Equal("用户修改的工具1", overriddenTool.Name);
        Assert.Equal(ToolSource.UserOverride, overriddenTool.Source);

        var defaultTool2 = tools.First(t => t.Id == "default-tool-2");
        Assert.Equal(ToolSource.Default, defaultTool2.Source);
    }

    #endregion

    #region 添加工具测试

    [Fact]
    public async Task AddToolAsync_NewTool_SavedToUserConfig()
    {
        // Arrange
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        var service = CreateService();
        await service.GetToolsAsync(); // 触发加载

        var newTool = new ToolItem
        {
            Id = "new-user-tool",
            Name = "新用户工具",
            ShortDescription = "新工具描述",
            ExecutablePath = @"C:\Tools\new.exe"
        };

        // Act
        var result = await service.AddToolAsync(newTool);

        // Assert
        Assert.True(result);

        var tools = (await service.GetToolsAsync()).ToList();
        Assert.Equal(3, tools.Count); // 2 默认 + 1 新增

        var addedTool = tools.First(t => t.Id == "new-user-tool");
        Assert.Equal(ToolSource.User, addedTool.Source);

        // 验证只保存到用户配置
        var userConfigPath = Path.Combine(_userDataDirectory, "tools.json");
        Assert.True(File.Exists(userConfigPath));

        var userJson = await File.ReadAllTextAsync(userConfigPath);
        var userData = JsonSerializer.Deserialize<ToolsData>(userJson, _jsonOptions);
        Assert.Single(userData!.Tools);
        Assert.Equal("new-user-tool", userData.Tools[0].Id);
    }

    [Fact]
    public async Task AddToolAsync_DuplicateId_ReturnsFalse()
    {
        // Arrange
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        var service = CreateService();

        var duplicateTool = new ToolItem
        {
            Id = "default-tool-1", // 已存在的 ID
            Name = "重复工具"
        };

        // Act
        var result = await service.AddToolAsync(duplicateTool);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region 更新工具测试

    [Fact]
    public async Task UpdateToolAsync_DefaultTool_MarkedAsUserOverride()
    {
        // Arrange
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        var service = CreateService();
        var tools = (await service.GetToolsAsync()).ToList();
        var toolToUpdate = tools.First(t => t.Id == "default-tool-1");

        // Act
        toolToUpdate.Name = "修改后的默认工具";
        toolToUpdate.ShortDescription = "修改后的描述";
        var result = await service.UpdateToolAsync(toolToUpdate);

        // Assert
        Assert.True(result);

        var updatedTools = (await service.GetToolsAsync()).ToList();
        var updatedTool = updatedTools.First(t => t.Id == "default-tool-1");

        Assert.Equal("修改后的默认工具", updatedTool.Name);
        Assert.Equal(ToolSource.UserOverride, updatedTool.Source);

        // 验证保存到用户配置
        var userConfigPath = Path.Combine(_userDataDirectory, "tools.json");
        var userJson = await File.ReadAllTextAsync(userConfigPath);
        var userData = JsonSerializer.Deserialize<ToolsData>(userJson, _jsonOptions);
        Assert.Single(userData!.Tools);
        Assert.Equal("default-tool-1", userData.Tools[0].Id);
    }

    [Fact]
    public async Task UpdateToolAsync_UserTool_KeepsUserSource()
    {
        // Arrange
        var userConfig = CreateUserConfig();
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        var service = CreateService();
        var tools = (await service.GetToolsAsync()).ToList();
        var toolToUpdate = tools.First(t => t.Id == "user-tool-1");

        // Act
        toolToUpdate.Name = "修改后的用户工具";
        var result = await service.UpdateToolAsync(toolToUpdate);

        // Assert
        Assert.True(result);

        var updatedTools = (await service.GetToolsAsync()).ToList();
        var updatedTool = updatedTools.First(t => t.Id == "user-tool-1");

        Assert.Equal("修改后的用户工具", updatedTool.Name);
        Assert.Equal(ToolSource.User, updatedTool.Source);
    }

    [Fact]
    public async Task UpdateToolAsync_NonExistentTool_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        await service.GetToolsAsync();

        var nonExistentTool = new ToolItem
        {
            Id = "non-existent",
            Name = "不存在的工具"
        };

        // Act
        var result = await service.UpdateToolAsync(nonExistentTool);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region 删除工具测试

    [Fact]
    public async Task DeleteToolAsync_UserTool_Success()
    {
        // Arrange
        var userConfig = CreateUserConfig();
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        var service = CreateService();
        var tools = (await service.GetToolsAsync()).ToList();
        Assert.Single(tools);

        // Act
        var result = await service.DeleteToolAsync("user-tool-1");

        // Assert
        Assert.True(result);

        var remainingTools = (await service.GetToolsAsync()).ToList();
        Assert.Empty(remainingTools);
    }

    [Fact]
    public async Task DeleteToolAsync_DefaultTool_ReturnsFalse()
    {
        // Arrange
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        var service = CreateService();

        // Act
        var result = await service.DeleteToolAsync("default-tool-1");

        // Assert: 不允许删除默认工具
        Assert.False(result);

        var tools = (await service.GetToolsAsync()).ToList();
        Assert.Equal(2, tools.Count);
        Assert.Contains(tools, t => t.Id == "default-tool-1");
    }

    [Fact]
    public async Task DeleteToolAsync_NonExistentTool_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        await service.GetToolsAsync();

        // Act
        var result = await service.DeleteToolAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsDefaultTool 测试

    [Fact]
    public async Task IsDefaultTool_DefaultTool_ReturnsTrue()
    {
        // Arrange
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        var service = CreateService();
        await service.GetToolsAsync(); // 触发加载

        // Act & Assert
        Assert.True(service.IsDefaultTool("default-tool-1"));
        Assert.True(service.IsDefaultTool("default-tool-2"));
    }

    [Fact]
    public async Task IsDefaultTool_UserTool_ReturnsFalse()
    {
        // Arrange
        var defaultConfig = CreateDefaultConfig();
        var userConfig = CreateUserConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        var service = CreateService();
        await service.GetToolsAsync(); // 触发加载

        // Act & Assert
        Assert.False(service.IsDefaultTool("user-tool-1"));
    }

    #endregion

    #region ResetToolToDefaultAsync 测试

    [Fact]
    public async Task ResetToolToDefaultAsync_OverriddenTool_RestoresDefault()
    {
        // Arrange: 先覆盖默认工具
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        var userConfig = new ToolsData
        {
            Tools =
            [
                new ToolItem
                {
                    Id = "default-tool-1",
                    Name = "用户修改的工具",
                    ShortDescription = "用户修改的描述"
                }
            ]
        };
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        var service = CreateService();
        var tools = (await service.GetToolsAsync()).ToList();

        // 验证初始状态
        var overriddenTool = tools.First(t => t.Id == "default-tool-1");
        Assert.Equal("用户修改的工具", overriddenTool.Name);
        Assert.Equal(ToolSource.UserOverride, overriddenTool.Source);

        // Act
        var result = await service.ResetToolToDefaultAsync("default-tool-1");

        // Assert
        Assert.True(result);

        // 重新加载验证
        await service.ReloadAsync();
        var resetTools = (await service.GetToolsAsync()).ToList();
        var resetTool = resetTools.First(t => t.Id == "default-tool-1");

        Assert.Equal("默认工具1", resetTool.Name);
        Assert.Equal(ToolSource.Default, resetTool.Source);
    }

    [Fact]
    public async Task ResetToolToDefaultAsync_UserTool_ReturnsFalse()
    {
        // Arrange
        var userConfig = CreateUserConfig();
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        var service = CreateService();
        await service.GetToolsAsync();

        // Act: 尝试重置用户工具（应该失败）
        var result = await service.ResetToolToDefaultAsync("user-tool-1");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region 分组测试

    [Fact]
    public async Task AddGroupAsync_NewGroup_SavedToUserConfig()
    {
        // Arrange
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        var service = CreateService();
        await service.GetGroupsAsync();

        var newGroup = new ToolGroup
        {
            Id = "new-user-group",
            Name = "新用户分组",
            Icon = "🆕"
        };

        // Act
        var result = await service.AddGroupAsync(newGroup);

        // Assert
        Assert.True(result);

        var groups = (await service.GetGroupsAsync()).ToList();
        Assert.Equal(2, groups.Count);
        Assert.Contains(groups, g => g.Id == "new-user-group");
    }

    [Fact]
    public async Task DeleteGroupAsync_UserGroup_Success()
    {
        // Arrange
        var userConfig = CreateUserConfig();
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        var service = CreateService();

        // Act
        var result = await service.DeleteGroupAsync("user-group");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteGroupAsync_DefaultGroup_ReturnsFalse()
    {
        // Arrange
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        var service = CreateService();

        // Act
        var result = await service.DeleteGroupAsync("default-group");

        // Assert
        Assert.False(result);

        var groups = (await service.GetGroupsAsync()).ToList();
        Assert.Contains(groups, g => g.Id == "default-group");
    }

    #endregion

    #region 路径测试

    [Fact]
    public void GetToolsFilePath_ReturnsUserConfigPath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var path = service.GetToolsFilePath();

        // Assert
        Assert.Equal(Path.Combine(_userDataDirectory, "tools.json"), path);
    }

    [Fact]
    public void GetDefaultConfigPath_ReturnsDefaultPath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var path = service.GetDefaultConfigPath();

        // Assert
        Assert.Equal(_defaultConfigPath, path);
    }

    #endregion

    #region 持久化测试

    [Fact]
    public async Task SaveAsync_EmptyUserData_DeletesUserConfigFile()
    {
        // Arrange: 只有默认配置，没有用户数据
        var defaultConfig = CreateDefaultConfig();
        await WriteConfigAsync(_defaultConfigPath, defaultConfig);

        // 创建一个空的用户配置文件
        var userConfigPath = Path.Combine(_userDataDirectory, "tools.json");
        await File.WriteAllTextAsync(userConfigPath, "{}");
        Assert.True(File.Exists(userConfigPath));

        var service = CreateService();
        await service.GetToolsAsync();

        // Act: 触发保存（用户数据为空）
        await service.SaveAsync();

        // Assert: 用户配置文件应被删除
        Assert.False(File.Exists(userConfigPath));
    }

    [Fact]
    public async Task ReloadAsync_RefreshesData()
    {
        // Arrange
        var userConfig = CreateUserConfig();
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        var service = CreateService();
        var initialTools = (await service.GetToolsAsync()).ToList();
        Assert.Single(initialTools);

        // 修改配置文件
        userConfig.Tools.Add(new ToolItem
        {
            Id = "another-tool",
            Name = "另一个工具"
        });
        await WriteConfigAsync(Path.Combine(_userDataDirectory, "tools.json"), userConfig);

        // Act
        await service.ReloadAsync();
        var reloadedTools = (await service.GetToolsAsync()).ToList();

        // Assert
        Assert.Equal(2, reloadedTools.Count);
    }

    #endregion
}
