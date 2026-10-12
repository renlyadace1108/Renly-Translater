using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamBilingualReader.Core.Models.Config;
using ExamBilingualReader.Core.Models.ExamHub;
using ExamBilingualReader.Core.Models.Reader;
using ExamBilingualReader.Core.Models.Vocab;
using ExamBilingualReader.Infrastructure.Data;
using ExamBilingualReader.Infrastructure.Services;

namespace ExamBilingualReader.Wpf.ViewModels;

public enum ReadingZone
{
    GeneralArticle, // 英语学术/外刊文章区
    ExamPreparation // 四六级真题备考区
}

public partial class MainViewModel : ObservableObject
{
    private readonly AppConfigService _configService;
    private readonly IOpenAiClient _llmClient;
    private readonly IDictionaryRepository _dictionaryRepo;
    private readonly IArticleProcessingService _articleProcessor;
    private readonly IExamHubService _examHub;

    [ObservableProperty]
    private ReadingZone _currentZone = ReadingZone.ExamPreparation;

    [ObservableProperty]
    private ObservableCollection<GitHubExamPaperItem> _examPapers = [];

    [ObservableProperty]
    private GitHubExamPaperItem? _selectedExamPaper;

    [ObservableProperty]
    private string _examFilterCategory = "ALL"; // ALL, CET4, CET6

    [ObservableProperty]
    private string _articleInput = "According to Smith et al., cognitive load increases during complex academic tasks (e.g., reading dense texts). This is confirmed by Fig. 3 in the paper.\n\nSubstantial research indicates that the phenomenon of cognitive fatigue is ubiquitous, but proper strategies can mitigate its impact.";

    [ObservableProperty]
    private ObservableCollection<BilingualParagraph> _paragraphs = [];

    [ObservableProperty]
    private BilingualSentencePair? _selectedSentence;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    // 侧边栏/详情面板长难句结构解析数据
    [ObservableProperty]
    private SentenceAnalysisResult? _currentAnalysis;

    // 当前活跃 LLM 配置
    [ObservableProperty]
    private LlmProviderConfig? _currentProvider;

    public MainViewModel(
        AppConfigService configService,
        IOpenAiClient llmClient,
        IDictionaryRepository dictionaryRepo,
        IArticleProcessingService articleProcessor,
        IExamHubService examHub)
    {
        _configService = configService;
        _llmClient = llmClient;
        _dictionaryRepo = dictionaryRepo;
        _articleProcessor = articleProcessor;
        _examHub = examHub;
    }

    public async Task InitializeAsync()
    {
        await _configService.InitializeAsync();
        await _dictionaryRepo.InitializeDatabaseAsync();
        CurrentProvider = _configService.GetActiveProvider();
        await LoadExamPapersAsync();
        await LoadArticleAsync();
    }

    [RelayCommand]
    public void SwitchZone(ReadingZone zone)
    {
        CurrentZone = zone;
    }

