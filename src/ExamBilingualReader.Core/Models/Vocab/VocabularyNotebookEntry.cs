namespace ExamBilingualReader.Core.Models.Vocab;

public class VocabularyNotebookEntry
{
    public long Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public string? Phonetic { get; set; }
    public string? Translation { get; set; }
    public string? LevelTag { get; set; }
    public string ContextSentence { get; set; } = string.Empty; // 摘录时的原文语境句子
    public string? ContextTranslation { get; set; }             // 原文句子的参考译文
    public DateTime AddedAt { get; set; } = DateTime.Now;
    public int ReviewCount { get; set; } = 0;                  // 复习次数
    public int MasteredLevel { get; set; } = 0;                // 掌握程度 (0-5)
}
