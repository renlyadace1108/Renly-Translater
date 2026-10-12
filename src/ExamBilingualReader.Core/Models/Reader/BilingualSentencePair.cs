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

public class BilingualSentencePair : System.ComponentModel.INotifyPropertyChanged
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int OrderIndex { get; set; }
    public string EnglishSentence { get; set; } = string.Empty;

    private string _chineseTranslation = string.Empty;
    public string ChineseTranslation
    {
        get => _chineseTranslation;
        set
        {
            if (_chineseTranslation != value)
            {
                _chineseTranslation = value;
                OnPropertyChanged(nameof(ChineseTranslation));
            }
        }
    }

    public List<WordToken> Tokens { get; set; } = [];

    private AnalysisStatus _status = AnalysisStatus.NotStarted;
    public AnalysisStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
    }

    private SentenceAnalysisResult? _analysisResult;
    public SentenceAnalysisResult? AnalysisResult
    {
        get => _analysisResult;
        set
        {
            if (_analysisResult != value)
            {
                _analysisResult = value;
                OnPropertyChanged(nameof(AnalysisResult));
            }
        }
    }

    public string? ErrorMessage { get; set; }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
}

public class BilingualParagraph
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int ParagraphIndex { get; set; }
    public List<BilingualSentencePair> Sentences { get; set; } = [];
}
