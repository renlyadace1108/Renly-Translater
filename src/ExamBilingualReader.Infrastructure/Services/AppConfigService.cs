using System.IO;
using System.Text.Json;
using ExamBilingualReader.Core.Models.Config;

namespace ExamBilingualReader.Infrastructure.Services;

public class AppConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "ExamBilingualReader");
    
    private static readonly string ConfigFilePath = Path.Combine(ConfigDir, "config.json");
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public AppSettings CurrentSettings { get; private set; } = new();

    public async Task InitializeAsync()
    {
        if (!Directory.Exists(ConfigDir))
            Directory.CreateDirectory(ConfigDir);

        if (File.Exists(ConfigFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(ConfigFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (loaded != null && loaded.Providers.Count > 0)
                {
                    CurrentSettings = loaded;
                    CurrentSettings.ReaderStyle ??= new ReaderStyleConfig();
                    var active = CurrentSettings.Providers.FirstOrDefault(p => p.Id == CurrentSettings.ActiveProviderId)
                                 ?? CurrentSettings.Providers.First();
                    CurrentSettings.ActiveProviderId = active.Id;
                    return;
                }
            }
            catch
            {
                // 读取失败则回退默认
            }
        }

        CurrentSettings = CreateDefaultSettings();
        await SaveAsync();
    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(CurrentSettings, _jsonOptions);
        await File.WriteAllTextAsync(ConfigFilePath, json);
    }

    public LlmProviderConfig GetActiveProvider()
    {
        return CurrentSettings.Providers.FirstOrDefault(p => p.Id == CurrentSettings.ActiveProviderId)
               ?? CurrentSettings.Providers.FirstOrDefault()
               ?? CreateDefaultProvider();
    }

    public static LlmProviderConfig CreateDefaultProvider()
    {
        return new LlmProviderConfig
        {
            Id = "custom_llm",
            Name = "自定义 LLM 模型",
            BaseUrl = "https://api.siliconflow.cn/v1",
            ModelName = "deepseek-ai/DeepSeek-V3",
            Temperature = 0.2
        };
    }

    public static AppSettings CreateDefaultSettings()
    {
        var defaultProvider = CreateDefaultProvider();

        return new AppSettings
        {
            ActiveProviderId = defaultProvider.Id,
            Providers = [defaultProvider]
        };
    }
}
