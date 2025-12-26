using AI_X_GrayLog.Application.DTOs.Requests;

namespace AI_X_GrayLog.Application.Interfaces;

/// <summary>
/// AI 告警分析服務介面
/// </summary>
public interface IAiAlertAnalysisService
{
    /// <summary>
    /// 分析 Graylog 告警並找出問題原因
    /// </summary>
    /// <param name="request">Graylog Webhook 請求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI 分析結果</returns>
    Task<string> AnalyzeAlertAsync(GraylogWebhookRequest request, CancellationToken cancellationToken = default);
}

