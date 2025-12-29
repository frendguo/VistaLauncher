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

        // 将查询分词（按空格分隔）
        var queryTokens = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

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

        // 按匹配度排序：名称完全匹配 > 名称包含 > 描述包含
        var sortedResults = results.OrderByDescending(tool =>
        {
            var score = 0;
            var lowerName = tool.Name.ToLower();

            // 名称完全匹配得分最高
            if (lowerName == query.ToLower())
            {
                score += 100;
            }
            // 名称以查询开头
            else if (lowerName.StartsWith(query.ToLower()))
            {
                score += 50;
            }
            // 名称包含查询
            else if (lowerName.Contains(query.ToLower()))
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
        });

        return Task.FromResult(sortedResults.AsEnumerable());
    }
}