    [RelayCommand]
    public async Task LoadExamPapersAsync()
    {
        try
        {
            var papers = await _examHub.FetchLatestExamPapersAsync();
            ExamPapers.Clear();
            foreach (var p in papers)
            {
                if (ExamFilterCategory == "ALL" || p.Category.Equals(ExamFilterCategory, StringComparison.OrdinalIgnoreCase))
                {
                    ExamPapers.Add(p);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载真题列表提示: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task FilterExamCategoryAsync(string category)
    {
        ExamFilterCategory = category;
        await LoadExamPapersAsync();
    }

    [RelayCommand]
    public async Task SelectExamPaperAsync(GitHubExamPaperItem paper)
    {
        if (paper == null) return;
        SelectedExamPaper = paper;
        IsBusy = true;
        StatusMessage = $"正在从 GitHub 拉取 [{paper.Title}] 真题全文...";
        try
        {
            var content = await _examHub.DownloadPaperContentAsync(paper.DownloadUrl);
            ArticleInput = content;
            await LoadArticleAsync();
            StatusMessage = $"已成功载入真题: {paper.Title}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"拉取真题失败: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task LoadArticleAsync()
    {
        if (string.IsNullOrWhiteSpace(ArticleInput))
            return;

        IsBusy = true;
        StatusMessage = "正在分句与匹配本地考纲词库...";
        try
        {
            var parsed = await _articleProcessor.ProcessRawArticleAsync(ArticleInput);
            Paragraphs.Clear();
            foreach (var p in parsed)
            {
                Paragraphs.Add(p);
            }

            StatusMessage = $"文章载入成功，共解析 {Paragraphs.SelectMany(p => p.Sentences).Count()} 个句子。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"载入失败: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SelectSentenceAsync(BilingualSentencePair sentence)
    {
        SelectedSentence = sentence;
        CurrentAnalysis = sentence.AnalysisResult;

        // 如果该句子尚未解析，且配置了 LLM，则自动或提示触发精读分析
        if (sentence.AnalysisResult == null && sentence.Status == AnalysisStatus.NotStarted)
        {
            await AnalyzeCurrentSentenceAsync();
        }
    }

    [RelayCommand]
    public async Task AnalyzeCurrentSentenceAsync()
    {
        if (SelectedSentence == null) return;
        var provider = CurrentProvider ?? _configService.GetActiveProvider();
        if (provider == null)
        {
            StatusMessage = "请先配置 LLM 供应商 API Key。";
            return;
        }

        SelectedSentence.Status = AnalysisStatus.Analyzing;
        IsBusy = true;
        StatusMessage = $"正在使用 [{provider.Name}] 精读分析长难句...";

        try
        {
            var result = await _llmClient.AnalyzeSentenceAsync(provider, SelectedSentence.EnglishSentence);
            SelectedSentence.AnalysisResult = result;
            SelectedSentence.ChineseTranslation = result.Translation;
            SelectedSentence.Status = AnalysisStatus.Completed;
            CurrentAnalysis = result;
            StatusMessage = "句子精读解析完成！";
        }
        catch (Exception ex)
        {
            SelectedSentence.Status = AnalysisStatus.Failed;
            SelectedSentence.ErrorMessage = ex.Message;
            StatusMessage = $"解析长难句失败: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task AddWordToNotebookAsync(WordToken token)
    {
        if (token?.DictInfo == null) return;

        var entry = new VocabularyNotebookEntry
        {
            Word = token.DictInfo.Word,
            Phonetic = token.DictInfo.Phonetic,
            Translation = token.DictInfo.DisplayTranslation,
            LevelTag = token.DictInfo.PrimaryLevelName,
            ContextSentence = SelectedSentence?.EnglishSentence ?? string.Empty,
            ContextTranslation = SelectedSentence?.ChineseTranslation ?? string.Empty,
            AddedAt = DateTime.Now
        };

        var success = await _dictionaryRepo.AddToNotebookAsync(entry);
        StatusMessage = success 
            ? $"已将单词 [{token.DictInfo.Word}] 收藏至生词本！" 
            : $"收藏失败。";
    }

    [RelayCommand]
    public async Task TranslateAllSentencesAsync()
    {
        var provider = CurrentProvider ?? _configService.GetActiveProvider();
        if (provider == null)
        {
            StatusMessage = "请先配置 LLM 供应商 API Key。";
            return;
        }

        var allSentences = Paragraphs.SelectMany(p => p.Sentences).Where(s => string.IsNullOrWhiteSpace(s.ChineseTranslation)).ToList();
        if (allSentences.Count == 0)
        {
            StatusMessage = "所有句子均已存在译文。";
            return;
        }

        IsBusy = true;
        int completed = 0;
        StatusMessage = $"开始批量翻译，共 {allSentences.Count} 句...";

        try
        {
            foreach (var sentence in allSentences)
            {
                sentence.Status = AnalysisStatus.Analyzing;
                try
                {
                    var translation = await _llmClient.TranslateSentenceAsync(provider, sentence.EnglishSentence);
                    sentence.ChineseTranslation = translation;
                    sentence.Status = AnalysisStatus.Completed;
                }
                catch
                {
                    sentence.Status = AnalysisStatus.Failed;
                }

                completed++;
                StatusMessage = $"批量翻译进度: {completed} / {allSentences.Count}";
            }
            StatusMessage = "整篇文章批量双语对照翻译完成！";
        }
        catch (Exception ex)
        {
            StatusMessage = $"批量翻译中断: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
