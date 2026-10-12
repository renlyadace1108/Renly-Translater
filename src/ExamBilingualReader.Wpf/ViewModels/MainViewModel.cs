using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamBilingualReader.Core.Models.Config;
using ExamBilingualReader.Core.Models.ExamHub;
using ExamBilingualReader.Core.Models.Reader;
using ExamBilingualReader.Core.Models.Vocab;
using ExamBilingualReader.Core.TextProcessing;
using ExamBilingualReader.Infrastructure.Data;
using ExamBilingualReader.Infrastructure.Services;

namespace ExamBilingualReader.Wpf.ViewModels;

public enum ReadingZone
{
    GeneralArticle, // 英语学术/外刊文章区
    ExamPreparation // 四六级真题备考区
}

public enum ReadingLayoutMode
{
    FullArticleContinuous, // 原文整块连贯沉浸展示 (支持任意鼠标划词拖选分析)
    ContinuousParagraph,   // 自然段落考纲精读 (带生词悬浮与考纲下划线)
    SentenceBySentence     // 逐句对照卡片模式
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
    private ReadingLayoutMode _layoutMode = ReadingLayoutMode.FullArticleContinuous;

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
    private ObservableCollection<BilingualSentencePair> _flatSentences = [];

    [ObservableProperty]
    private bool _isExamDrawerOpen = true; // 抽屉默认展开，可随时收起

    [ObservableProperty]
    private BilingualSentencePair? _selectedSentence;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    // 侧边栏/详情面板长难句结构解析数据
    [ObservableProperty]
    private SentenceAnalysisResult? _currentAnalysis;

    // 侧边栏词汇即时查询结果
    [ObservableProperty]
    private EcdictEntry? _quickLookupResult;

    [ObservableProperty]
    private string _quickLookupWord = string.Empty;

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
    public void ToggleLayoutMode()
    {
        LayoutMode = LayoutMode switch
        {
            ReadingLayoutMode.FullArticleContinuous => ReadingLayoutMode.ContinuousParagraph,
            ReadingLayoutMode.ContinuousParagraph => ReadingLayoutMode.SentenceBySentence,
            _ => ReadingLayoutMode.FullArticleContinuous
        };
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
            // 将耗时的文章分句与词库比对放到后台线程，确保 UI 线程完全不卡死
            var parsed = await Task.Run(async () => await _articleProcessor.ProcessRawArticleAsync(ArticleInput));

            Paragraphs.Clear();
            FlatSentences.Clear();

            var flatList = new List<BilingualSentencePair>();
            foreach (var p in parsed)
            {
                Paragraphs.Add(p);
                flatList.AddRange(p.Sentences);
            }

            foreach (var s in flatList)
            {
                FlatSentences.Add(s);
            }

            // 主动触发轻量 GC 释放临时大量分词字符串，降低内存驻留
            GC.Collect(1, GCCollectionMode.Optimized, false);

            StatusMessage = $"文章载入成功，共解析 {FlatSentences.Count} 个句子。";
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
    public void ToggleExamDrawer()
    {
        IsExamDrawerOpen = !IsExamDrawerOpen;
    }

    [RelayCommand]
    public void SelectSentence(BilingualSentencePair sentence)
    {
        if (sentence == null) return;
        SelectedSentence = sentence;
        CurrentAnalysis = sentence.AnalysisResult;
        StatusMessage = $"已选中第 {sentence.OrderIndex} 句。可点击右侧「精读分析此句」进行深度学术拆解。";
    }

    [RelayCommand]
    public void SelectCustomText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = text.Trim();

        // 查找是否匹配现存句子，如果没有则创建一个临时选中项
        var match = FlatSentences.FirstOrDefault(s => s.EnglishSentence.Trim() == text);
        if (match != null)
        {
            SelectedSentence = match;
            CurrentAnalysis = match.AnalysisResult;
        }
        else
        {
            var customPair = new BilingualSentencePair
            {
                OrderIndex = 0,
                EnglishSentence = text
            };
            SelectedSentence = customPair;
            CurrentAnalysis = null;
        }

        StatusMessage = $"已拖选文本 ({text.Length} 字符)。点击右侧「精读分析此句」开始 AI 结构拆解。";
    }

    [RelayCommand]
    public async Task AnalyzeCurrentSentenceAsync()
    {
        if (SelectedSentence == null) return;
        if (SelectedSentence.Status == AnalysisStatus.Analyzing) return; // 防重复点击

        var provider = CurrentProvider ?? _configService.GetActiveProvider();
        if (provider == null || string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            StatusMessage = "提示: 请先在「模型设置」中配置 API Key 后再使用 AI 精读功能。";
            return;
        }

        SelectedSentence.Status = AnalysisStatus.Analyzing;
        IsBusy = true;
        StatusMessage = $"正在通过 [{provider.Name}] 精读分析第 {SelectedSentence.OrderIndex} 句...";

        try
        {
            var result = await Task.Run(async () => 
                await _llmClient.AnalyzeSentenceAsync(provider, SelectedSentence.EnglishSentence));

            SelectedSentence.AnalysisResult = result;
            if (string.IsNullOrWhiteSpace(SelectedSentence.ChineseTranslation))
            {
                SelectedSentence.ChineseTranslation = result.Translation;
            }
            SelectedSentence.Status = AnalysisStatus.Completed;
            CurrentAnalysis = result;
            StatusMessage = "句子学术精读分析完成！";
        }
        catch (Exception ex)
        {
            SelectedSentence.Status = AnalysisStatus.Failed;
            SelectedSentence.ErrorMessage = ex.Message;
            StatusMessage = $"长难句解析失败: {ex.Message}";
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
    public async Task LookupWordAsync(string? rawWord)
    {
        if (string.IsNullOrWhiteSpace(rawWord)) return;

        var clean = SentenceSplitter.NormalizeWord(rawWord);
        if (string.IsNullOrWhiteSpace(clean)) return;

        QuickLookupWord = clean;
        var entry = await _dictionaryRepo.FindWordAsync(clean);
        if (entry != null)
        {
            QuickLookupResult = entry;
            StatusMessage = $"查词成功: [{entry.Word}] {entry.PrimaryLevelName}";
        }
        else
        {
            QuickLookupResult = new EcdictEntry
            {
                Word = clean,
                Translation = "本地词典暂未收录该单词详细条目"
            };
            StatusMessage = $"未在本地词库中找到 [{clean}]";
        }
    }

    [RelayCommand]
    public async Task AddQuickLookupToNotebookAsync()
    {
        if (QuickLookupResult == null || string.IsNullOrWhiteSpace(QuickLookupResult.Word)) return;

        var entry = new VocabularyNotebookEntry
        {
            Word = QuickLookupResult.Word,
            Phonetic = QuickLookupResult.Phonetic,
            Translation = QuickLookupResult.DisplayTranslation,
            LevelTag = QuickLookupResult.PrimaryLevelName,
            ContextSentence = SelectedSentence?.EnglishSentence ?? string.Empty,
            ContextTranslation = SelectedSentence?.ChineseTranslation ?? string.Empty,
            AddedAt = DateTime.Now
        };

        var success = await _dictionaryRepo.AddToNotebookAsync(entry);
        StatusMessage = success 
            ? $"已将单词 [{QuickLookupResult.Word}] 收藏至生词本！" 
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
