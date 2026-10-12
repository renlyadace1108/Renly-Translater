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
    public async Task SaveSettingsAsync()
    {
        if (SelectedProvider == null || string.IsNullOrWhiteSpace(SelectedProvider.ApiKey))
        {
            TestStatus = "⚠️ API Key 不能为空，请填写有效密钥后再保存！";
            return;
        }

        _configService.CurrentSettings.ActiveProviderId = SelectedProvider.Id;
        _configService.CurrentSettings.Providers = Providers.ToList();
        await _configService.SaveAsync();
        TestStatus = "设置已保存！";
        RequestClose?.Invoke();
    }

    [RelayCommand]
    public void AddCustomProvider()
    {
        var custom = new LlmProviderConfig
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "自定义 OpenAI 兼容接口",
            BaseUrl = "https://api.openai.com/v1",
            ModelName = "gpt-4o-mini"
        };
        Providers.Add(custom);
        SelectedProvider = custom;
    }
}
