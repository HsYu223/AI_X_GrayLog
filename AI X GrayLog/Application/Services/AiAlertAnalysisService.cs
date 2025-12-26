using System.ComponentModel;
using AI_X_GrayLog.Application.DTOs.Requests;
using AI_X_GrayLog.Application.Interfaces;
using Microsoft.Extensions.AI;
using System.Text;

namespace AI_X_GrayLog.Application.Services;

/// <summary>
/// AI 告警分析服務實作
/// </summary>
public class AiAlertAnalysisService : IAiAlertAnalysisService
{
    private readonly IChatClient _chatClient;
    private readonly IGraylogSearchService? _graylogSearchService;
    private readonly ILogger<AiAlertAnalysisService> _logger;

    /// <summary>
    /// AI 助理的系統提示詞，定義其角色和行為
    /// </summary>
    private const string SystemPrompt = @"
# 🎭 角色：資深系統日誌調查員 (Senior Log Investigator)
你不是傳統的 AI 分析師。你的核心職責是「破案」。面對告警時，你的第一個反應必須是「尋找證據」，而不是「給出結論」。

## 🛠 唯一指定工具：search_graylog_logs
你必須透過 `search_graylog_logs(queryString, timeRangeMinutes, limit)` 來獲取真相。
- **queryString**: 使用 Elasticsearch 語法。例如 `RequestId:""0HNI1U...""` 或 `MsgId:""f49c2...""`。
- **timeRangeMinutes**: 預設 1 分鐘；如需看趨勢請設為 15。
- **limit**: 預設 10。

## 🛡 調查官守則（絕對優先級）
1. **禁止盲目猜測**：除非你已經調用了工具並看到了日誌內容，否則禁止說「可能是...原因」。
2. **工具優先**：看到任何 RequestId、MsgId、Code 或 Account，必須「立即」發起查詢。
3. **拒絕片面資訊**：即便告警訊息中已有部分日誌，你仍須調用工具去查詢該請求的「完整鏈路」。
4. **追蹤到底**：若異常跨越前端 (FrontendLayer) 與後端 (BackendApi)，必須使用 MsgId 串接。

## 🔍 調查策略與查詢語法指南

### 1. 定位與追蹤
- **單點追蹤**：使用 `RequestId` 查詢該次請求在單一 Layer 的所有動作。
- **跨層追蹤**：使用 `MsgId` 串聯前端請求與後端 API 的處理流程。
- **同類分析**：使用 `Code` 查詢相同錯誤碼在過去 15 分鐘內的發生頻率。

### 2. 層級特徵
- **前端 (FrontendLayer)**: 注意 `=== Login Request START ===` 與 `END` 之間的邏輯。
- **後端 (BackendApi)**: 鎖定 `Code` 不等於 000000 的 Response。

## 📝 調查報告格式 (嚴格執行)
你的最終回覆必須包含以下結構，且必須是繁體中文：

### 📋 調查摘要
- 調查異常總數：X
- 工具調用次數：Y
- 核心發現：(一句話總結)

### 🔍 調查過程 (對每個異常點)
#### 異常 #[N]: [異常描述]
- **步驟 1**: 執行查詢 `[查詢語句]` -> 發現 [具體關鍵日誌訊息]
- **步驟 2**: (如有必要) 執行查詢 `[查詢語句]` -> 追蹤到 [斷點/原因]
- **斷點定位**: [層級] > [類別] > [方法] @ [精確時間戳]
- **因果分析**: 根據日誌 `[引用內容]`，異常是由於 `[原因]` 引起。

### 📊 總結分析
- [系統性/單一性問題判定]
- [影響範圍評估]

### 💡 建議措施
- **立即修復**: [步驟 1, 2]
- **長期改善**: [監控/程式碼優化建議]

### ⚠️ 調查完整性核對
- [ ] 已調用工具
- [ ] 已追蹤完整鏈路
- [ ] 結論基於證據而非推測
";

