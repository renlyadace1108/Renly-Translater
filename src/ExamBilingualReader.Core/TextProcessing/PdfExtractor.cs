using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace ExamBilingualReader.Core.TextProcessing;

public static partial class PdfExtractor
{
    [GeneratedRegex(@"\r\n|\r|\n", RegexOptions.Compiled)]
    private static partial Regex NewlineRegex();

    [GeneratedRegex(@"(\w+)-\s*\n\s*(\w+)", RegexOptions.Compiled)]
    private static partial Regex HyphenatedWordRegex();

    [GeneratedRegex(@"[ \t]+", RegexOptions.Compiled)]
    private static partial Regex MultiSpaceRegex();

    /// <summary>
    /// 从 PDF 文件中提取纯文本，并智能修复跨行连字符 (hyphenation) 与异常换行，还原学术段落结构。
    /// </summary>
    public static string ExtractText(string pdfFilePath)
    {
        if (!File.Exists(pdfFilePath))
            throw new FileNotFoundException("未找到指定的 PDF 文件", pdfFilePath);

        var sb = new StringBuilder();

        using (var document = PdfDocument.Open(pdfFilePath))
        {
            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    sb.AppendLine(pageText);
                    sb.AppendLine(); // 页面之间增加空行
                }
            }
        }

        return CleanAcademicPdfText(sb.ToString());
    }

    /// <summary>
    /// 学术论文/真题 PDF 排版修整：
    /// 1. 修复跨行连字符断词 (例: "cog- \n nitive" -> "cognitive")
    /// 2. 合并同段落内的硬换行，保留真实双换行段落
    /// </summary>
    public static string CleanAcademicPdfText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        // 1. 修复连字符跨行
        var text = HyphenatedWordRegex().Replace(rawText, "$1$2");

        // 2. 统一换行符
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // 3. 将连续2个及以上换行视为段落分隔，单个换行视为句内折行并替换为空格
        var paragraphs = text.Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries);
        var cleanedParagraphs = new List<string>();

        foreach (var p in paragraphs)
        {
            var singleLine = p.Replace('\n', ' ');
            singleLine = MultiSpaceRegex().Replace(singleLine, " ").Trim();
            if (!string.IsNullOrWhiteSpace(singleLine))
            {
                cleanedParagraphs.Add(singleLine);
            }
        }

        return string.Join("\n\n", cleanedParagraphs);
    }
}
