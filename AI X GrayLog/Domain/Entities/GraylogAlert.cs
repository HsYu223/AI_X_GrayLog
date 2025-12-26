namespace AI_X_GrayLog.Domain.Entities;

/// <summary>
/// Graylog 告警事件實體
/// </summary>
public class GraylogAlert
{
    public Guid Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventDefinitionId { get; set; } = string.Empty;
    public string EventDefinitionTitle { get; set; } = string.Empty;
    public string? EventDefinitionDescription { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public int Priority { get; set; }
    public bool IsAlert { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string? RawPayload { get; set; }
}

