namespace AI_X_GrayLog.Models;

/// <summary>
/// Graylog Webhook 告警主要資料結構
/// </summary>
public class GraylogWebhookPayload
{
    /// <summary>
    /// 事件定義 ID
    /// </summary>
    public string? EventDefinitionId { get; set; }
    
    /// <summary>
    /// 事件定義類型
    /// </summary>
    public string? EventDefinitionType { get; set; }
    
    /// <summary>
    /// 事件定義標題
    /// </summary>
    public string? EventDefinitionTitle { get; set; }
    
    /// <summary>
    /// 事件定義描述
    /// </summary>
    public string? EventDefinitionDescription { get; set; }
    
    /// <summary>
    /// 工作流程定義 ID
    /// </summary>
    public string? JobDefinitionId { get; set; }
    
    /// <summary>
    /// 工作流程觸發 ID
    /// </summary>
    public string? JobTriggerId { get; set; }
    
    /// <summary>
    /// 事件資料
    /// </summary>
    public GraylogEvent? Event { get; set; }
    
    /// <summary>
    /// 回溯資料
    /// </summary>
    public List<object>? Backlog { get; set; }
}

/// <summary>
/// Graylog 事件詳細資料
/// </summary>
public class GraylogEvent
{
    /// <summary>
    /// 事件 ID
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// 事件定義 ID
    /// </summary>
    public string? EventDefinitionId { get; set; }
    
    /// <summary>
    /// 事件定義類型
    /// </summary>
    public string? EventDefinitionType { get; set; }
    
    /// <summary>
    /// 來源串流
    /// </summary>
    public List<string>? OriginContext { get; set; }
    
    /// <summary>
    /// 時間戳記
    /// </summary>
    public DateTime? Timestamp { get; set; }
    
    /// <summary>
    /// 時間範圍開始
    /// </summary>
    public DateTime? TimerangeStart { get; set; }
    
    /// <summary>
    /// 時間範圍結束
    /// </summary>
    public DateTime? TimerangeEnd { get; set; }
    
    /// <summary>
    /// 串流
    /// </summary>
    public List<string>? Streams { get; set; }
    
    /// <summary>
    /// 來源串流
    /// </summary>
    public List<string>? SourceStreams { get; set; }
    
    /// <summary>
    /// 訊息
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// 來源
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// 關鍵值欄位
    /// </summary>
    public List<string>? KeyTuple { get; set; }
    
    /// <summary>
    /// 優先等級
    /// </summary>
    public int? Priority { get; set; }
    
    /// <summary>
    /// 告警標記
    /// </summary>
    public bool? Alert { get; set; }
    
    /// <summary>
    /// 欄位資料
    /// </summary>
    public Dictionary<string, object>? Fields { get; set; }
}

/// <summary>
/// Graylog Webhook 回應
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
    public string? Message { get; set; }
    
    /// <summary>
    /// 接收時間
    /// </summary>
    public DateTime ReceivedAt { get; set; }
    
    /// <summary>
    /// 事件 ID
    /// </summary>
    public string? EventId { get; set; }
}

