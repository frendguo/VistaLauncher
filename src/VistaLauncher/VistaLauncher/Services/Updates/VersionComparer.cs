using System.Text.RegularExpressions;

namespace VistaLauncher.Services.Updates;

/// <summary>
/// 版本比较工具类
/// </summary>
public static partial class VersionComparer
{
    /// <summary>
    /// 比较两个版本号
    /// </summary>
    /// <param name="v1">版本号1</param>
    /// <param name="v2">版本号2</param>
    /// <returns>负数表示 v1 小于 v2，0 表示相等，正数表示 v1 大于 v2</returns>
    public static int Compare(string? v1, string? v2)
    {
        if (string.IsNullOrEmpty(v1) && string.IsNullOrEmpty(v2))
            return 0;
        if (string.IsNullOrEmpty(v1))
            return -1;
        if (string.IsNullOrEmpty(v2))
            return 1;

        var normalized1 = NormalizeVersion(v1);
        var normalized2 = NormalizeVersion(v2);

        // 尝试使用 Version 类比较
        if (Version.TryParse(normalized1, out var version1) &&
            Version.TryParse(normalized2, out var version2))
        {
            return version1.CompareTo(version2);
        }

        // 回退到字符串分段比较
        return CompareVersionStrings(normalized1, normalized2);
    }

    /// <summary>
    /// 判断 newVersion 是否比 currentVersion 更新
    /// </summary>
    public static bool IsNewer(string? newVersion, string? currentVersion)
    {
        return Compare(newVersion, currentVersion) > 0;
    }

    /// <summary>
    /// 标准化版本号
    /// </summary>
    private static string NormalizeVersion(string version)
    {
        var normalized = version.TrimStart('v', 'V');
        var match = VersionPartRegex().Match(normalized);
        return match.Success ? match.Value : normalized;
    }

    /// <summary>
    /// 字符串分段比较版本号
    /// </summary>
    private static int CompareVersionStrings(string v1, string v2)
    {
        var parts1 = v1.Split('.', '-', '_');
        var parts2 = v2.Split('.', '-', '_');
        var maxLength = Math.Max(parts1.Length, parts2.Length);

        for (var i = 0; i < maxLength; i++)
        {
            var part1 = i < parts1.Length ? parts1[i] : "0";
            var part2 = i < parts2.Length ? parts2[i] : "0";

            if (int.TryParse(part1, out var num1) && int.TryParse(part2, out var num2))
            {
                if (num1 != num2)
                    return num1.CompareTo(num2);
            }
            else
            {
                var cmp = string.Compare(part1, part2, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0)
                    return cmp;
            }
        }

        return 0;
    }

    [GeneratedRegex(@"^[\d.]+")]
    private static partial Regex VersionPartRegex();
}
