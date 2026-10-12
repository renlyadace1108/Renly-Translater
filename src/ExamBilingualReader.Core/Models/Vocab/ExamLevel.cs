namespace ExamBilingualReader.Core.Models.Vocab;

[Flags]
public enum ExamLevel
{
    None = 0,
    ZK = 1 << 0,     // 中考
    GK = 1 << 1,     // 高考
    CET4 = 1 << 2,   // 大学英语四级
    CET6 = 1 << 3,   // 大学英语六级
    IELTS = 1 << 4,  // 雅思
    TOEFL = 1 << 5,  // 托福
    GRE = 1 << 6     // GRE
}

public static class ExamLevelHelper
{
    public static ExamLevel ParseTags(string? tagString)
    {
        if (string.IsNullOrWhiteSpace(tagString))
            return ExamLevel.None;

        var tags = tagString.Split([' ', ',', '/', ';'], StringSplitOptions.RemoveEmptyEntries);
        var result = ExamLevel.None;

        foreach (var tag in tags)
        {
            var clean = tag.Trim().ToLowerInvariant();
            result |= clean switch
            {
                "zk" => ExamLevel.ZK,
                "gk" => ExamLevel.GK,
                "cet4" or "cet-4" or "4" => ExamLevel.CET4,
                "cet6" or "cet-6" or "6" => ExamLevel.CET6,
                "ielts" or "ielts-l" or "ielts-r" => ExamLevel.IELTS,
                "toefl" => ExamLevel.TOEFL,
                "gre" => ExamLevel.GRE,
                _ => ExamLevel.None
            };
        }

        return result;
    }

    /// <summary>
    /// 获取当前考纲最高优先级级别（按 雅思/托福/GRE > 六级 > 四级 > 高考/中考）
    /// </summary>
    public static string GetPrimaryLevelName(ExamLevel level)
    {
        if (level.HasFlag(ExamLevel.IELTS)) return "IELTS";
        if (level.HasFlag(ExamLevel.TOEFL)) return "TOEFL";
        if (level.HasFlag(ExamLevel.GRE)) return "GRE";
        if (level.HasFlag(ExamLevel.CET6)) return "CET-6";
        if (level.HasFlag(ExamLevel.CET4)) return "CET-4";
        if (level.HasFlag(ExamLevel.GK)) return "高考";
        if (level.HasFlag(ExamLevel.ZK)) return "中考";
        return string.Empty;
    }

    /// <summary>
    /// 获取考纲对应的 Hex 标亮颜色
    /// </summary>
    public static string GetHighlightColorHex(ExamLevel level)
    {
        if (level.HasFlag(ExamLevel.IELTS) || level.HasFlag(ExamLevel.TOEFL) || level.HasFlag(ExamLevel.GRE))
            return "#FF7043"; // 橙红 (学术/雅思)

        if (level.HasFlag(ExamLevel.CET6))
            return "#FBC02D"; // 亮黄 (六级)

        if (level.HasFlag(ExamLevel.CET4))
            return "#66BB6A"; // 淡绿 (四级)

        return "#9E9E9E"; // 基础灰
    }
}
