using AI_X_GrayLog.Application.DTOs.Requests;
using AI_X_GrayLog.Application.DTOs.Responses;
using AI_X_GrayLog.Application.Interfaces;
using AI_X_GrayLog.Domain.Entities;
using AI_X_GrayLog.Domain.Interfaces;
using System.Text.Json;

namespace AI_X_GrayLog.Application.Services;

/// <summary>
/// Graylog 告警應用服務實作
/// </summary>
public class GraylogAlertApplicationService : IGraylogAlertApplicationService
{
    private readonly IGraylogAlertRepository _repository;
    private readonly IAiAlertAnalysisService? _aiAnalysisService;
    private readonly ITeamsNotificationService? _teamsNotificationService;
    private readonly ILogger<GraylogAlertApplicationService> _logger;

    public GraylogAlertApplicationService(
        IGraylogAlertRepository repository,
        ILogger<GraylogAlertApplicationService> logger,
        IAiAlertAnalysisService? aiAnalysisService = null,
        ITeamsNotificationService? teamsNotificationService = null)
    {
        _repository = repository;
        _logger = logger;
        _aiAnalysisService = aiAnalysisService;
        _teamsNotificationService = teamsNotificationService;
    }

    public async Task<GraylogWebhookResponse> ProcessWebhookAsync(GraylogWebhookRequest request)
    {
        try
        {
            // 記錄接收到的告警
            _logger.LogInformation("收到 Graylog 告警: {EventDefinitionTitle}", 
                request.EventDefinitionTitle);
            
            if (request.Event != null)
            {
                _logger.LogInformation("事件 ID: {EventId}, 訊息: {Message}, 優先等級: {Priority}",
                    request.Event.Id,
                    request.Event.Message,
                    request.Event.Priority);
                
                // 根據優先等級進行不同的處理
                if (request.Event.Priority >= 2)
                {
                    _logger.LogWarning("高優先等級告警: {Message}", request.Event.Message);
                    
                    // 使用 AI 分析告警（如果已配置）
                    if (_aiAnalysisService != null)
                    {
                        try
                        {
                            _logger.LogInformation("開始 AI 分析告警...");
                            var aiAnalysis = await _aiAnalysisService.AnalyzeAlertAsync(request);
                            _logger.LogInformation("AI 分析完成");
                            
                            // 發送 AI 分析結果到 Microsoft Teams
                            if (_teamsNotificationService != null)
                            {
                                _logger.LogInformation("發送 AI 分析結果到 Microsoft Teams...");
                                var sent = await _teamsNotificationService.SendAiAnalysisToTeamsAsync(
                                    title: request.EventDefinitionTitle ?? "未知告警",
                                    aiAnalysis: aiAnalysis,
                                    eventId: request.Event.Id,
                                    priority: request.Event.Priority
                                );
                                
                                if (sent)
                                {
                                    _logger.LogInformation("成功發送 AI 分析結果到 Microsoft Teams");
                                }
                                else
                                {
                                    _logger.LogWarning("發送到 Microsoft Teams 失敗");
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Teams 通知服務未配置，AI 分析結果: {Analysis}", aiAnalysis);
                            }
                        }
                        catch (Exception aiEx)
                        {
                            _logger.LogError(aiEx, "AI 分析失敗，但繼續處理告警");
                        }
                    }
                }
                
                // 建立實體並儲存
                var alert = new GraylogAlert
                {
                    Id = Guid.NewGuid(),
                    EventId = request.Event.Id ?? string.Empty,
                    EventDefinitionId = request.EventDefinitionId ?? string.Empty,
                    EventDefinitionTitle = request.EventDefinitionTitle ?? string.Empty,
                    EventDefinitionDescription = request.EventDefinitionDescription,
                    Message = request.Event.Message ?? string.Empty,
                    Source = request.Event.Source,
                    Priority = request.Event.Priority ?? 0,
                    IsAlert = request.Event.Alert ?? false,
                    Timestamp = request.Event.Timestamp ?? DateTime.UtcNow,
                    ReceivedAt = DateTime.UtcNow,
                    RawPayload = JsonSerializer.Serialize(request)
                };

                var savedAlert = await _repository.AddAsync(alert);

                return new GraylogWebhookResponse
                {
                    Success = true,
                    Message = "告警已成功接收並處理",
                    ReceivedAt = DateTime.UtcNow,
                    EventId = request.Event.Id,
                    AlertId = savedAlert.Id
                };
            }

            return new GraylogWebhookResponse
            {
                Success = true,
                Message = "告警已接收，但缺少事件資料",
                ReceivedAt = DateTime.UtcNow
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
                EventId = request.Event?.Id
            };
        }
    }

    public WebhookInfoResponse GetWebhookInfo()
    {
        return new WebhookInfoResponse
        {
            Endpoint = "/api/graylog/webhook",
            Method = "POST",
            Description = "接收 Graylog Webhook 告警",
            ContentType = "application/json",
            ExamplePayload = new
            {
                event_definition_id = "example-id",
                event_definition_title = "測試告警",
                event_definition_description = "這是一個測試告警",
                @event = new
                {
                    id = "event-123",
                    message = "偵測到異常活動",
                    priority = 2,
                    timestamp = DateTime.UtcNow,
                    alert = true
                }
            }
        };
    }
}

