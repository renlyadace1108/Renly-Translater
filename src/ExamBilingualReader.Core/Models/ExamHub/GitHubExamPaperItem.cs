namespace ExamBilingualReader.Core.Models.ExamHub;

public class GitHubExamPaperItem
{
    public string Category { get; set; } = string.Empty; // CET4 / CET6 / IELTS
    public string Title { get; set; } = string.Empty;    // 例如: 2023年12月 六级真题 (第1套)
    public string YearMonth { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long Size { get; set; }
}