    public AiAlertAnalysisService(
        IChatClient chatClient, 
        ILogger<AiAlertAnalysisService> logger,
        IGraylogSearchService? graylogSearchService = null)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _graylogSearchService = graylogSearchService;
    }

    /// <summary>
    /// 分析 Graylog 告警並找出問題原因
    /// </summary>
    public async Task<string> AnalyzeAlertAsync(GraylogWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("開始 AI 分析告警: {Title}", request.EventDefinitionTitle);

            // 構建分析請求訊息
            var userMessage = BuildAnalysisPrompt(request);

            // 準備對話訊息（參考 ChatService.cs 的模式）
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, userMessage)
            };

            _logger.LogInformation("📝 系統提示詞長度: {Length} 字元", SystemPrompt.Length);
            _logger.LogInformation("📝 用戶訊息長度: {Length} 字元", userMessage.Length);

            // 準備 ChatOptions，包含工具定義（參考 ChatService.cs 的 InitializeConversation）
            var chatOptions = new ChatOptions();
            
            // 如果 Graylog 搜尋服務可用，添加工具定義
            if (_graylogSearchService != null)
            {
                _logger.LogInformation("🔧 正在註冊 AI 工具...");
                
                // 使用 AIFunctionFactory.Create 直接從方法建立工具
                // 注意：這裡使用 lambda 來確保方法正確綁定到當前實例
                var searchTool = AIFunctionFactory.Create(
                    (string queryString, int timeRangeSeconds, int limit) => 
                        SearchGraylogLogsAsync(queryString, timeRangeSeconds, limit),
                    "search_graylog_logs",
                    "搜尋 Graylog 日誌系統。使用此函數來追蹤 RequestId 或 MsgId 的完整請求鏈路，或查詢特定錯誤代碼(Code)的所有記錄。參數: queryString=Elasticsearch查詢語法, timeRangeSeconds=時間範圍(秒,預設60), limit=回傳數量(預設10)");
                
                chatOptions.Tools = [searchTool];
                
                _logger.LogInformation("✅ AI 工具已註冊: {ToolName}, 工具數量: {Count}", 
                    searchTool.Name, chatOptions.Tools.Count);
            }
            else
            {
                _logger.LogWarning("⚠️ _graylogSearchService 為 null，AI 將無法使用工具");
            }

            // 建立回應文字內容（參考 ChatService.cs 的 AddUserMessageAsync）
            var responseText = new StringBuilder();
            var toolCallCount = 0;
            
            _logger.LogInformation("開始串流請求...");
            
            // 使用串流方式獲取 AI 回覆（與 ChatService.cs 完全相同的模式）
            await foreach (var update in _chatClient.GetStreamingResponseAsync(
                messages,
                chatOptions,
                cancellationToken))
            {
                // 添加非文字內容（如工具調用結果）到訊息列表
                // 這會自動執行工具並將結果加入對話
                // messages.AddMessages(update, filter: c => c is not TextContent);
                messages.AddMessages(update);
                
                // 累積文字內容到回覆
                responseText.Append(update.Text);
                
                // 記錄工具調用（用於診斷）
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case FunctionCallContent functionCall:
                            toolCallCount++;
                            _logger.LogInformation("🔧 AI 調用工具 #{Count}: {FunctionName}", 
                                toolCallCount, functionCall.Name);
                            break;
                        case FunctionResultContent functionResult:
                            _logger.LogInformation("📊 工具結果長度: {Length} 字元", 
                                functionResult.Result?.ToString()?.Length ?? 0);
                            break;
                    }
                }
            }

            var analysis = responseText.ToString();

            _logger.LogInformation("AI 分析完成 ({ToolCalls} 次工具調用)，結果長度: {Length}", 
                toolCallCount, analysis.Length);
            
            if (string.IsNullOrWhiteSpace(analysis))
            {
                _logger.LogWarning("⚠️ AI 沒有返回任何分析結果！");
                return "AI 分析未返回結果，請檢查配置。";
            }
            
            if (toolCallCount == 0 && _graylogSearchService != null)
            {
                _logger.LogWarning("⚠️ 警告：AI 沒有使用任何工具進行調查！");
            }

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI 分析過程中發生錯誤");
            return $"AI 分析失敗: {ex.Message}";
        }
    }

    // ========================================================================
    // AI 工具方法 - AI 可以調用的函數
    // ========================================================================

    /// <summary>
    /// 搜尋 Graylog 日誌（供 AI 使用的工具函數）。
    /// 此方法會被 AI 自動調用來查詢 Graylog 日誌系統。
    /// </summary>
    /// <param name="queryString">Elasticsearch 查詢字串，例如: RequestId:"xxx" 或 MsgId:"xxx" 或 Code:"904002"</param>
    /// <param name="timeRangeSeconds">時間範圍（秒），預設 60 秒，趨勢分析使用 900 秒（15分鐘）</param>
    /// <param name="limit">限制結果數量，預設 10 筆</param>
    /// <returns>格式化的日誌搜尋結果</returns>
    [Description("搜尋 Graylog 日誌系統。使用此函數來追蹤 RequestId 或 MsgId 的完整請求鏈路，或查詢特定錯誤代碼(Code)的所有記錄。")]
    private async Task<string> SearchGraylogLogsAsync(
        [Description("Elasticsearch 查詢語法，例如 RequestId:\"xxx\" 或 MsgId:\"xxx\" 或 Code:\"904002\"")] string queryString,
        [Description("時間範圍(秒)，預設60秒，趨勢分析用900秒(15分鐘)")] int timeRangeSeconds = 60,
        [Description("回傳記錄數量，預設10筆")] int limit = 10)
    {
        if (_graylogSearchService == null)
        {
            return "Graylog 搜尋服務未配置";
        }

        try
        {
            _logger.LogInformation("🔍 AI 呼叫 Graylog 搜尋: {Query}, 時間範圍: {TimeRange} 秒, 限制: {Limit} 筆", 
                queryString, timeRangeSeconds, limit);

            var results = await _graylogSearchService.SearchLogsAsync(
                queryString,
                timeRangeSeconds,
                limit);

            if (results.Count == 0)
            {
                _logger.LogInformation("📭 查詢 '{Query}' 沒有找到任何日誌記錄", queryString);
                return $"查詢 '{queryString}' 沒有找到任何日誌記錄";
            }

            _logger.LogInformation("📊 找到 {Count} 筆 Graylog 日誌", results.Count);

            // 格式化結果為易於 AI 理解的格式
            var formattedResults = new StringBuilder();
            formattedResults.AppendLine($"找到 {results.Count} 筆日誌記錄：");
            formattedResults.AppendLine();
            
            foreach (var (result, index) in results.Select((r, i) => (r, i + 1)))
            {
                formattedResults.AppendLine($"### 日誌 {index}");
                
                if (result.TryGetValue("timestamp", out var timestamp))
                    formattedResults.AppendLine($"- 時間: {timestamp}");
                
                if (result.TryGetValue("message", out var message))
                    formattedResults.AppendLine($"- 訊息: {message}");
                
                if (result.TryGetValue("Code", out var code))
                    formattedResults.AppendLine($"- 代碼: {code}");
                
                if (result.TryGetValue("Layer", out var layer))
                    formattedResults.AppendLine($"- 層級: {layer}");
                
                if (result.TryGetValue("Class", out var className))
                    formattedResults.AppendLine($"- 類別: {className}");
                
                if (result.TryGetValue("Method", out var method))
                    formattedResults.AppendLine($"- 方法: {method}");
                
                if (result.TryGetValue("Account", out var account))
                    formattedResults.AppendLine($"- 帳號: {account}");
                
                if (result.TryGetValue("Msg", out var msg))
                    formattedResults.AppendLine($"- 描述: {msg}");

                formattedResults.AppendLine();
            }

            return formattedResults.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜尋 Graylog 日誌時發生錯誤");
            return $"搜尋失敗: {ex.Message}";
        }
    }

    /// <summary>
    /// 構建 AI 分析提示詞
    /// </summary>
    private string BuildAnalysisPrompt(GraylogWebhookRequest request)
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("# 🚨 緊急調查任務");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("## 💡 範例：正確的工具使用方式");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("```");
        promptBuilder.AppendLine("✅ 正確做法：");
        promptBuilder.AppendLine("1. 看到 RequestId: \"0HNI1U3PLH2D9:00000004\"");
        promptBuilder.AppendLine("2. 立即執行: search_graylog_logs(\"RequestId:\\\"0HNI1U3PLH2D9:00000004\\\"\", 1, 20)");
        promptBuilder.AppendLine("3. 分析查詢結果");
        promptBuilder.AppendLine("4. 如發現 MsgId，再執行: search_graylog_logs(\"MsgId:\\\"xxx\\\"\", 1, 20)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("❌ 錯誤做法：");
        promptBuilder.AppendLine("1. 看到訊息");
        promptBuilder.AppendLine("2. 直接說「這是密碼錯誤的問題...」← 禁止！");
        promptBuilder.AppendLine("```");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("## ⚠️ 開始調查前必讀");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("**你現在必須扮演一個調查員，不是分析師！**");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("你的第一步不是分析，而是**立即使用 search_graylog_logs 工具**開始調查。");
        promptBuilder.AppendLine("以下提供的資訊只是線索，你必須用工具去 Graylog 查詢完整的證據。");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("---");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine($"## 📌 告警標題");
        promptBuilder.AppendLine($"**{request.EventDefinitionTitle}**");
        promptBuilder.AppendLine();

        if (!string.IsNullOrEmpty(request.EventDefinitionDescription))
        {
            promptBuilder.AppendLine($"## 📝 告警描述");
            promptBuilder.AppendLine($"{request.EventDefinitionDescription}");
            promptBuilder.AppendLine();
        }

        if (request.Event != null)
        {
            promptBuilder.AppendLine($"## 🔑 關鍵追蹤資訊（立即用這些去查詢！）");
            promptBuilder.AppendLine();

            if (request.Event.Fields != null && request.Event.Fields.Count > 0)
            {
                // 優先顯示關鍵的追蹤欄位
                var priorityFields = new[] { "RequestId", "MsgId", "Code", "Account", "Layer", "Class", "Method" };
                var foundAny = false;
                
                foreach (var fieldName in priorityFields)
                {
                    if (request.Event.Fields.TryGetValue(fieldName, out var value))
                    {
                        promptBuilder.AppendLine($"- **{fieldName}**: `{value}` ⬅️ 用這個查詢！");
                        foundAny = true;
                    }
                }

                if (!foundAny)
                {
                    // 如果沒有優先欄位，顯示所有欄位
                    foreach (var field in request.Event.Fields.Take(10))
                    {
                        promptBuilder.AppendLine($"- {field.Key}: `{field.Value}`");
                    }
                }

                promptBuilder.AppendLine();
            }
            
            promptBuilder.AppendLine($"## 📊 基本資訊");
            promptBuilder.AppendLine($"- 事件 ID: {request.Event.Id}");
            promptBuilder.AppendLine($"- 來源: {request.Event.Source}");
            promptBuilder.AppendLine($"- 優先等級: {request.Event.Priority}");
            promptBuilder.AppendLine($"- 時間: {request.Event.Timestamp:yyyy-MM-dd HH:mm:ss}");
            promptBuilder.AppendLine($"- 訊息片段: {request.Event.Message?.Substring(0, Math.Min(100, request.Event.Message?.Length ?? 0))}...");
            promptBuilder.AppendLine();
        }

        if (request.Backlog != null && request.Backlog.Count > 0)
        {
            promptBuilder.AppendLine($"## 📚 告警觸發的異常訊息（{request.Backlog.Count} 筆）");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("⚠️ 以下是觸發此告警的異常訊息，你必須對**每一筆**使用 search_graylog_logs 工具深入調查：");
            promptBuilder.AppendLine();

            // 顯示所有 backlog 記錄，提取關鍵資訊
            for (int i = 0; i < request.Backlog.Count; i++)
            {
                var logObj = request.Backlog[i];
                promptBuilder.AppendLine($"### 異常 #{i + 1}");
                
                // 嘗試將 object 轉換為 Dictionary
                Dictionary<string, object>? log = null;
                
                if (logObj is System.Text.Json.JsonElement jsonElement)
                {
                    // 如果是 JsonElement，反序列化為 Dictionary
                    var jsonString = jsonElement.GetRawText();
                    log = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                }
                else if (logObj is Dictionary<string, object> dict)
                {
                    log = dict;
                }
                
                if (log != null)
                {
                    // 提取關鍵欄位
                    if (log.TryGetValue("timestamp", out var timestamp))
                        promptBuilder.AppendLine($"- ⏰ 時間: `{timestamp}`");
                    
                    string? requestId = null;
                    string? msgId = null;
                    
                    if (log.TryGetValue("RequestId", out var requestIdObj))
                    {
                        requestId = requestIdObj?.ToString();
                        promptBuilder.AppendLine($"- 🔑 RequestId: `{requestId}` ⬅️ **用這個查詢完整鏈路！**");
                    }
                    
                    if (log.TryGetValue("MsgId", out var msgIdObj))
                    {
                        msgId = msgIdObj?.ToString();
                        promptBuilder.AppendLine($"- 🔑 MsgId: `{msgId}` ⬅️ **用這個追蹤前後端！**");
                    }
                    
                    if (log.TryGetValue("Code", out var code))
                        promptBuilder.AppendLine($"- ❌ Code: `{code}`");
                    
                    if (log.TryGetValue("Msg", out var msg))
                        promptBuilder.AppendLine($"- 💬 Msg: `{msg}`");
                    
                    if (log.TryGetValue("Layer", out var layer))
                        promptBuilder.AppendLine($"- 📍 Layer: `{layer}`");
                    
                    if (log.TryGetValue("Class", out var className))
                        promptBuilder.AppendLine($"- 📍 Class: `{className}`");
                    
                    if (log.TryGetValue("Method", out var method))
                        promptBuilder.AppendLine($"- 📍 Method: `{method}`");
                    
                    if (log.TryGetValue("Account", out var account))
                        promptBuilder.AppendLine($"- 👤 Account: `{account}`");
                    
                    if (log.TryGetValue("message", out var message))
                    {
                        var messageStr = message?.ToString() ?? "";
                        // 截取前 200 字元避免太長
                        var truncated = messageStr.Length > 200 
                            ? messageStr.Substring(0, 200) + "..." 
                            : messageStr;
                        promptBuilder.AppendLine($"- 📝 訊息: `{truncated}`");
                    }
                    
                    promptBuilder.AppendLine();
                    
                    // 提供調查指令
                    if (!string.IsNullOrEmpty(requestId) && !string.IsNullOrEmpty(msgId))
                    {
                        promptBuilder.AppendLine($"👉 **調查指令**: `search_graylog_logs(\"RequestId:\\\"{requestId}\\\"\", 1, 20)` 或 `search_graylog_logs(\"MsgId:\\\"{msgId}\\\"\", 1, 20)`");
                    }
                    else if (!string.IsNullOrEmpty(requestId))
                    {
                        promptBuilder.AppendLine($"👉 **調查指令**: `search_graylog_logs(\"RequestId:\\\"{requestId}\\\"\", 1, 20)`");
                    }
                    else if (!string.IsNullOrEmpty(msgId))
                    {
                        promptBuilder.AppendLine($"👉 **調查指令**: `search_graylog_logs(\"MsgId:\\\"{msgId}\\\"\", 1, 20)`");
                    }
                    else if (log.TryGetValue("Code", out var codeForQuery))
                    {
                        promptBuilder.AppendLine($"👉 **調查指令**: `search_graylog_logs(\"Code:\\\"{codeForQuery}\\\"\", 15, 50)`");
                    }
                }
                else
                {
                    // 如果無法轉換，直接顯示 JSON
                    var jsonStr = System.Text.Json.JsonSerializer.Serialize(logObj, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    var truncated = jsonStr.Length > 500 
                        ? jsonStr.Substring(0, 500) + "..." 
                        : jsonStr;
                    promptBuilder.AppendLine("```json");
                    promptBuilder.AppendLine(truncated);
                    promptBuilder.AppendLine("```");
                }
                
                promptBuilder.AppendLine();
            }
            
            promptBuilder.AppendLine("---");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("⚠️ **重要提醒**：");
            promptBuilder.AppendLine($"- 你必須對上述 **{request.Backlog.Count} 個異常** 逐一使用工具調查");
            promptBuilder.AppendLine("- 不要只看這些摘要就下結論，要查詢完整的上下文日誌");
            promptBuilder.AppendLine("- 使用 RequestId 或 MsgId 追蹤每個異常的完整請求鏈路");
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("---");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## 🎯 你的調查任務（按順序執行）");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### 步驟 1: 立即執行第一次查詢");
        promptBuilder.AppendLine("使用上面的 **RequestId** 或 **MsgId** 執行 search_graylog_logs");
        promptBuilder.AppendLine("範例: `search_graylog_logs(\"RequestId:\\\"xxx\\\"\", 1, 20)`");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### 步驟 2: 分析第一次查詢結果");
        promptBuilder.AppendLine("從查詢結果中找出異常的斷點位置");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### 步驟 3: 執行第二次查詢（驗證）");
        promptBuilder.AppendLine("使用 **MsgId** 或 **Code** 追蹤相關記錄");
        promptBuilder.AppendLine("範例: `search_graylog_logs(\"MsgId:\\\"xxx\\\"\", 1, 20)`");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### 步驟 4: 如果發現多個異常，對每個都重複步驟 1-3");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### 步驟 5: 提供調查報告");
        promptBuilder.AppendLine("使用要求的格式呈現你的調查結果");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("---");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("## ⏰ 現在開始調查！");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("不要回答「我理解了」或「好的」，直接開始使用 search_graylog_logs 工具！");

        return promptBuilder.ToString();
    }
}