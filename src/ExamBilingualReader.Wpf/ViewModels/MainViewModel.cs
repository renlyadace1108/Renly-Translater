using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamBilingualReader.Core.Models.Config;
using ExamBilingualReader.Core.Models.Reader;
using ExamBilingualReader.Infrastructure.Data;
using ExamBilingualReader.Infrastructure.Services;

namespace ExamBilingualReader.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppConfigService _configService;
    private readonly IOpenAiClient _llmClient;
    private readonly IDictionaryRepository _dictionaryRepo;
    private readonly IArticleProcessingService _articleProcessor;

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
        IArticleProcessingService articleProcessor)
    {
        _configService = configService;
        _llmClient = llmClient;
        _dictionaryRepo = dictionaryRepo;
        _articleProcessor = articleProcessor;
    }

    public async Task InitializeAsync()
    {
        await _configService.InitializeAsync();
        await _dictionaryRepo.InitializeDatabaseAsync();
        CurrentProvider = _configService.GetActiveProvider();
        await LoadArticleAsync();
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
}
