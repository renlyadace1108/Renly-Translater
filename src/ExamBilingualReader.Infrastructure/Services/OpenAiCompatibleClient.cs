using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExamBilingualReader.Core.Models.Config;
using ExamBilingualReader.Core.Models.Reader;

namespace ExamBilingualReader.Infrastructure.Services;

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public long LatencyMs { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IOpenAiClient
{
    Task<ConnectionTestResult> TestConnectionAsync(LlmProviderConfig config);
    Task<SentenceAnalysisResult> AnalyzeSentenceAsync(LlmProviderConfig config, string sentence, CancellationToken cancellationToken = default);
    Task<string> TranslateSentenceAsync(LlmProviderConfig config, string sentence, CancellationToken cancellationToken = default);
}

public class OpenAiCompatibleClient : IOpenAiClient
{
    public HttpClient CreateClient(LlmProviderConfig config)
    {
        var handler = new SocketsHttpHandler();

        if (config.Proxy.Enabled && !string.IsNullOrWhiteSpace(config.Proxy.Host))
        {
            var proxyUri = $"{config.Proxy.Type.ToString().ToLower()}://{config.Proxy.Host}:{config.Proxy.Port}";
            var webProxy = new WebProxy(proxyUri);
            if (!string.IsNullOrEmpty(config.Proxy.Username))
            {
                webProxy.Credentials = new NetworkCredential(config.Proxy.Username, config.Proxy.Password);
            }
            handler.Proxy = webProxy;
            handler.UseProxy = true;
        }

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
        };

        if (!string.IsNullOrWhiteSpace(config.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey.Trim());
        }

        return client;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(LlmProviderConfig config)
    {
        var sw = Stopwatch.StartNew();
        using var client = CreateClient(config);

        var endpoint = NormalizeEndpoint(config.BaseUrl, "/chat/completions");
        var requestPayload = new
        {
            model = config.ModelName,
            messages = new[]
            {
                new { role = "user", content = "hi" }
            },
            max_tokens = 5
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json")
            };

            using var response = await client.SendAsync(request);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new ConnectionTestResult
                {
                    Success = true,
                    LatencyMs = sw.ElapsedMilliseconds,
                    Message = $"连接成功！响应耗时: {sw.ElapsedMilliseconds} ms"
                };
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            var message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "【认证失败 (401)】API Key 无效或过期，请检查秘钥。",
                HttpStatusCode.NotFound => $"【地址未找到 (404)】Base URL 路径可能错误: {endpoint}",
                HttpStatusCode.TooManyRequests => "【请求过频 (429)】接口配额耗尽或请求超出频次限制。",
                HttpStatusCode.BadGateway => "【网关错误 (502)】上游模型供应商服务暂时不可用。",
                _ => $"【请求失败 ({(int)response.StatusCode})】{response.ReasonPhrase}\n详情: {errorBody}"
            };

            return new ConnectionTestResult { Success = false, LatencyMs = sw.ElapsedMilliseconds, Message = message };
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            return new ConnectionTestResult
            {
                Success = false,
                LatencyMs = sw.ElapsedMilliseconds,
                Message = $"【网络连接超时】在 {config.TimeoutSeconds} 秒内未收到服务器响应，请检查网络或代理配置。"
            };
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            return new ConnectionTestResult
            {
                Success = false,
                LatencyMs = sw.ElapsedMilliseconds,
                Message = $"【网络请求异常】无法建立连接: {ex.Message}"
            };
        }
    }

    public async Task<SentenceAnalysisResult> AnalyzeSentenceAsync(
        LlmProviderConfig config, 
        string sentence, 
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(config);
        var endpoint = NormalizeEndpoint(config.BaseUrl, "/chat/completions");

        var requestPayload = new
        {
            model = config.ModelName,
            temperature = config.Temperature,
            messages = new[]
            {
                new { role = "system", content = SyntaxAnalysisPromptTemplate.SystemPrompt },
                new { role = "user", content = SyntaxAnalysisPromptTemplate.BuildUserMessage(sentence) }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var cleanJson = ExtractJsonContent(content);
        return JsonSerializer.Deserialize<SentenceAnalysisResult>(cleanJson)
            ?? throw new InvalidOperationException("无法反序列化模型返回的长难句解析结果。");
    }

    public async Task<string> TranslateSentenceAsync(
        LlmProviderConfig config, 
        string sentence, 
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(config);
        var endpoint = NormalizeEndpoint(config.BaseUrl, "/chat/completions");

        var requestPayload = new
        {
            model = config.ModelName,
            temperature = 0.3,
            messages = new[]
            {
                new { role = "system", content = "你是一位专业的双语对照翻译助手。请将给出的英文句子直接翻译为地道、准确的中文。不要输出任何解释、拼音或额外内容。" },
                new { role = "user", content = sentence.Trim() }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?.Trim() ?? string.Empty;
    }

    private static string NormalizeEndpoint(string baseUrl, string path)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) 
            ? trimmed 
            : $"{trimmed}{path}";
    }

    private static string ExtractJsonContent(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text[7..];
        }
        else if (text.StartsWith("```"))
        {
            text = text[3..];
        }

        if (text.EndsWith("```"))
        {
            text = text[..^3];
        }
        return text.Trim();
    }
}
