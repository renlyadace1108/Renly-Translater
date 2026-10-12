namespace ExamBilingualReader.Core.Models.Vocab;

public class EcdictEntry
{
    public long Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public string? Phonetic { get; set; }
    public string? Definition { get; set; }
    public string? Translation { get; set; }
    public string? Pos { get; set; }
    public string? Tag { get; set; }
    public int? Collins { get; set; }
    public int? Oxford { get; set; }
    public string? Exchange { get; set; }
    public string? Detail { get; set; }
    public string? Audio { get; set; }

    // 运行时解析的考纲级别
    public ExamLevel Level => ExamLevelHelper.ParseTags(Tag);
    public string PrimaryLevelName => ExamLevelHelper.GetPrimaryLevelName(Level);
    public string ColorHex => ExamLevelHelper.GetHighlightColorHex(Level);

    // 格式化后的简要释义（便于 Tooltip 悬浮直接显示）
    public string DisplayTranslation
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Translation))
                return Definition ?? "暂无释义";
            return Translation.Replace("\\n", "\n").Trim();
        }
    }
}
