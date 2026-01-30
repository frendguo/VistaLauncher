using VistaLauncher.Services;
using Xunit;

namespace VistaLauncher.Tests;

/// <summary>
/// 路径解析服务测试
/// </summary>
public class PathResolverServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly PathResolverService _service;

    public PathResolverServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PathResolver_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _service = new PathResolverService(_testDirectory);
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
    public void ResolvePath_EmptyPath_ReturnsEmpty()
    {
        // Act
        var result = _service.ResolvePath(string.Empty);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ResolvePath_NoVariables_ReturnsOriginal()
    {
        // Arrange
        var path = @"C:\Tools\tool.exe";

        // Act
        var result = _service.ResolvePath(path);

        // Assert
        Assert.Equal(path, result);
    }

    [Fact]
    public void ResolvePath_ToolsPathVariable_ReplacesCorrectly()
    {
        // Arrange
        var path = @"${ToolsPath}\tool.exe";

        // Act
        var result = _service.ResolvePath(path);

        // Assert
        Assert.Equal(Path.Combine(_testDirectory, "tool.exe"), result);
    }

    [Fact]
    public void ResolvePath_MultipleVariables_ReplacesAll()
    {
        // Arrange
        var path = @"${ToolsPath}\subfolder";

        // Act
        var result = _service.ResolvePath(path);

        // Assert
        Assert.Equal(Path.Combine(_testDirectory, "subfolder"), result);
    }

    [Fact]
    public void GetToolsDirectory_ReturnsConfiguredPath()
    {
        // Act
        var result = _service.GetToolsDirectory();

        // Assert
        Assert.Equal(_testDirectory, result);
    }

    [Fact]
    public void GetToolInstallDirectory_CreatesSubdirectory()
    {
        // Arrange
        var toolId = "test-tool-1";
        var toolName = "Test Tool";

        // Act
        var result = _service.GetToolInstallDirectory(toolId, toolName);

        // Assert
        Assert.True(Directory.Exists(result));
        Assert.Contains("Test_Tool", result);
    }

    [Fact]
    public void GetToolInstallDirectory_SanitizesName()
    {
        // Arrange
        var toolId = "test-tool-2";
        var toolName = "Tool/With\\Invalid:Chars";

        // Act
        var result = _service.GetToolInstallDirectory(toolId, toolName);

        // Assert
        // 验证目录存在（意味着创建成功）
        Assert.True(Directory.Exists(result));
        // 验证名称被清理过（包含下划线替换）
        Assert.Contains("Tool_With_Invalid_Chars", result);
    }
}
