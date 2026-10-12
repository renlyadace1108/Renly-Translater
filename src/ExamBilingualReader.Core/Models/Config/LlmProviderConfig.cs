using System.ComponentModel;
using System.Runtime.CompilerServices;

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

public class LlmProviderConfig : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "自定义模型";
    private string _baseUrl = "https://api.siliconflow.cn/v1";
    private string _apiKey = string.Empty;
    private string _modelName = "deepseek-ai/DeepSeek-V3";
    private int _timeoutSeconds = 30;
    private double _temperature = 0.2;
    private ProxyConfig _proxy = new();

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string BaseUrl
    {
        get => _baseUrl;
        set => SetField(ref _baseUrl, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetField(ref _apiKey, value);
    }

    public string ModelName
    {
        get => _modelName;
        set => SetField(ref _modelName, value);
    }

    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set => SetField(ref _timeoutSeconds, value);
    }

    public double Temperature
    {
        get => _temperature;
        set => SetField(ref _temperature, value);
    }

    public ProxyConfig Proxy
    {
        get => _proxy;
        set => SetField(ref _proxy, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class AppSettings
{
    public string ActiveProviderId { get; set; } = "custom_llm";
    public List<LlmProviderConfig> Providers { get; set; } = new();
    public string Theme { get; set; } = "System"; // Light, Dark, System
    public ReaderStyleConfig ReaderStyle { get; set; } = new();
}
