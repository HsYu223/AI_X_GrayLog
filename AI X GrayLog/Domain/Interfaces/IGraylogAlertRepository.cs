using AI_X_GrayLog.Domain.Entities;

namespace AI_X_GrayLog.Domain.Interfaces;

/// <summary>
/// Graylog 告警儲存庫介面
/// </summary>
public interface IGraylogAlertRepository
{
    Task<GraylogAlert> AddAsync(GraylogAlert alert);
    Task<GraylogAlert?> GetByIdAsync(Guid id);
    Task<IEnumerable<GraylogAlert>> GetAllAsync();
    Task<IEnumerable<GraylogAlert>> GetByPriorityAsync(int minPriority);
}

