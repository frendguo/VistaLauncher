using VistaLauncher.Core.Services;
using Xunit;

namespace VistaLauncher.Tests;

/// <summary>
/// FileVersionHelper 版本提取功能测试
/// </summary>
public class ImportServiceTests : IDisposable
{
    private readonly string _testDirectory;

    public ImportServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"VistaLauncher_ImportTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
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
                // 忽略删除失败
            }
        }
    }

    #region ExtractVersionFromExe 单元测试

    [Fact]
    public void ExtractVersionFromExe_ValidExeWithVersion_ReturnsVersion()
    {
        // Arrange: 使用系统自带的 notepad.exe 作为测试文件
        var notepadPath = Path.Combine(Environment.SystemDirectory, "notepad.exe");

        if (!File.Exists(notepadPath))
        {
            // 跳过测试如果文件不存在
            return;
        }

        // Act
        var version = FileVersionHelper.ExtractVersionFromExe(notepadPath);

        // Assert: 应该返回非空版本号
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        // Windows 的 notepad.exe 版本号格式类似 "10.0.19041.1"
        Assert.Matches(@"\d+(\.\d+)+", version);
    }

    [Fact]
    public void ExtractVersionFromExe_NonExistentFile_ReturnsEmptyString()
    {
        // Arrange: 一个不存在的文件路径
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.exe");

        // Act
        var version = FileVersionHelper.ExtractVersionFromExe(nonExistentPath);

        // Assert
        Assert.Equal(string.Empty, version);
    }

    [Fact]
    public void ExtractVersionFromExe_EmptyPath_ReturnsEmptyString()
    {
        // Arrange: 空字符串路径
        var emptyPath = string.Empty;

        // Act
        var version = FileVersionHelper.ExtractVersionFromExe(emptyPath);

        // Assert
        Assert.Equal(string.Empty, version);
    }

    [Fact]
    public void ExtractVersionFromExe_NullPath_ReturnsEmptyString()
    {
        // Arrange: null 路径
        string nullPath = null!;

        // Act
        var version = FileVersionHelper.ExtractVersionFromExe(nullPath);

        // Assert
        Assert.Equal(string.Empty, version);
    }

    [Fact]
    public void ExtractVersionFromExe_TextFile_ReturnsEmptyString()
    {
        // Arrange: 创建一个文本文件
        var textFile = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(textFile, "This is a text file");

        // Act
        var version = FileVersionHelper.ExtractVersionFromExe(textFile);

        // Assert: 文本文件没有版本信息，应返回空字符串
        Assert.Equal(string.Empty, version);
    }

    [Fact]
    public void ExtractVersionFromExe_InvalidExeFile_ReturnsEmptyString()
    {
        // Arrange: 创建一个空文件（不是有效的 PE 文件）
        var exePath = Path.Combine(_testDirectory, "invalid.exe");
        File.WriteAllBytes(exePath, []);

        // Act
        var version = FileVersionHelper.ExtractVersionFromExe(exePath);

        // Assert: 无效的 exe 会返回空字符串
        Assert.Equal(string.Empty, version);
    }

    #endregion

    #region 集成测试

    [Fact]
    public void ExtractVersionFromExe_RegEditExe_ReturnsVersion()
    {
        // Arrange: 使用 regedit.exe 作为测试
        var regeditPath = Path.Combine(Environment.SystemDirectory, "regedit.exe");

        if (!File.Exists(regeditPath))
        {
            return;
        }

        // Act
        var version = FileVersionHelper.ExtractVersionFromExe(regeditPath);

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        Assert.Matches(@"\d+(\.\d+)+", version);
    }

    [Fact]
    public void ExtractVersionFromExe_ExePathWithSpaces_ReturnsVersion()
    {
        // Arrange: 创建带空格路径的测试
        var notepadPath = Path.Combine(Environment.SystemDirectory, "notepad.exe");

        if (!File.Exists(notepadPath))
        {
            return;
        }

        // 将 notepad.exe 复制到带空格的路径
        var destDir = Path.Combine(_testDirectory, "Folder With Spaces");
        Directory.CreateDirectory(destDir);
        var destPath = Path.Combine(destDir, "notepad.exe");
        File.Copy(notepadPath, destPath);

        // Act
        var version = FileVersionHelper.ExtractVersionFromExe(destPath);

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }

    #endregion
}
