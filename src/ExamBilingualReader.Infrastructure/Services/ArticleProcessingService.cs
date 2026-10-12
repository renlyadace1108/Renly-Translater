using System.Text.RegularExpressions;
using ExamBilingualReader.Core.Models.Reader;
using ExamBilingualReader.Core.Models.Vocab;
using ExamBilingualReader.Core.TextProcessing;
using ExamBilingualReader.Infrastructure.Data;

namespace ExamBilingualReader.Infrastructure.Services;

public interface IArticleProcessingService
{
    Task<List<BilingualParagraph>> ProcessRawArticleAsync(string rawEnglishArticle);
    Task EnrichSentencesWithVocabAsync(IEnumerable<BilingualSentencePair> sentences);
}

public partial class ArticleProcessingService : IArticleProcessingService
{
    private readonly IDictionaryRepository _dictionaryRepository;

    [GeneratedRegex(@"\b[a-zA-Z]+(?:'[a-zA-Z]+)?\b", RegexOptions.Compiled)]
    private static partial Regex WordTokenRegex();

    public ArticleProcessingService(IDictionaryRepository dictionaryRepository)
    {
        _dictionaryRepository = dictionaryRepository;
    }

    public async Task<List<BilingualParagraph>> ProcessRawArticleAsync(string rawEnglishArticle)
    {
        if (string.IsNullOrWhiteSpace(rawEnglishArticle))
            return [];

        // 按段落行切分
        var rawParagraphs = rawEnglishArticle.Split(
            ["\r\n\r\n", "\n\n", "\r\r"], 
            StringSplitOptions.RemoveEmptyEntries);

        var paragraphs = new List<BilingualParagraph>();
        int sentenceOrder = 0;

        for (int pIndex = 0; pIndex < rawParagraphs.Length; pIndex++)
        {
            var pText = rawParagraphs[pIndex].Trim();
            if (string.IsNullOrWhiteSpace(pText))
                continue;

            var sentences = SentenceSplitter.Split(pText);
            var sentencePairs = new List<BilingualSentencePair>();

            foreach (var s in sentences)
            {
                var pair = new BilingualSentencePair
                {
                    OrderIndex = ++sentenceOrder,
                    EnglishSentence = s,
                    ChineseTranslation = string.Empty, // 初始为空，支持逐句异步或批量 AI 翻译
                    Tokens = TokenizeSentence(s)
                };
                sentencePairs.Add(pair);
            }

            paragraphs.Add(new BilingualParagraph
            {
                ParagraphIndex = pIndex + 1,
                Sentences = sentencePairs
            });
        }

        // 统一提取所有句子的词汇，批量命中 SQLite 本地字典
        var allSentences = paragraphs.SelectMany(p => p.Sentences).ToList();
        await EnrichSentencesWithVocabAsync(allSentences);

        return paragraphs;
    }

    public async Task EnrichSentencesWithVocabAsync(IEnumerable<BilingualSentencePair> sentences)
    {
        var allTokens = sentences.SelectMany(s => s.Tokens).Where(t => t.IsWord).ToList();
        var uniqueWords = allTokens.Select(t => t.CleanWord).Distinct();

        var dictMap = await _dictionaryRepository.BatchLookupAsync(uniqueWords);

        foreach (var token in allTokens)
        {
            if (dictMap.TryGetValue(token.CleanWord, out var entry))
            {
                token.DictInfo = entry;
            }
        }
    }

    private static List<WordToken> TokenizeSentence(string sentence)
    {
        var tokens = new List<WordToken>();
        var matches = WordTokenRegex().Matches(sentence);
        int lastIndex = 0;

        foreach (Match match in matches)
        {
            // 添加单词前的标点/空格作为非词 Token
            if (match.Index > lastIndex)
            {
                tokens.Add(new WordToken
                {
                    RawText = sentence[lastIndex..match.Index],
                    CleanWord = string.Empty,
                    IsWord = false
                });
            }

            tokens.Add(new WordToken
            {
                RawText = match.Value,
                CleanWord = match.Value.ToLowerInvariant(),
                IsWord = true
            });

            lastIndex = match.Index + match.Length;
        }

        // 句末尾的标点或空格
        if (lastIndex < sentence.Length)
        {
            tokens.Add(new WordToken
            {
                RawText = sentence[lastIndex..],
                CleanWord = string.Empty,
                IsWord = false
            });
        }

        return tokens;
    }
}
