using System.ClientModel;
using AI_X_GrayLog.Application.Interfaces;
using AI_X_GrayLog.Application.Services;
using AI_X_GrayLog.Domain.Interfaces;
using AI_X_GrayLog.Infrastructure.Repositories;
using Microsoft.Extensions.AI;
using OpenAI;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "AI X GrayLog API",
        Version = "v1",
        Description = "接收和處理 Graylog Webhook 告警的 API，支援 AI 分析"
    });

    // 讀取 XML 註解檔案以顯示 API 說明
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Register AI services (optional - only if API key is configured)
var azureOpenAiKey = builder.Configuration["OpenAI:ApiKey"];
var azureOpenAiEndpoint = builder.Configuration["OpenAI:Endpoint"];

if (!string.IsNullOrEmpty(azureOpenAiKey) && !string.IsNullOrEmpty(azureOpenAiEndpoint))
{
    var openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";
    
    builder.Services.AddSingleton<IChatClient>(sp =>
    {
        // 設置 Azure OpenAI 端點 URI
        var azureOpenAIEndpoint = new Uri(
            new Uri(builder.Configuration["OpenAI:Endpoint"] 
                    ?? throw new InvalidOperationException("請設置環境變量：OpenAI:Endpoint。請查看 README 的設置說明。")), 
            "/openai/v1");

        // 設置 OpenAI 客戶端選項，指定服務端點
        var openAIOptions = new OpenAIClientOptions { Endpoint = azureOpenAIEndpoint };

        // 建立 Azure OpenAI 客戶端，用於獲取聊天服務
        var azureOpenAiClient = new OpenAIClient(
            new ApiKeyCredential(builder.Configuration["OpenAI:ApiKey"] ?? 
                                 throw new InvalidOperationException("請設置環境變數：OpenAI:ApiKey。請查看 README 的設置說明。")), 
            openAIOptions);
        
        // 獲取聊天客戶端並轉換為 IChatClient
        // 使用 UseFunctionInvocation 確保工具函數被正確執行
        return azureOpenAiClient
            .GetOpenAIResponseClient(openAiModel)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    });
    
    builder.Services.AddScoped<IAiAlertAnalysisService, AiAlertAnalysisService>();
    
    Console.WriteLine($"✅ AI 服務已啟用，使用模型: {openAiModel}");
    Console.WriteLine($"✅ 使用端點: {azureOpenAiEndpoint}");
    Console.WriteLine("✅ 已啟用 FunctionInvocation 中間件");
}
else
{
    Console.WriteLine("未配置 OpenAI API Key 或 Endpoint，AI 分析功能將不可用");
}

// Register Teams notification service (optional - only if webhook URL is configured)
var teamsWebhookUrl = builder.Configuration["Teams:WebhookUrl"];
if (!string.IsNullOrEmpty(teamsWebhookUrl))
{
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<ITeamsNotificationService, TeamsNotificationService>();
    
    Console.WriteLine("Teams 通知服務已啟用");
}
else
{
    Console.WriteLine("未配置 Teams Webhook URL，Teams 通知功能將不可用");
}

// Register Graylog search service (optional - only if Graylog is configured)
// 注意：必須在 AiAlertAnalysisService 之前註冊
var graylogUrl = builder.Configuration["Graylog:Url"];
var graylogUsername = builder.Configuration["Graylog:Username"];
if (!string.IsNullOrEmpty(graylogUrl) && !string.IsNullOrEmpty(graylogUsername))
{
    builder.Services.AddScoped<IGraylogSearchService, GraylogSearchService>();
    
    Console.WriteLine($"✅ Graylog 搜尋服務已啟用: {graylogUrl}");
}
else
{
    Console.WriteLine("⚠️ 未配置 Graylog，AI 工具搜尋功能將不可用");
}

// Register application services
builder.Services.AddScoped<IGraylogAlertApplicationService, GraylogAlertApplicationService>();

// Register repository
builder.Services.AddSingleton<IGraylogAlertRepository, InMemoryGraylogAlertRepository>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AI X GrayLog API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "AI X GrayLog API 文件";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

