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

    [RelayCommand]
    public async Task TestConnectionAsync()
    {
        if (SelectedProvider == null) return;

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
        if (SelectedProvider != null)
        {
            _configService.CurrentSettings.ActiveProviderId = SelectedProvider.Id;
        }
        _configService.CurrentSettings.Providers = Providers.ToList();
        await _configService.SaveAsync();
        TestStatus = "设置已保存！";
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
