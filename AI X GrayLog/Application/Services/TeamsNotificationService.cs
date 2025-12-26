using AI_X_GrayLog.Application.Interfaces;
using System.Text;
using System.Text.Json;

namespace AI_X_GrayLog.Application.Services;

/// <summary>
/// Microsoft Teams 通知服務實作
/// </summary>
public class TeamsNotificationService : ITeamsNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TeamsNotificationService> _logger;
    private readonly string _teamsWebhookUrl;

    public TeamsNotificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TeamsNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _teamsWebhookUrl = configuration["Teams:WebhookUrl"] 
            ?? throw new InvalidOperationException("未配置 Teams Webhook URL");
    }

    /// <summary>
    /// 發送 AI 分析結果到 Microsoft Teams
    /// </summary>
    public async Task<bool> SendAiAnalysisToTeamsAsync(
        string title,
        string aiAnalysis,
        string? eventId = null,
        int? priority = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("準備發送 AI 分析結果到 Microsoft Teams");

            // 構建 Teams message with adaptive card attachment 格式
            var teamsMessage = new
            {
                type = "message",
                attachments = new[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        content = new
                        {
                            schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                            type = "AdaptiveCard",
                            version = "1.4",
                            body = new object[]
                            {
                                new
                                {
                                    type = "TextBlock",
                                    text = "🚨 Graylog 告警 AI 分析報告",
                                    weight = "Bolder",
                                    size = "Large",
                                    color = "Attention"
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = $"系統偵測到**高優先級告警**，AI 已完成分析：",
                                    wrap = true
                                },
                                new
                                {
                                    type = "FactSet",
                                    facts = new[]
                                    {
                                        new { title = "告警標題", value = title },
                                        new { title = "事件 ID", value = eventId ?? "N/A" },
                                        new { title = "優先等級", value = GetPriorityText(priority) },
                                        new { title = "分析時間", value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") }
                                    }
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = "AI 分析結果",
                                    weight = "Bolder",
                                    size = "Medium",
                                    separator = true
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = aiAnalysis,
                                    wrap = true
                                }
                            }
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(teamsMessage, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogDebug("Teams 訊息內容: {Content}", jsonContent);

            // 發送 HTTP POST 請求到 Power Automate webhook
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(_teamsWebhookUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("成功發送 AI 分析結果到 Microsoft Teams");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "發送到 Teams 失敗，狀態碼: {StatusCode}, 錯誤: {Error}",
                    response.StatusCode,
                    errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "發送 AI 分析結果到 Microsoft Teams 時發生錯誤");
            return false;
        }
    }

    /// <summary>
    /// 將優先等級轉換為文字描述
    /// </summary>
    private static string GetPriorityText(int? priority)
    {
        return priority switch
        {
            0 => "ℹ️ 資訊 (Information)",
            1 => "⚠️ 低 (Low)",
            2 => "🔶 中 (Normal)",
            3 => "🔴 高 (High)",
            _ => "❓ 未知"
        };
    }
}

