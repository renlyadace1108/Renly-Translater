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

    public LlmProviderConfig? GetActiveProvider()
    {
        return CurrentSettings.Providers.FirstOrDefault(p => p.Id == CurrentSettings.ActiveProviderId)
               ?? CurrentSettings.Providers.FirstOrDefault();
    }

    public static AppSettings CreateDefaultSettings()
    {
        var deepseek = new LlmProviderConfig
        {
            Id = "deepseek",
            Name = "DeepSeek",
            BaseUrl = "https://api.deepseek.com/v1",
            ModelName = "deepseek-chat"
        };

        var siliconflow = new LlmProviderConfig
        {
            Id = "siliconflow",
            Name = "SiliconFlow (硅基流动)",
            BaseUrl = "https://api.siliconflow.cn/v1",
            ModelName = "deepseek-ai/DeepSeek-V3"
        };

        var zhipu = new LlmProviderConfig
        {
            Id = "zhipu",
            Name = "智谱 (GLM-4-Flash)",
            BaseUrl = "https://open.bigmodel.cn/api/paas/v4",
            ModelName = "glm-4-flash"
        };

        var ollama = new LlmProviderConfig
        {
            Id = "ollama",
            Name = "本地 Ollama",
            BaseUrl = "http://localhost:11434/v1",
            ModelName = "qwen2.5:7b"
        };

        return new AppSettings
        {
            ActiveProviderId = deepseek.Id,
            Providers = [deepseek, siliconflow, zhipu, ollama]
        };
    }
}
