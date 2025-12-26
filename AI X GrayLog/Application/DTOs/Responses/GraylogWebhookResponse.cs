namespace AI_X_GrayLog.Application.DTOs.Responses;

/// <summary>
/// Graylog Webhook 回應 DTO
/// </summary>
public class GraylogWebhookResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 接收時間
    /// </summary>
    public DateTime ReceivedAt { get; set; }
    
    /// <summary>
    /// 事件 ID
    /// </summary>
    public string? EventId { get; set; }
    
    /// <summary>
    /// 告警 ID（系統內部）
    /// </summary>
    public Guid? AlertId { get; set; }
}

/// <summary>
/// Webhook 端點資訊回應 DTO
/// </summary>
public class WebhookInfoResponse
{
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public object? ExamplePayload { get; set; }
}

