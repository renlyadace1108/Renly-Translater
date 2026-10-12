using System.Net.Http.Headers;
using System.Text.Json;
using ExamBilingualReader.Core.Models.ExamHub;
using ExamBilingualReader.Core.TextProcessing;

namespace ExamBilingualReader.Infrastructure.Services;

public interface IExamHubService
{
    Task<List<GitHubExamPaperItem>> FetchLatestExamPapersAsync(CancellationToken cancellationToken = default);
    Task<string> DownloadPaperContentAsync(string rawDownloadUrl, CancellationToken cancellationToken = default);
}

public class GitHubExamHubService : IExamHubService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(25)
    };

    static GitHubExamHubService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ExamBilingualReader", "1.0"));
    }

    /// <summary>
    /// 从持续更新的开源四六级/真题仓库（wamich/english-exem-md）拉取最新的真题目录列表
    /// </summary>
    public async Task<List<GitHubExamPaperItem>> FetchLatestExamPapersAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<GitHubExamPaperItem>();
        const string apiUrl = "https://api.github.com/repos/wamich/english-exem-md/git/trees/main?recursive=1";

        try
        {
            using var response = await HttpClient.GetAsync(apiUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return GetFallbackPresetPapers();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("tree", out var treeElement))
                return GetFallbackPresetPapers();

            foreach (var item in treeElement.EnumerateArray())
            {
                var path = item.GetProperty("path").GetString() ?? string.Empty;
                var type = item.GetProperty("type").GetString() ?? string.Empty;

                if (type != "blob" || !path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || path.StartsWith("README", StringComparison.OrdinalIgnoreCase))
                    continue;

                // 路径结构: CET6/2023.12/cet6-2023-12-1.md
                var parts = path.Split('/');
                if (parts.Length < 3) continue;

                var category = parts[0];
                var yearMonth = parts[1];
                var fileName = parts[2];
                var size = item.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt64() : 0;

                var rawUrl = $"https://raw.githubusercontent.com/wamich/english-exem-md/main/{path}";

                result.Add(new GitHubExamPaperItem
                {
                    Category = category,
                    YearMonth = yearMonth,
                    FileName = fileName,
                    Title = FormatPaperTitle(category, yearMonth, fileName),
                    DownloadUrl = rawUrl,
                    Size = size
                });
            }

            // 按年份倒序排列，优先展示最新试卷
            return result.OrderByDescending(r => r.YearMonth).ThenBy(r => r.FileName).ToList();
        }
        catch
        {
            // 离线或 GitHub 访问受限时，使用内置的高频真实试卷链接保底
            return GetFallbackPresetPapers();
        }
    }

    public async Task<string> DownloadPaperContentAsync(string rawDownloadUrl, CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(rawDownloadUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        var rawMarkdown = await response.Content.ReadAsStringAsync(cancellationToken);
        return DocumentTextExtractor.CleanMarkdownText(rawMarkdown);
    }

    private static string FormatPaperTitle(string category, string yearMonth, string fileName)
    {
        // cet6-2023-12-1.md -> 第 1 套
        var setIndex = "1";
        var parts = fileName.Split('-');
        if (parts.Length >= 4)
        {
            setIndex = parts[3].Replace(".md", "");
        }

        return $"{yearMonth} {category} 考试真题 (第 {setIndex} 套)";
    }

    private static List<GitHubExamPaperItem> GetFallbackPresetPapers()
    {
        return
        [
            new() {
                Category = "CET6",
                YearMonth = "2023.12",
                FileName = "cet6-2023-12-1.md",
                Title = "2023.12 CET6 考试真题 (第 1 套)",
                DownloadUrl = "https://raw.githubusercontent.com/wamich/english-exem-md/main/CET6/2023.12/cet6-2023-12-1.md"
            },
            new() {
                Category = "CET6",
                YearMonth = "2023.12",
                FileName = "cet6-2023-12-2.md",
                Title = "2023.12 CET6 考试真题 (第 2 套)",
                DownloadUrl = "https://raw.githubusercontent.com/wamich/english-exem-md/main/CET6/2023.12/cet6-2023-12-2.md"
            },
            new() {
                Category = "CET4",
                YearMonth = "2023.12",
                FileName = "cet4-2023-12-1.md",
                Title = "2023.12 CET4 考试真题 (第 1 套)",
                DownloadUrl = "https://raw.githubusercontent.com/wamich/english-exem-md/main/CET4/2023.12/cet4-2023-12-1.md"
            },
            new() {
                Category = "CET4",
                YearMonth = "2023.06",
                FileName = "cet4-2023-06-1.md",
                Title = "2023.06 CET4 考试真题 (第 1 套)",
                DownloadUrl = "https://raw.githubusercontent.com/wamich/english-exem-md/main/CET4/2023.06/cet4-2023-06-1.md"
            }
        ];
    }
}
