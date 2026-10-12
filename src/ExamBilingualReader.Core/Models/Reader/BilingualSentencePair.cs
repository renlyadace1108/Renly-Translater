using ExamBilingualReader.Core.Models.Vocab;

namespace ExamBilingualReader.Core.Models.Reader;

public enum AnalysisStatus
{
    NotStarted,
    Analyzing,
    Completed,
    Failed
}

public class WordToken
{
    public string RawText { get; set; } = string.Empty;       // 原文包含前后标点
    public string CleanWord { get; set; } = string.Empty;     // 纯单词小写 (用于查字典)
    public bool IsWord { get; set; } = true;
    public EcdictEntry? DictInfo { get; set; }                // 匹配到的词典条目
    public bool HasExamHighlight => DictInfo != null && DictInfo.Level != ExamLevel.None;
}

public class BilingualSentencePair
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int OrderIndex { get; set; }
    public string EnglishSentence { get; set; } = string.Empty;
    public string ChineseTranslation { get; set; } = string.Empty;

    // 分词及考点标记序列（供 WPF ItemsControl / Inline 流式展示与悬浮卡片绑定）
    public List<WordToken> Tokens { get; set; } = [];

    // LLM 解析状态与结构化结果
    public AnalysisStatus Status { get; set; } = AnalysisStatus.NotStarted;
    public SentenceAnalysisResult? AnalysisResult { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BilingualParagraph
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int ParagraphIndex { get; set; }
    public List<BilingualSentencePair> Sentences { get; set; } = [];
}
