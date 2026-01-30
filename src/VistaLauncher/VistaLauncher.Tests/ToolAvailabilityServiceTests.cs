using VistaLauncher.Models;
using VistaLauncher.Services;
using Xunit;

namespace VistaLauncher.Tests;

/// <summary>
/// 工具可用性检查服务测试
/// </summary>
public class ToolAvailabilityServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly PathResolverService _pathResolver;
    private readonly ToolAvailabilityService _service;

    public ToolAvailabilityServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"Availability_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _pathResolver = new PathResolverService(_testDirectory);
        _service = new ToolAvailabilityService(_pathResolver);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ExistingFile_ReturnsAvailable()
    {
        // Arrange: 创建一个测试文件
        var fileName = "test-tool.exe";
        var filePath = Path.Combine(_testDirectory, fileName);
        await File.WriteAllTextAsync(filePath, "test content");

        var tool = new ToolItem
        {
            Id = "tool-1",
            Name = "Test Tool",
            ExecutablePath = filePath
        };

        // Act
        var result = await _service.CheckAvailabilityAsync(tool);

        // Assert
        Assert.Equal(ToolAvailability.Available, result);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_NonExistingFile_ReturnsNotInstalled()
    {
        // Arrange
        var tool = new ToolItem
        {
            Id = "tool-2",
            Name = "Non Existent Tool",
            ExecutablePath = Path.Combine(_testDirectory, "non-existent.exe")
        };

        // Act
        var result = await _service.CheckAvailabilityAsync(tool);

        // Assert
        Assert.Equal(ToolAvailability.NotInstalled, result);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_WithUpdate_ReturnsUpdateAvailable()
    {
        // Arrange: 创建一个测试文件
        var fileName = "updatable-tool.exe";
        var filePath = Path.Combine(_testDirectory, fileName);
        await File.WriteAllTextAsync(filePath, "test content");

        var tool = new ToolItem
        {
            Id = "tool-3",
            Name = "Updatable Tool",
            ExecutablePath = filePath,
            Version = "1.0.0",
            UpdateInfo = new UpdateInfo
            {
                Version = "2.0.0"
            }
        };

        // Act
        var result = await _service.CheckAvailabilityAsync(tool);

        // Assert
        Assert.Equal(ToolAvailability.UpdateAvailable, result);
    }

    [Fact]
    public void IsToolInstalled_ExistingFile_ReturnsTrue()
    {
        // Arrange
        var fileName = "installed-tool.exe";
        var filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, "test content");

        var tool = new ToolItem
        {
            Id = "tool-4",
            Name = "Installed Tool",
            ExecutablePath = filePath
        };

        // Act
        var result = _service.IsToolInstalled(tool);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsToolInstalled_NonExistingFile_ReturnsFalse()
    {
        // Arrange
        var tool = new ToolItem
        {
            Id = "tool-5",
            Name = "Not Installed Tool",
            ExecutablePath = Path.Combine(_testDirectory, "not-found.exe")
        };

        // Act
        var result = _service.IsToolInstalled(tool);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetResolvedExecutablePath_VariablePath_ResolvesCorrectly()
    {
        // Arrange
        var tool = new ToolItem
        {
            Id = "tool-6",
            Name = "Variable Path Tool",
            ExecutablePath = @"${ToolsPath}\tool.exe"
        };

        // Act
        var result = _service.GetResolvedExecutablePath(tool);

        // Assert
        Assert.Equal(Path.Combine(_testDirectory, "tool.exe"), result);
    }

    [Fact]
    public void GetResolvedExecutablePath_EmptyPath_ReturnsDefault()
    {
        // Arrange
        var tool = new ToolItem
        {
            Id = "tool-7",
            Name = "No Path Tool",
            ExecutablePath = string.Empty
        };

        // Act
        var result = _service.GetResolvedExecutablePath(tool);

        // Assert
        Assert.Contains(tool.Name, result);
        Assert.EndsWith(".exe", result);
    }

    [Fact]
    public async Task CheckAvailabilitiesAsync_MultipleTools_ReturnsCorrectStatuses()
    {
        // Arrange: 创建两个测试文件
        var file1 = Path.Combine(_testDirectory, "tool1.exe");
        var file2 = Path.Combine(_testDirectory, "tool2.exe");
        await File.WriteAllTextAsync(file1, "content1");
        // file2 不创建

        var tools = new[]
        {
            new ToolItem { Id = "t1", Name = "Tool1", ExecutablePath = file1 },
            new ToolItem { Id = "t2", Name = "Tool2", ExecutablePath = file2 },
            new ToolItem { Id = "t3", Name = "Tool3", ExecutablePath = @"${ToolsPath}\non-existent.exe" }
        };

        // Act
        var results = await _service.CheckAvailabilitiesAsync(tools);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(ToolAvailability.Available, results["t1"]);
        Assert.Equal(ToolAvailability.NotInstalled, results["t2"]);
        Assert.Equal(ToolAvailability.NotInstalled, results["t3"]);
    }
}
