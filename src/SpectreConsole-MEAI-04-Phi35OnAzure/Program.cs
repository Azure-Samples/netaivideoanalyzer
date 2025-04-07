using System.Diagnostics;
using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenCvSharp;
using Spectre.Console;

SpectreConsoleOutput.DisplayTitle("OLLAMA & Phi3.5V");

// define video file and data folder
SpectreConsoleOutput.DisplayTitleH1("Video file and data folder");
string videoFile = VideosHelper.GetVideoFilePathCar();
string dataFolderPath = VideosHelper.CreateDataFolder();
Console.WriteLine();

var systemPrompt = PromptsHelper.SystemPrompt;
var userPrompt = PromptsHelper.UserPromptInsuranceCarAnalysis;

//////////////////////////////////////////////////////
/// VIDEO ANALYSIS using OpenCV
//////////////////////////////////////////////////////

SpectreConsoleOutput.DisplayTitleH1("Video Analysis using OpenCV");

// Extract the frames from the video
var video = new VideoCapture(videoFile);
var frames = new List<Mat>();
while (video.IsOpened())
{
    var frame = new Mat();
    if (!video.Read(frame) || frame.Empty())
        break;
    // resize the frame to half of its size
    Cv2.Resize(frame, frame, new OpenCvSharp.Size(frame.Width / 2, frame.Height / 2));
    frames.Add(frame);
}
video.Release();
SpectreConsoleOutput.DisplaySubtitle("Total Frames", frames.Count.ToString());
SpectreConsoleOutput.DisplayTitleH3("Video Analysis using OpenCV done!");

//////////////////////////////////////////////////////
/// Microsoft.Extensions.AI using Ollama
//////////////////////////////////////////////////////
SpectreConsoleOutput.DisplayTitleH1("Video Analysis using Phi-3.5 in Azure");

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var endpointVision = config["AZURE_PHI35V_URI"];
var modelIdVision = config["AZURE_PHI35V_MODEL"];
var apiKeyVision = config["AZURE_PHI35V_KEY"];
var endpointChat = config["AZURE_PHI35_URI"];
var modelIdChat = config["AZURE_PHI35_MODEL"];
var apiKeyChat = config["AZURE_PHI35_KEY"];


var credentialVision = new AzureKeyCredential(apiKeyVision);
IChatClient chatClientImageAnalyzer = new ChatCompletionsClient(
        endpoint: new Uri(endpointVision), 
        credential: credentialVision).AsIChatClient(modelIdVision);

var credentialChat = new AzureKeyCredential(apiKeyChat);
IChatClient chatClient = new ChatCompletionsClient(
        endpoint: new Uri(endpointChat),
        credential: credentialChat).AsIChatClient(modelIdChat);



List<string> imageAnalysisResponses = new();
int step = (int)Math.Ceiling((double)frames.Count / PromptsHelper.NumberOfFrames);

// show the total number of frames and the step to get the desired number of frames using spectre console
SpectreConsoleOutput.DisplaySubtitle("Process", $"Get 1 frame every [{step}] to get the [{PromptsHelper.NumberOfFrames}] frames for analysis");

var tableImageAnalysis = new Table();
await AnsiConsole.Live(tableImageAnalysis)
    .AutoClear(false)   // Do not remove when done
    .Overflow(VerticalOverflow.Ellipsis) // Show ellipsis when overflowing
    .StartAsync(async ctx =>
    {
        tableImageAnalysis.AddColumn("N#");
        tableImageAnalysis.AddColumn("Elapsed");
        tableImageAnalysis.AddColumn("Description");
        ctx.Refresh();

        var stopwatch = new Stopwatch();

        for (int i = 0; i < frames.Count; i += step)
        {
            // save the frame to the "data/frames" folder
            string framePath = Path.Combine(dataFolderPath, "frames", $"{i}.jpg");
            Cv2.ImWrite(framePath, frames[i]);

            // read the image bytes, create a new image content part and add it to the messages
            AIContent aic = new DataContent(File.ReadAllBytes(framePath), "image/jpeg");
            List<ChatMessage> messages =
            [
                new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, @$"The image represents a frame of a video. Describe the image in a single paragraph, including all the car reference details, i for the frame Number: [{i}]. If there is car information like license number, milleage, color, make, model, year, etc., include it in the paragraph. Also include and details car damage if present in the analyzed image."),
        new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, [aic])
             ];
            // send the messages to the assistant            
            stopwatch.Restart();
            var imageAnalysis = await chatClientImageAnalyzer.GetResponseAsync(messages);
            var imageAnalysisResponse = $"{imageAnalysis.Text}\n";
            imageAnalysisResponses.Add(imageAnalysisResponse);
            stopwatch.Stop();
            var elapsedTime = stopwatch.Elapsed;

            // add row
            var shortResponse = imageAnalysisResponse.Length > 100 ? imageAnalysisResponse.Substring(0, 100) + "..." : imageAnalysisResponse;
            tableImageAnalysis.AddRow(new Text(i.ToString()), new Text(elapsedTime.ToString("ss\\.fff")), new Text(shortResponse));
            ctx.Refresh();
        }
        ctx.Refresh();
    });

SpectreConsoleOutput.DisplayTitleH3("Frame by frame Analysis using Phi-3.5 Vision model done!");

SpectreConsoleOutput.DisplayTitleH2("Start build prompt");
var imageAnalysisResponseCollection = string.Empty;

foreach (var desc in imageAnalysisResponses)
{
    imageAnalysisResponseCollection += $"\n[FRAME ANALYSIS START]{desc}[FRAME ANALYSIS END]";
}

SpectreConsoleOutput.DisplayTitleH3("Start build prompt done!");

SpectreConsoleOutput.DisplayTitleH2("Start video analysis using LLM");

// send the messages to the assistant
var response = await chatClient.GetResponseAsync($"{userPrompt}\n{imageAnalysisResponseCollection}");

var panelResponse = new Panel(response.Text);
panelResponse.Header = new PanelHeader("MEAI Chat Client using Phi-3.5 in Azure Response");
AnsiConsole.Write(panelResponse);