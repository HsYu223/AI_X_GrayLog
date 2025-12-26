# AI X GrayLog - 智能告警分析系統

此專案是一個 .NET 10 Web API，用於接收和處理來自 Graylog 的 Webhook 告警通知，並使用 AI 自動分析問題原因，最後將結果發送到 Microsoft Teams。

## 🌟 主要功能

### ✅ 已實作功能

1. **Graylog Webhook 接收**
   - 接收來自 Graylog 的告警通知
   - 支援完整的 Webhook payload 解析
   - 優先等級處理和分類

2. **AI 智能分析**
   - 使用 Azure OpenAI 自動分析告警
   - 識別問題類型和嚴重程度
   - 推測根本原因
   - 提供解決方案建議
   - 建議預防措施

3. **Microsoft Teams 通知**
   - 自動發送 AI 分析結果到 Teams 頻道
   - 透過 Power Automate webhook 整合
   - 支援格式化訊息顯示

## 🚀 快速開始

### 1. 配置設定

編輯 `appsettings.json`：

```json
{
  "OpenAI": {
    "ApiKey": "your-azure-openai-key",
    "Model": "gpt-4o-mini",
    "Endpoint": "https://your-resource.cognitiveservices.azure.com/"
  },
  "Teams": {
    "WebhookUrl": "your-power-automate-webhook-url"
  }
}
```

### 2. 啟動應用程式

```bash
cd "AI X GrayLog"
dotnet run
```

## 📋 API 端點

### Graylog Webhook

| 方法 | 端點 | 說明 |
|------|------|------|
| POST | `/api/graylog/webhook` | 接收 Graylog Webhook 告警（自動觸發 AI 分析） |
| POST | `/api/graylog/webhook/analyze` | 手動請求 AI 分析 |
| GET | `/api/graylog/webhook/info` | 取得 Webhook 端點資訊 |

### 文件

| 端點 | 說明 |
|------|------|
| `/swagger` | Swagger UI 介面 |
| `/swagger/v1/swagger.json` | OpenAPI JSON 規格 |

## 🔧 工作流程

```
┌─────────────┐
│  Graylog    │
│   告警      │
└──────┬──────┘
       │ Webhook
       ▼
┌─────────────────────┐
│ GraylogController   │
│ 接收 Webhook        │
└──────┬──────────────┘
       │
       ▼
┌────────────────────────────┐
│ GraylogAlertApplication    │
│ Service                     │
│ - 儲存告警                  │
│ - 判斷優先等級              │
└──────┬─────────────────────┘
       │ Priority >= 2
       ▼
┌────────────────────────────┐
│ AiAlertAnalysisService     │
│ - 分析告警內容              │
│ - 找出根本原因              │
│ - 提供解決建議              │
└──────┬─────────────────────┘
       │
       ▼
┌────────────────────────────┐
│ TeamsNotificationService   │
│ - 格式化訊息                │
│ - 發送到 Teams              │
└──────┬─────────────────────┘
       │ Power Automate
       ▼
┌────────────────────────────┐
│  Microsoft Teams           │
│  團隊即時收到分析結果       │
└────────────────────────────┘
```

## 📊 專案架構

```
AI X GrayLog/
├── Application/              # 應用層
│   ├── DTOs/                # 資料傳輸物件
│   │   ├── Requests/        # 請求 DTO
│   │   └── Responses/       # 回應 DTO
│   ├── Interfaces/          # 服務介面
│   └── Services/            # 應用服務實作
│       ├── GraylogAlertApplicationService.cs
│       ├── AiAlertAnalysisService.cs
│       └── TeamsNotificationService.cs
├── Domain/                   # 領域層
│   ├── Entities/            # 實體
│   └── Interfaces/          # 儲存庫介面
├── Infrastructure/           # 基礎設施層
│   └── Repositories/        # 儲存庫實作
├── Presentation/             # 展示層
│   └── Controllers/         # API 控制器
└── Program.cs               # 主程式進入點
```

### 預期結果

1. **Teams 訊息**：
   - 收到格式化的告警通知
   - 包含 AI 詳細分析
   - 問題原因和解決建議

## ⚙️ 配置說明

### OpenAI 設定

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key",      // Azure OpenAI API 金鑰
    "Model": "gpt-4o-mini",         // 使用的模型
    "Endpoint": "https://..."       // Azure OpenAI 端點
  }
}
```

### Teams 設定

```json
{
  "Teams": {
    "WebhookUrl": "https://..."     // Power Automate Webhook URL
  }
}
```

### 觸發條件

- 告警優先等級 >= 2 時自動觸發 AI 分析和 Teams 通知
- 可在 `GraylogAlertApplicationService.cs` 中修改觸發條件

## 🛠️ 技術堆疊

- **.NET**: 10.0
- **ASP.NET Core**: Web API
- **Microsoft.Extensions.AI**: 9.1.0
- **OpenAI**: 2.8.0
- **Swashbuckle.AspNetCore**: 10.1.0

## 🤝 貢獻

歡迎提交 Issue 和 Pull Request！

## 📄 授權

請依照您的專案需求加入適當的授權資訊。

