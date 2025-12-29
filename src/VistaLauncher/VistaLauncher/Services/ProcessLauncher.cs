using System.Diagnostics;
using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 进程启动器接口
/// </summary>
public interface IProcessLauncher
{
    /// <summary>
    /// 启动工具
    /// </summary>
    Task<bool> LaunchAsync(ToolItem tool);
}

/// <summary>
/// 进程启动器实现
/// </summary>
public class ProcessLauncher : IProcessLauncher
{
    public Task<bool> LaunchAsync(ToolItem tool)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.ExpandEnvironmentVariables(tool.ExecutablePath),
                Arguments = tool.Arguments,
                UseShellExecute = true,
            };

            // 设置工作目录
            if (!string.IsNullOrWhiteSpace(tool.WorkingDirectory))
            {
                startInfo.WorkingDirectory = Environment.ExpandEnvironmentVariables(tool.WorkingDirectory);
            }

            // Console 类型使用新窗口
            if (tool.Type == ToolType.Console)
            {
                startInfo.UseShellExecute = true;
            }

            Process.Start(startInfo);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to launch {tool.Name}: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}
