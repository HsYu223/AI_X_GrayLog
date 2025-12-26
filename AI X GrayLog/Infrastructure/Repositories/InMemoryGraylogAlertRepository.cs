using AI_X_GrayLog.Domain.Entities;
using AI_X_GrayLog.Domain.Interfaces;

namespace AI_X_GrayLog.Infrastructure.Repositories;

/// <summary>
/// Graylog 告警儲存庫實作（In-Memory）
/// </summary>
public class InMemoryGraylogAlertRepository : IGraylogAlertRepository
{
    private readonly List<GraylogAlert> _alerts = new();
    private readonly ILogger<InMemoryGraylogAlertRepository> _logger;

    public InMemoryGraylogAlertRepository(ILogger<InMemoryGraylogAlertRepository> logger)
    {
        _logger = logger;
    }

    public Task<GraylogAlert> AddAsync(GraylogAlert alert)
    {
        _alerts.Add(alert);
        _logger.LogInformation("告警已儲存至記憶體，ID: {AlertId}", alert.Id);
        return Task.FromResult(alert);
    }

    public Task<GraylogAlert?> GetByIdAsync(Guid id)
    {
        var alert = _alerts.FirstOrDefault(a => a.Id == id);
        return Task.FromResult(alert);
    }

    public Task<IEnumerable<GraylogAlert>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<GraylogAlert>>(_alerts);
    }

    public Task<IEnumerable<GraylogAlert>> GetByPriorityAsync(int minPriority)
    {
        var alerts = _alerts.Where(a => a.Priority >= minPriority);
        return Task.FromResult<IEnumerable<GraylogAlert>>(alerts);
    }
}

