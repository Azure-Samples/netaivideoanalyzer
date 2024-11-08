using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using OpenAI;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
builder.Logging.AddConsole();

builder.AddAzureOpenAIClient("openai");

// get chat client from aspire hosting configuration
builder.Services.AddSingleton(serviceProvider =>
{
    var config = serviceProvider.GetService<IConfiguration>()!;
    OpenAIClient client = serviceProvider.GetRequiredService<OpenAIClient>();
    var chatClient = client.GetChatClient(config["AI_ChatDeploymentName"]);
    return chatClient;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// create a new endpoint that receives a VideoRequest and returns a VideoResponse
app.MapPost("/AnalyzeVideo", async (HttpContext httpContext, VideoRequest request, ILogger<Program> logger, ChatClient client) =>
{
    if (request.NumberOfFramesToBeProcessed <= 1)
        request.NumberOfFramesToBeProcessed = 10;

    VideoProcessor videoProcessor = new VideoProcessor(app.Configuration, logger, client);

    List<ChatMessage> messages = videoProcessor.CreateMessages(request.SystemPrompt, request.UserPrompt);

    // extract the frames from the video
    var frames = videoProcessor.ExtractVideoFrames(request.VideoBytes);

    // process the frames
    var videoDescription = videoProcessor.AnalyzeVideoAsync(frames, request.NumberOfFramesToBeProcessed, messages, client);

    // create a response
    var response = new VideoResponse
    {
        ProcessedFrames = request.NumberOfFramesToBeProcessed,
        TotalFrames = frames.Count,
        VideoDescription = videoDescription,
        VideoFrame = "/images/frame.jpg"
    };

    // get the request URL
    var requestUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}";
    logger.LogInformation($"Request URL: {requestUrl}");

    // define the complete url for the video frame using the current app running url
    response.VideoFrame = $"{requestUrl}/images/frame.jpg";

    logger.LogInformation($"Video Response: {response}");

    return response;
});

app.MapGet("/SystemInfo", async (ILogger<Program> logger) =>
{
    return Results.Json(await GetSystemInfo(logger));
});

app.MapGet("/", async (ILogger<Program> logger) =>
{
    logger.LogInformation("API Service is running");
    return Results.Json($".NET Video Analyzer API Service - {DateTime.Now}");
});

app.MapGet("/AICheck", (ILogger<Program> logger, ChatClient client) =>
{
    logger.LogInformation("Checking AI service");

    ChatCompletion completion = client.CompleteChat(new UserChatMessage("Please solve 2 + 2."));

    logger.LogInformation($"Response Finish Reason: {completion.FinishReason}");
    logger.LogInformation($"completion.Content[0].Text: {completion.Content[0].Text}");

    var i = 0;
    foreach (var message in completion.Content)
    {
        logger.LogInformation($"Message {i}: {message.Text}");
        i++;
    }
    return Results.Json(completion!);
});

try
{
    // publish the content of the folder "images", as images
    if (!Directory.Exists("images"))
    {
        Directory.CreateDirectory("images");
    }
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "images")),
        RequestPath = "/images"
    });
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Error publishing images folder");
}

await GetSystemInfo(app.Services.GetRequiredService<ILogger<Program>>());

app.MapDefaultEndpoints();

app.Run();

async Task<SystemInformation> GetSystemInfo(ILogger<Program> logger)
{
    SystemInformation systemInfo = new();
    try
    {
        logger.LogInformation("Get System information");
        systemInfo = await SystemInformation.GetSystemInfoAsync();
        // show the system information in the log
        logger.LogInformation($"System information: {systemInfo}");
    }
    catch (Exception exc)
    {
        logger.LogError(exc, "Error retrieving system information");
    }
    return systemInfo;
}