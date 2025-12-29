using System.Text.Json;
using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 应用配置服务
/// </summary>
public class ConfigService
{
    private readonly string _configFilePath;
    private AppConfig _config = new();
    private bool _isLoaded = false;

    public ConfigService()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VistaLauncher");
        _configFilePath = Path.Combine(dataDirectory, "config.json");
        
        Directory.CreateDirectory(dataDirectory);
    }

    /// <summary>
    /// 获取当前配置
    /// </summary>
    public async Task<AppConfig> GetConfigAsync()
    {
        await EnsureLoadedAsync();
        return _config;
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public async Task SaveConfigAsync(AppConfig config)
    {
        _config = config;
        var json = JsonSerializer.Serialize(_config, JsonContext.Default.AppConfig);
        await File.WriteAllTextAsync(_configFilePath, json);
    }

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    public string GetConfigFilePath() => _configFilePath;

    private async Task EnsureLoadedAsync()
    {
        if (_isLoaded) return;

        if (File.Exists(_configFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                var config = JsonSerializer.Deserialize(json, JsonContext.Default.AppConfig);
                if (config != null)
                {
                    _config = config;
                }
            }
            catch (Exception)
            {
                _config = new AppConfig();
            }
        }
        else
        {
            _config = new AppConfig();
            await SaveConfigAsync(_config);
        }

        _isLoaded = true;
    }
}
