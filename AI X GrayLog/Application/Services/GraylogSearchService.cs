using AI_X_GrayLog.Application.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AI_X_GrayLog.Application.Services;

/// <summary>
/// Graylog 搜尋服務實作
/// </summary>
public class GraylogSearchService : IGraylogSearchService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GraylogSearchService> _logger;
    
    private readonly string _graylogUrl;
    private readonly string _graylogUsername;
    private readonly string _graylogPassword;

    public GraylogSearchService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GraylogSearchService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        
        _graylogUrl = configuration["Graylog:Url"] ?? "http://localhost:9000";
        _graylogUsername = configuration["Graylog:Username"] ?? "";
        _graylogPassword = configuration["Graylog:Password"] ?? "";
    }

    public async Task<List<Dictionary<string, object>>> SearchLogsAsync(
        string queryString,
        int timeRangeSeconds = 60,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("搜尋 Graylog 日誌: {Query}, 時間範圍: {Seconds} 秒", queryString, timeRangeSeconds);

            var httpClient = _httpClientFactory.CreateClient();
            
            // 設定 Basic Authentication
            var authBytes = Encoding.UTF8.GetBytes($"{_graylogUsername}:{_graylogPassword}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", 
                Convert.ToBase64String(authBytes));
            
            // 設定 X-Requested-By header (Graylog API 必需)
            var searchUrl = $"{_graylogUrl}/api/views/search/sync";
            httpClient.DefaultRequestHeaders.Add("X-Requested-By", searchUrl);

            // 構建搜尋請求
            var searchRequest = new
            {
                queries = new[]
                {
                    new
                    {
                        id = "query_id",
                        timerange = new
                        {
                            type = "relative",
                            range = timeRangeSeconds
                        },
                        query = new
                        {
                            type = "elasticsearch",
                            query_string = queryString
                        },
                        search_types = new[]
                        {
                            new
                            {
                                id = "messages_id",
                                type = "messages",
                                offset = 0,
                                limit,
                                sort = new[]
                                {
                                    new
                                    {
                                        field = "timestamp",
                                        order = "DESC"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var requestJson = JsonSerializer.Serialize(searchRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync(searchUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Graylog 搜尋失敗: {StatusCode}, {Error}", response.StatusCode, errorContent);
                return new List<Dictionary<string, object>>();
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResult = JsonSerializer.Deserialize<JsonElement>(responseJson);

            // 解析搜尋結果
            var messages = new List<Dictionary<string, object>>();
            
            if (searchResult.TryGetProperty("results", out var results) &&
                results.TryGetProperty("query_id", out var queryResult) &&
                queryResult.TryGetProperty("search_types", out var searchTypes) &&
                searchTypes.TryGetProperty("messages_id", out var messagesResult) &&
                messagesResult.TryGetProperty("messages", out var messagesArray))
            {
                foreach (var messageItem in messagesArray.EnumerateArray())
                {
                    if (messageItem.TryGetProperty("message", out var messageObj))
                    {
                        var messageDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            messageObj.GetRawText());
                        
                        if (messageDict != null)
                        {
                            messages.Add(messageDict);
                        }
                    }
                }
            }

            _logger.LogInformation("找到 {Count} 筆 Graylog 日誌", messages.Count);
            
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜尋 Graylog 日誌時發生錯誤");
            return new List<Dictionary<string, object>>();
        }
    }
}

