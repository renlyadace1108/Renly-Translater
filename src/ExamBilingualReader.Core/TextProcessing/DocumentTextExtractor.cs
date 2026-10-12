using System.IO;
using System.Text.RegularExpressions;

namespace ExamBilingualReader.Core.TextProcessing;

public static partial class DocumentTextExtractor
{
    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"!\[.*?\]\(.*?\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownImageRegex();

    [GeneratedRegex(@"\[(.*?)\]\(.*?\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"^#{1,6}\s*", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MarkdownHeadingRegex();

    /// <summary>
    /// 支持从常见文本类型 (.txt, .md, .markdown, .json, .csv, .html, .xml, .pdf) 提取适合精读的学术英文段落文本
    /// </summary>
    public static string ExtractFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件不存在", filePath);

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => PdfExtractor.ExtractText(filePath),
            ".md" or ".markdown" => CleanMarkdownText(File.ReadAllText(filePath)),
            ".html" or ".htm" => CleanHtmlText(File.ReadAllText(filePath)),
            _ => File.ReadAllText(filePath)
        };
    }

    /// <summary>
    /// 清理 Markdown 标记（保留标题文字与纯段落），滤除无用图片链接与排版字符
    /// </summary>
    public static string CleanMarkdownText(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // 去除图片链接 ![alt](url)
        var text = MarkdownImageRegex().Replace(markdown, "");

        // 提取链接文字 [text](url) -> text
        text = MarkdownLinkRegex().Replace(text, "$1");

        // 去除前置标题符号 ### 
        text = MarkdownHeadingRegex().Replace(text, "");

        // 替换粗体斜体标记
        text = text.Replace("**", "").Replace("__", "").Replace("*", "").Replace("_", "");

        return PdfExtractor.CleanAcademicPdfText(text);
    }

    /// <summary>
    /// 清理 HTML 标签
    /// </summary>
    public static string CleanHtmlText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = HtmlTagRegex().Replace(html, " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return PdfExtractor.CleanAcademicPdfText(text);
    }
}
