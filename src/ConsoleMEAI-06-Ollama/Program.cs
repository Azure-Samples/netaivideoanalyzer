using OpenCvSharp;
using Microsoft.Extensions.AI;

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
    // resize the frame to half of its size
    Cv2.Resize(frame, frame, new OpenCvSharp.Size(frame.Width / 2, frame.Height / 2));
    frames.Add(frame);
}
video.Release();

//////////////////////////////////////////////////////
/// Microsoft.Extensions.AI using Ollama
//////////////////////////////////////////////////////


IChatClient chatClientImageAnalyzer =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "llava:7b");
IChatClient chatClient =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.2");

Console.WriteLine("====================================================");
Console.WriteLine("Start frame by frame analysis");
Console.WriteLine("====================================================");
Console.WriteLine("");

List<string> imageAnalysisResponses = new();
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
    List<ChatMessage> messages =
    [
        new ChatMessage(ChatRole.User, @$"The image represents a frame of a video. Describe the image in a single sentence. Frame Number: [{i}]

In example:
Frame Number: 0
A view of a fire station with red garage doors. The sidewalk is empty, and there are yellow posts in front of the garage doors. Surrounding buildings are visible in the background, along with a clear blue sky.

Frame Number: 1
The same fire station is shown, but now a fire truck is partially visible, parked in front of the garage doors. The scene retains the same urban backdrop, with nearby buildings and trees.

Frame Number: 2
The fire truck is now seen moving out of the station and onto the street. The background features a tall black building and additional urban elements, including traffic signs and trees."),
        new ChatMessage(ChatRole.User, [aic])
     ];
    // send the messages to the assistant
    var imageAnalysis = await chatClientImageAnalyzer.GetResponseAsync(messages);
    var imageAnalysisResponse = $"{imageAnalysis.Text}\n";
    imageAnalysisResponses.Add(imageAnalysisResponse);

    Console.WriteLine($"Frame: {i}\n{imageAnalysisResponse}");
}

Console.WriteLine("====================================================");
Console.WriteLine("Start build prompt");
Console.WriteLine("====================================================");
Console.WriteLine("");
var imageAnalysisResponseCollection = string.Empty;

foreach (var desc in imageAnalysisResponses)
{
    imageAnalysisResponseCollection += $"\n[FRAME ANALYSIS START]{desc}[FRAME ANALYSIS END]";
}

var userPrompt = $"The texts below represets a video analysis from different frames from the video. Using that frames description, describe the video. Do not describe individual frames. Do not mention the frame number of frame description. Using the frames information infer the content of the video.\n{imageAnalysisResponseCollection}";

// display the full user prompt 
Console.WriteLine(userPrompt);
Console.WriteLine("");

Console.WriteLine("====================================================");
Console.WriteLine("Start video analysis");
Console.WriteLine("====================================================");
Console.WriteLine("");

// send the messages to the assistant
var response = await chatClient.GetResponseAsync(userPrompt);
Console.WriteLine("MEAI Chat Client using Ollama Response: ");
Console.WriteLine(response.Text);

