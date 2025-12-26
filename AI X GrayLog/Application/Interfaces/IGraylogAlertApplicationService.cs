using AI_X_GrayLog.Application.DTOs.Requests;
using AI_X_GrayLog.Application.DTOs.Responses;

namespace AI_X_GrayLog.Application.Interfaces;

/// <summary>
/// Graylog 告警應用服務介面
/// </summary>
public interface IGraylogAlertApplicationService
{
    /// <summary>
    /// 處理 Graylog Webhook 告警
    /// </summary>
    Task<GraylogWebhookResponse> ProcessWebhookAsync(GraylogWebhookRequest request);
    
    /// <summary>
    /// 取得 Webhook 資訊
    /// </summary>
    WebhookInfoResponse GetWebhookInfo();
}

