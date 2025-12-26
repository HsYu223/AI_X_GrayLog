using AI_X_GrayLog.Application.DTOs.Requests;
using AI_X_GrayLog.Application.DTOs.Responses;
using AI_X_GrayLog.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AI_X_GrayLog.Presentation.Controllers;

/// <summary>
/// Graylog Webhook API 控制器
/// </summary>
[ApiController]
[Route("api/graylog")]
[Produces("application/json")]
public class GraylogController : ControllerBase
{
    private readonly IGraylogAlertApplicationService _alertService;
    private readonly IAiAlertAnalysisService? _aiAnalysisService;
    private readonly ILogger<GraylogController> _logger;

    public GraylogController(
        IGraylogAlertApplicationService alertService,
        ILogger<GraylogController> logger,
        IAiAlertAnalysisService? aiAnalysisService = null)
    {
        _alertService = alertService;
        _logger = logger;
        _aiAnalysisService = aiAnalysisService;
    }

    /// <summary>
    /// 接收 Graylog Webhook 告警
    /// </summary>
    /// <param name="request">Graylog Webhook 請求資料</param>
    /// <returns>處理結果</returns>
    /// <response code="200">告警處理成功</response>
    /// <response code="400">請求資料無效</response>
    /// <response code="500">伺服器內部錯誤</response>
    [HttpPost("webhook")]
    [ProducesResponseType(typeof(GraylogWebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GraylogWebhookResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GraylogWebhookResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReceiveWebhook([FromBody] GraylogWebhookRequest request)
    {
        _logger.LogInformation("收到 Graylog Webhook 請求");

        if (request == null)
        {
            _logger.LogWarning("收到空的請求內容");
            return BadRequest(new GraylogWebhookResponse
            {
                Success = false,
                Message = "請求內容不能為空",
                ReceivedAt = DateTime.UtcNow
            });
        }

        var response = await _alertService.ProcessWebhookAsync(request);

        if (response.Success)
        {
            return Ok(response);
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// 取得 Webhook 端點資訊
    /// </summary>
    /// <returns>Webhook 設定資訊和範例</returns>
    /// <response code="200">成功取得資訊</response>
    [HttpGet("webhook/info")]
    [ProducesResponseType(typeof(WebhookInfoResponse), StatusCodes.Status200OK)]
    public IActionResult GetWebhookInfo()
    {
        var info = _alertService.GetWebhookInfo();
        return Ok(info);
    }

    /// <summary>
    /// 使用 AI 分析 Graylog Webhook 告警
    /// </summary>
    /// <param name="request">Graylog Webhook 請求資料</param>
    /// <returns>AI 分析結果</returns>
    /// <response code="200">分析成功</response>
    /// <response code="400">請求資料無效</response>
    /// <response code="503">AI 服務未配置</response>
    [HttpPost("webhook/analyze")]
    [ProducesResponseType(typeof(AiAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AnalyzeWebhook([FromBody] GraylogWebhookRequest request)
    {
        _logger.LogInformation("收到 AI 分析請求");

        if (request == null)
        {
            _logger.LogWarning("收到空的請求內容");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "請求資料無效",
                Detail = "請求內容不能為空"
            });
        }

        if (_aiAnalysisService == null)
        {
            _logger.LogWarning("AI 服務未配置");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "AI 服務未配置",
                Detail = "請在 appsettings.json 中配置 OpenAI API Key"
            });
        }

        try
        {
            var analysis = await _aiAnalysisService.AnalyzeAlertAsync(request);
            
            return Ok(new AiAnalysisResponse
            {
                Success = true,
                Analysis = analysis,
                AnalyzedAt = DateTime.UtcNow,
                EventTitle = request.EventDefinitionTitle
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI 分析過程中發生錯誤");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "AI 分析失敗",
                Detail = ex.Message
            });
        }
    }
}

/// <summary>
/// AI 分析回應
/// </summary>
public class AiAnalysisResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// AI 分析結果
    /// </summary>
    public string? Analysis { get; set; }
    
    /// <summary>
    /// 分析時間
    /// </summary>
    public DateTime AnalyzedAt { get; set; }
    
    /// <summary>
    /// 事件標題
    /// </summary>
    public string? EventTitle { get; set; }
}

