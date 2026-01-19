using System.Text;

namespace VistaLauncher.Services;

/// <summary>
/// 简单的 INI 文件解析器
/// </summary>
internal class IniParser
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 获取所有节
    /// </summary>
    public IReadOnlyDictionary<string, Dictionary<string, string>> Sections => _sections;

    /// <summary>
    /// 从文件解析 INI
    /// </summary>
    public static async Task<IniParser> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var parser = new IniParser();

        // NirLauncher 的 nlp 文件可能是 ANSI 编码
        var encoding = DetectEncoding(filePath);
        var lines = await File.ReadAllLinesAsync(filePath, encoding, cancellationToken);

        string? currentSection = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 跳过空行和注释
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(';') || trimmedLine.StartsWith('#'))
            {
                continue;
            }

            // 检测节 [SectionName]
            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                currentSection = trimmedLine[1..^1].Trim();
                if (!parser._sections.ContainsKey(currentSection))
                {
                    parser._sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                continue;
            }

            // 解析 Key=Value
            if (currentSection != null)
            {
                var equalsIndex = trimmedLine.IndexOf('=');
                if (equalsIndex > 0)
                {
                    var key = trimmedLine[..equalsIndex].Trim();
                    var value = trimmedLine[(equalsIndex + 1)..].Trim();
                    parser._sections[currentSection][key] = value;
                }
            }
        }

        return parser;
    }

    /// <summary>
    /// 获取指定节的指定键的值
    /// </summary>
    public string? GetValue(string section, string key)
    {
        if (_sections.TryGetValue(section, out var sectionData))
        {
            if (sectionData.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取指定节的指定键的值，如果不存在则返回默认值
    /// </summary>
    public string GetValueOrDefault(string section, string key, string defaultValue = "")
    {
        return GetValue(section, key) ?? defaultValue;
    }

    /// <summary>
    /// 获取指定节的指定键的整数值
    /// </summary>
    public int GetIntValue(string section, string key, int defaultValue = 0)
    {
        var value = GetValue(section, key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// 检查是否存在指定节
    /// </summary>
    public bool HasSection(string section)
    {
        return _sections.ContainsKey(section);
    }

    /// <summary>
    /// 检测文件编码
    /// </summary>
    private static Encoding DetectEncoding(string filePath)
    {
        // 读取文件开头的 BOM
        var bom = new byte[4];
        using (var file = File.OpenRead(filePath))
        {
            file.Read(bom, 0, 4);
        }

        // 检测 BOM
        if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            return Encoding.UTF8;
        if (bom[0] == 0xFF && bom[1] == 0xFE)
            return Encoding.Unicode; // UTF-16 LE
        if (bom[0] == 0xFE && bom[1] == 0xFF)
            return Encoding.BigEndianUnicode; // UTF-16 BE

        // 默认使用系统默认编码（通常是 Windows-1252 或当前系统代码页）
        // 对于 NirLauncher 的文件，这通常是正确的选择
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252);
    }
}
