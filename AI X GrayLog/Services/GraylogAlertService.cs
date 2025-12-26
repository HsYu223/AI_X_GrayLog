using AI_X_GrayLog.Models;

namespace AI_X_GrayLog.Services;

/// <summary>
/// Graylog 告警處理服務
/// </summary>
public interface IGraylogAlertService
{
    /// <summary>
    /// 處理 Graylog Webhook 告警
    /// </summary>
    Task<GraylogWebhookResponse> ProcessAlertAsync(GraylogWebhookPayload payload);
}

/// <summary>
/// Graylog 告警處理服務實作
/// </summary>
public class GraylogAlertService : IGraylogAlertService
{
    private readonly ILogger<GraylogAlertService> _logger;

    public GraylogAlertService(ILogger<GraylogAlertService> logger)
    {
        _logger = logger;
    }

    public async Task<GraylogWebhookResponse> ProcessAlertAsync(GraylogWebhookPayload payload)
    {
        try
        {
            // 記錄接收到的告警
            _logger.LogInformation("收到 Graylog 告警: {EventDefinitionTitle}", 
                payload.EventDefinitionTitle);
            
            if (payload.Event != null)
            {
                _logger.LogInformation("事件 ID: {EventId}, 訊息: {Message}, 優先等級: {Priority}",
                    payload.Event.Id,
                    payload.Event.Message,
                    payload.Event.Priority);
                
                // 根據優先等級進行不同的處理
                if (payload.Event.Priority >= 2)
                {
                    _logger.LogWarning("高優先等級告警: {Message}", payload.Event.Message);
                    // 這裡可以加入發送通知、寄送郵件等邏輯
                }
                
                // 這裡可以加入您的業務邏輯
                // 例如：儲存到資料庫、發送通知、觸發其他流程等
                await Task.CompletedTask;
            }

            return new GraylogWebhookResponse
            {
                Success = true,
                Message = "告警已成功接收並處理",
                ReceivedAt = DateTime.UtcNow,
                EventId = payload.Event?.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "處理 Graylog 告警時發生錯誤");
            
            return new GraylogWebhookResponse
            {
                Success = false,
                Message = $"處理告警時發生錯誤: {ex.Message}",
                ReceivedAt = DateTime.UtcNow,
                EventId = payload.Event?.Id
            };
        }
    }
}

