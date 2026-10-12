using System.Text.Json.Serialization;

namespace ExamBilingualReader.Core.Models.Reader;

public class SentenceAnalysisResult
{
    [JsonPropertyName("translation")]
    public string Translation { get; set; } = string.Empty;

    [JsonPropertyName("core_structure")]
    public CoreStructure Core { get; set; } = new();

    [JsonPropertyName("clause_analysis")]
    public List<ClauseItem> Clauses { get; set; } = [];

    [JsonPropertyName("exam_focus_notes")]
    public List<string> ExamFocusNotes { get; set; } = [];

    [JsonPropertyName("key_vocabularies")]
    public List<KeyVocabularyItem> KeyVocabularies { get; set; } = [];
}

public class CoreStructure
{
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("predicate")]
    public string Predicate { get; set; } = string.Empty;

    [JsonPropertyName("object_or_complement")]
    public string ObjectOrComplement { get; set; } = string.Empty;
}

public class ClauseItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("clause_content")]
    public string ClauseContent { get; set; } = string.Empty;

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;
}

public class KeyVocabularyItem
{
    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("synonym_substitutions")]
    public List<string> SynonymSubstitutions { get; set; } = [];

    [JsonPropertyName("exam_level")]
    public string ExamLevel { get; set; } = string.Empty;
}
