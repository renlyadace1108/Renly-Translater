using System.Text.RegularExpressions;

namespace ExamBilingualReader.Core.TextProcessing;

public static partial class SentenceSplitter
{
    private static readonly HashSet<string> KnownHonorificsAndTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        "mr", "mrs", "ms", "dr", "prof", "sr", "jr", "rev", "gen", "rep", "sen", "st"
    };

    private static readonly HashSet<string> KnownAcademicAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "e.g", "i.e", "et al", "etc", "fig", "figs", "vs", "vol", "no", "approx", "dept", "univ", "ed", "pp"
    };

    [GeneratedRegex(@"(\.{2,}|[!?.])([""'\)\]}]*)(\s+|$)", RegexOptions.Compiled)]
    private static partial Regex SentenceEndRegex();

    [GeneratedRegex(@"\b[A-Za-z]\.$", RegexOptions.Compiled)]
    private static partial Regex SingleLetterInitialRegex();

    [GeneratedRegex(@"[^\w\s-]", RegexOptions.Compiled)]
    private static partial Regex PunctuationCleanRegex();

    /// <summary>
    /// 将文本切分为句子列表
    /// </summary>
    public static List<string> Split(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var sentences = new List<string>();
        var matches = SentenceEndRegex().Matches(text);
        int lastIndex = 0;

        foreach (Match match in matches)
        {
            int matchEnd = match.Index + match.Length;
            string candidate = text[lastIndex..matchEnd].Trim();

            int punctIndex = match.Groups[1].Index - lastIndex;
            string beforePunct = candidate[..punctIndex].TrimEnd();

            if (IsFalseSentenceBoundary(beforePunct, match.Groups[1].Value, text, matchEnd))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(candidate))
            {
                sentences.Add(candidate);
            }
            lastIndex = matchEnd;
        }

        if (lastIndex < text.Length)
        {
            var remaining = text[lastIndex..].Trim();
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                sentences.Add(remaining);
            }
        }

        return sentences;
    }

    private static bool IsFalseSentenceBoundary(string textBeforePunct, string punct, string fullText, int nextIndex)
    {
        if (punct != ".")
            return false;

        if (SingleLetterInitialRegex().IsMatch(textBeforePunct))
            return true;

        var words = textBeforePunct.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return false;

        var lastWord = words[^1].Trim().TrimStart('(', '[', '{', '"', '\'');

        if (KnownHonorificsAndTitles.Contains(lastWord))
            return true;

        if (KnownAcademicAbbreviations.Contains(lastWord))
            return true;

        if (lastWord.Contains('.'))
        {
            var normalized = lastWord.Replace(".", "").ToLowerInvariant();
            if (normalized is "eg" or "ie" or "al")
                return true;
        }

        if (nextIndex < fullText.Length)
        {
            var nextChar = fullText[nextIndex];
            if (char.IsLower(nextChar))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 提取单词用于词典比对
    /// </summary>
    public static string NormalizeWord(string rawWord)
    {
        var cleaned = PunctuationCleanRegex().Replace(rawWord, "").Trim().ToLowerInvariant();
        return cleaned;
    }
}
