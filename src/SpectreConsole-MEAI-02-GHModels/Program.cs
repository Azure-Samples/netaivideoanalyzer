using Microsoft.Extensions.Configuration;
using OpenCvSharp;
using Microsoft.Extensions.AI;
using Azure.AI.Inference;
using Azure;
using Spectre.Console;

SpectreConsoleOutput.DisplayTitle("MEAI - GH Models");

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
    // resize the frame to half of its size if the with is greater than 800
    if (frame.Width > 800)
    {
        Cv2.Resize(frame, frame, new OpenCvSharp.Size(frame.Width / 2, frame.Height / 2));
    }
    frames.Add(frame);
}
video.Release();
SpectreConsoleOutput.DisplaySubtitle("Total Frames", frames.Count.ToString());
SpectreConsoleOutput.DisplayTitleH3("Video Analysis using OpenCV done!");


//////////////////////////////////////////////////////
/// Microsoft.Extensions.AI using GitHub Models
//////////////////////////////////////////////////////

SpectreConsoleOutput.DisplayTitleH1("Video Analysis using Microsoft.Extensions.AI using GitHub Models");

// get the GitHub token from the environment variables or user secrets
// the GitHub token is used to authenticate the GitHub Models API 
// when the project is running in CodeSpaces
var github_token = System.Environment.GetEnvironmentVariable("GITHUB_TOKEN");

if(string.IsNullOrEmpty(github_token))
{
    var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
    github_token = config["GITHUB_TOKEN"];
}

IChatClient chatClient =
    new ChatCompletionsClient(
        endpoint: new Uri("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(github_token))
        .AsChatClient("gpt-4o-mini");

List<ChatMessage> messages =
[
    new ChatMessage(Microsoft.Extensions.AI.ChatRole.System, systemPrompt),
    new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, userPrompt),
];

// create the OpenAI files that represent the video frames
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
        tableImageAnalysis.AddColumn("Location");
        ctx.Refresh();

        for (int i = 0; i < frames.Count; i += step)
        {
            // save the frame to the "data/frames" folder
            string framePath = Path.Combine(dataFolderPath, "frames", $"{i}.jpg");
            Cv2.ImWrite(framePath, frames[i]);

            // read the image bytes, create a new image content part and add it to the messages
            AIContent aic = new DataContent(File.ReadAllBytes(framePath), "image/jpeg");
            var message = new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, [aic]);
            messages.Add(message);

            // add row
            tableImageAnalysis.AddRow(new Text(i.ToString()), new TextPath(framePath));
            ctx.Refresh();
        }
        ctx.Refresh();
    });

// display prompts
SpectreConsoleOutput.DisplayTablePrompts(systemPrompt, userPrompt);

SpectreConsoleOutput.DisplayTitleH1("Chat Client Response");

// send the messages to the chat client
var response = chatClient.GetStreamingResponseAsync(messages);

// display the response
SpectreConsoleOutput.DisplayTitleH3("GitHub Models response using Microsoft Extensions for AI");

await foreach (var update in response)
{
    AnsiConsole.Write(update.Text);
}

//Console.WriteLine($"\n[GitHub Models response using Microsoft Extensions for AI]: ");
//Console.WriteLine(response.Message);