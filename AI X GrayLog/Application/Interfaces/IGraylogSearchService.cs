namespace AI_X_GrayLog.Application.Interfaces;

/// <summary>
/// Graylog 搜尋服務介面
/// </summary>
public interface IGraylogSearchService
{
    /// <summary>
    /// 搜尋 Graylog 日誌
    /// </summary>
    /// <param name="queryString">查詢字串 (Elasticsearch query)</param>
    /// <param name="timeRangeSeconds">時間範圍（秒），預設 60 秒</param>
    /// <param name="limit">限制結果數量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜尋結果的訊息列表</returns>
    Task<List<Dictionary<string, object>>> SearchLogsAsync(
        string queryString,
        int timeRangeSeconds = 60,
        int limit = 20,
        CancellationToken cancellationToken = default);
}

