namespace VistaLauncher.Services;

public class ImportService : IImportService
{
    public event EventHandler<ImportProgressEventArgs>? ProgressChanged;

    public bool ValidateNirLauncherDirectory(string path)
    {
        return !string.IsNullOrEmpty(path);
    }

    public Task<ImportResult> ImportFromNirLauncherAsync(string path, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ImportResult { ImportedTools = 0, SkippedTools = 0 });
    }
}
