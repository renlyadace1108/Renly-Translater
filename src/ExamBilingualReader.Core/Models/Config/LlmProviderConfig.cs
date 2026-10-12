namespace ExamBilingualReader.Core.Models.Config;

public enum ProxyType
{
    None,
    Http,
    Socks5
}

public class ProxyConfig
{
    public bool Enabled { get; set; } = false;
    public ProxyType Type { get; set; } = ProxyType.Http;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7890;
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class LlmProviderConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "DeepSeek";
    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "deepseek-chat";
    public int TimeoutSeconds { get; set; } = 30;
    public double Temperature { get; set; } = 0.2;
    public ProxyConfig Proxy { get; set; } = new();
}

public class AppSettings
{
    public string ActiveProviderId { get; set; } = "deepseek";
    public List<LlmProviderConfig> Providers { get; set; } = new();
    public string Theme { get; set; } = "System"; // Light, Dark, System
}
