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
                var existingKey = loaded?.Providers.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ApiKey))?.ApiKey ?? string.Empty;
                var singleProvider = CreateDefaultProvider();
                if (!string.IsNullOrWhiteSpace(existingKey))
                {
                    singleProvider.ApiKey = existingKey;
                }
                CurrentSettings = new AppSettings
                {
                    ActiveProviderId = singleProvider.Id,
                    Providers = [singleProvider]
                };
                await SaveAsync();
                return;
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

    public LlmProviderConfig? GetActiveProvider()
    {
        return CurrentSettings.Providers.FirstOrDefault(p => p.Id == "siliconflow_qwen7b")
               ?? CurrentSettings.Providers.FirstOrDefault()
               ?? CreateDefaultProvider();
    }

    public static LlmProviderConfig CreateDefaultProvider()
    {
        return new LlmProviderConfig
        {
            Id = "siliconflow_qwen7b",
            Name = "SiliconFlow - Qwen2.5-7B-Instruct (永久免费)",
            BaseUrl = "https://api.siliconflow.cn/v1",
            ModelName = "Qwen/Qwen2.5-7B-Instruct",
            Temperature = 0.2
        };
    }

    public static AppSettings CreateDefaultSettings()
    {
        var qwenFree = CreateDefaultProvider();

        return new AppSettings
        {
            ActiveProviderId = qwenFree.Id,
            Providers = [qwenFree]
        };
    }
}
