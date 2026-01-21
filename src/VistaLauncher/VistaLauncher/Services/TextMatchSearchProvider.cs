using VistaLauncher.Models;

namespace VistaLauncher.Services;

/// <summary>
/// 文本匹配搜索提供者
/// 基于名称、描述和标签进行模糊匹配
/// </summary>
public class TextMatchSearchProvider : ISearchProvider
{
    public Task<IEnumerable<ToolItem>> SearchAsync(
        string query,
        IEnumerable<ToolItem> tools,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(tools);
        }

        // 预计算小写查询字符串（只计算一次）
        var lowerQuery = query.ToLower();
        var queryTokens = lowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var results = tools.Where(tool =>
        {
            // 每个 token 都必须在 Name、ShortDescription、LongDescription 或 Tags 中出现
            return queryTokens.All(token =>
            {
                var nameMatch = tool.Name.Contains(token, StringComparison.OrdinalIgnoreCase);
                var shortDescMatch = tool.ShortDescription.Contains(token, StringComparison.OrdinalIgnoreCase);
                var longDescMatch = tool.LongDescription.Contains(token, StringComparison.OrdinalIgnoreCase);
                var tagMatch = tool.Tags.Any(tag => tag.Contains(token, StringComparison.OrdinalIgnoreCase));

                return nameMatch || shortDescMatch || longDescMatch || tagMatch;
            });
        });

        // 先计算分数再排序（避免排序时重复计算分数）
        var sortedResults = results
            .Select(tool => (tool, score: CalculateScore(tool, lowerQuery, queryTokens)))
            .OrderByDescending(x => x.score)
            .Select(x => x.tool);

        return Task.FromResult(sortedResults.AsEnumerable());
    }

    /// <summary>
    /// 计算工具的匹配分数
    /// </summary>
    private static int CalculateScore(ToolItem tool, string lowerQuery, string[] queryTokens)
    {
        var score = 0;
        var lowerName = tool.Name.ToLower();

        // 名称完全匹配得分最高
        if (lowerName == lowerQuery)
        {
            score += 100;
        }
        // 名称以查询开头
        else if (lowerName.StartsWith(lowerQuery))
        {
            score += 50;
        }
        // 名称包含查询
        else if (lowerName.Contains(lowerQuery))
        {
            score += 25;
        }

        // 每个匹配的 token 加分
        foreach (var token in queryTokens)
        {
            if (tool.Name.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += 10;
            if (tool.ShortDescription.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += 5;
            if (tool.Tags.Any(t => t.Contains(token, StringComparison.OrdinalIgnoreCase)))
                score += 3;
        }

        return score;
    }
}
