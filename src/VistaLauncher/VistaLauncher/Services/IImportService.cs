using VistaLauncher.Models;

namespace VistaLauncher.Services;

public interface IImportService
{
    bool ValidateNirLauncherDirectory(string path);
    Task<ImportResult> ImportFromNirLauncherAsync(string path, CancellationToken cancellationToken);
}

public class ImportProgressEventArgs : EventArgs
{
    public double Current { get; set; }
    public double Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrentItem { get; set; } = string.Empty;
}

public class ImportResult
{
    public int ImportedTools { get; set; }
    public int SkippedTools { get; set; }
    public List<string> Errors { get; set; } = new();
}
