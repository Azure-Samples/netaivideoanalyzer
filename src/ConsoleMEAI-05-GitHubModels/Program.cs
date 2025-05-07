using Microsoft.Extensions.Configuration;
using OpenCvSharp;
using Microsoft.Extensions.AI;
using Azure.AI.Inference;
using Azure;

// define video file and data folder
string videoFile = VideosHelper.GetVideoFilePathFireTruck();
string dataFolderPath = VideosHelper.CreateDataFolder();

//////////////////////////////////////////////////////
/// VIDEO ANALYSIS using OpenCV
//////////////////////////////////////////////////////

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

//////////////////////////////////////////////////////
/// Microsoft.Extensions.AI using GitHub Models
//////////////////////////////////////////////////////

// get the GitHub token from the environment variables or user secrets
// the GitHub token is used to authenticate the GitHub Models API 
// when the project is running in CodeSpaces
var gh_token = System.Environment.GetEnvironmentVariable("GITHUB_TOKEN");

if(string.IsNullOrEmpty(gh_token))
{
    var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
    gh_token = config["GH_TOKEN"];
}

IChatClient chatClient =
    new ChatCompletionsClient(
        endpoint: new Uri("https://models.inference.ai.azure.com"),
        new AzureKeyCredential(gh_token))
        .AsIChatClient("gpt-4o-mini");

List<ChatMessage> messages =
[
    new ChatMessage(Microsoft.Extensions.AI.ChatRole.System, PromptsHelper.SystemPrompt),
    new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, PromptsHelper.UserPromptDescribeVideo),
];

// create the OpenAI files that represent the video frames
int step = (int)Math.Ceiling((double)frames.Count / PromptsHelper.NumberOfFrames);

// show in the console the total number of frames and the step that neeeds to be taken to get the desired number of frames for the video analysis
Console.WriteLine($"Video total number of frames: {frames.Count}");
Console.WriteLine($"Get 1 frame every [{step}] to get the [{PromptsHelper.NumberOfFrames}] frames for analysis");

for (int i = 0; i < frames.Count; i += step)
{
    // save the frame to the "data/frames" folder
    string framePath = Path.Combine(dataFolderPath, "frames", $"{i}.jpg");
    Cv2.ImWrite(framePath, frames[i]);

    // read the image bytes, create a new image content part and add it to the messages
    AIContent aic = new DataContent(File.ReadAllBytes(framePath), "image/jpeg");
    var message = new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, [aic]);
    messages.Add(message);
}

// send the messages to the assistant
var response = await chatClient.GetResponseAsync(messages);
Console.WriteLine($"\n[GitHub Models response using Microsoft Extensions for AI]: ");
Console.WriteLine(response.Text);