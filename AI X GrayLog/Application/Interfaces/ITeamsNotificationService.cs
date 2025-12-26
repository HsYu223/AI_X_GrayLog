namespace AI_X_GrayLog.Application.Interfaces;

/// <summary>
/// Microsoft Teams 通知服務介面
/// </summary>
public interface ITeamsNotificationService
{
    /// <summary>
    /// 發送 AI 分析結果到 Microsoft Teams
    /// </summary>
    /// <param name="title">告警標題</param>
    /// <param name="aiAnalysis">AI 分析結果</param>
    /// <param name="eventId">事件 ID</param>
    /// <param name="priority">優先等級</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功發送</returns>
    Task<bool> SendAiAnalysisToTeamsAsync(
        string title, 
        string aiAnalysis, 
        string? eventId = null,
        int? priority = null,
        CancellationToken cancellationToken = default);
}

