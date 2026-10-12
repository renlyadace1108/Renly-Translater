using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamBilingualReader.Core.Models.Config;
using ExamBilingualReader.Infrastructure.Services;

namespace ExamBilingualReader.Wpf.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppConfigService _configService;
    private readonly IOpenAiClient _llmClient;

    [ObservableProperty]
    private ObservableCollection<LlmProviderConfig> _providers = [];

    [ObservableProperty]
    private LlmProviderConfig? _selectedProvider;

    [ObservableProperty]
    private string _testStatus = string.Empty;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private ReaderStyleConfig _readerStyle = new();

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public List<string> AvailableFontFamilies { get; } = [
        "Segoe UI, Microsoft YaHei, Calibri",
        "Microsoft YaHei",
        "SimSun",
        "KaiTi",
        "Georgia",
        "Times New Roman",
        "Calibri",
        "Arial",
        "Cascadia Code, Consolas"
    ];

    public List<string> AvailableFontWeights { get; } = [
        "Normal",
        "Medium",
        "SemiBold",
        "Bold"
    ];

    public SettingsViewModel(AppConfigService configService, IOpenAiClient llmClient)
    {
        _configService = configService;
        _llmClient = llmClient;
        LoadSettings();
    }

    private void LoadSettings()
    {
        Providers.Clear();
        foreach (var p in _configService.CurrentSettings.Providers)
        {
            Providers.Add(p);
        }

        SelectedProvider = Providers.FirstOrDefault(p => p.Id == _configService.CurrentSettings.ActiveProviderId)
                           ?? Providers.FirstOrDefault();

        ReaderStyle = _configService.CurrentSettings.ReaderStyle ?? new ReaderStyleConfig();
    }

    public event Action? RequestClose;

    [RelayCommand]
    public async Task TestConnectionAsync()
    {
        if (SelectedProvider == null) return;
        if (string.IsNullOrWhiteSpace(SelectedProvider.ApiKey))
        {
            TestStatus = "⚠️ 请先填写 API Key 再进行测试！";
            return;
        }

        IsTesting = true;
        TestStatus = "正在测试连接，请稍候...";

        try
        {
            var res = await _llmClient.TestConnectionAsync(SelectedProvider);
            TestStatus = res.Message;
        }
        catch (Exception ex)
        {
            TestStatus = $"连接发生异常: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    public void ApplyPreset(string presetKey)
    {
        if (SelectedProvider == null) return;
        switch (presetKey)
        {
            case "SiliconFlow-DeepSeek-V3":
                SelectedProvider.Name = "SiliconFlow DeepSeek-V3";
                SelectedProvider.BaseUrl = "https://api.siliconflow.cn/v1";
                SelectedProvider.ModelName = "deepseek-ai/DeepSeek-V3";
                TestStatus = "已切换为 DeepSeek-V3 预设（极强长难句拆解与句法分析能力）";
                break;
            case "SiliconFlow-Qwen-72B":
                SelectedProvider.Name = "SiliconFlow Qwen-72B";
                SelectedProvider.BaseUrl = "https://api.siliconflow.cn/v1";
                SelectedProvider.ModelName = "Qwen/Qwen2.5-72B-Instruct";
                TestStatus = "已切换为通义千问 72B 预设（高精度中文翻译与语法）";
                break;
            case "DeepSeek-Official":
                SelectedProvider.Name = "DeepSeek 官方 API";
                SelectedProvider.BaseUrl = "https://api.deepseek.com/v1";
                SelectedProvider.ModelName = "deepseek-chat";
                TestStatus = "已切换为 DeepSeek 官方接口预设";
                break;
            case "SiliconFlow-Qwen-7B-Free":
                SelectedProvider.Name = "SiliconFlow Qwen-7B (免费)";
                SelectedProvider.BaseUrl = "https://api.siliconflow.cn/v1";
                SelectedProvider.ModelName = "Qwen/Qwen2.5-7B-Instruct";
                TestStatus = "已切换为 Qwen-7B 免费预设";
                break;
        }
    }

    [RelayCommand]
    public void ApplyThemePreset(string presetKey)
    {
        ReaderStyle.ApplyPreset(presetKey);
    }

    [RelayCommand]
    public void ApplyFontWeight(string weight)
    {
        ReaderStyle.FontWeight = weight;
    }

    [RelayCommand]
    public void ApplyFontFamily(string family)
    {
        ReaderStyle.FontFamily = family;
    }

    [RelayCommand]
    public void ApplyTextColor(string color)
    {
        ReaderStyle.TextColor = color;
    }

    [RelayCommand]
    public void ApplyReadingBg(string color)
    {
        ReaderStyle.ReadingBackground = color;
    }

    [RelayCommand]
    public void ApplyPageBg(string color)
    {
        ReaderStyle.PageBackground = color;
    }

    [RelayCommand]
    public void IncreaseFontSize()
    {
        ReaderStyle.FontSize = Math.Min(32, ReaderStyle.FontSize + 1);
    }

    [RelayCommand]
    public void DecreaseFontSize()
    {
        ReaderStyle.FontSize = Math.Max(12, ReaderStyle.FontSize - 1);
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        if (SelectedProvider == null || string.IsNullOrWhiteSpace(SelectedProvider.ApiKey))
        {
            TestStatus = "⚠️ API Key 不能为空，请填写有效密钥后再保存！";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedProvider.ModelName))
        {
            TestStatus = "⚠️ 模型名称不能为空，请填写要使用的模型名称！";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedProvider.BaseUrl))
        {
            TestStatus = "⚠️ 接口地址 (Base URL) 不能为空！";
            return;
        }

        _configService.CurrentSettings.ActiveProviderId = SelectedProvider.Id;
        _configService.CurrentSettings.Providers = Providers.ToList();
        _configService.CurrentSettings.ReaderStyle = ReaderStyle;
        await _configService.SaveAsync();
        TestStatus = "设置已保存！";
        RequestClose?.Invoke();
    }
}
