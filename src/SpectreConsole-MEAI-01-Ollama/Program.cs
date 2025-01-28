using OpenCvSharp;
using Microsoft.Extensions.AI;
using Spectre.Console;
using System.Diagnostics;

SpectreConsoleOutput.DisplayTitle("MEAI - OLLAMA");

// define video file and data folder
SpectreConsoleOutput.DisplayTitleH1("Video file and data folder");
string videoFile = VideosHelper.GetVideoFilePathCar();
string dataFolderPath = VideosHelper.CreateDataFolder();
Console.WriteLine();

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

    // if the local model requires too much memory,
    // you can resize the frame to half of its size
    // smaller images are faster to process
    //Cv2.Resize(frame, frame, new OpenCvSharp.Size(frame.Width / 3, frame.Height / 3));

    frames.Add(frame);
}
video.Release();
SpectreConsoleOutput.DisplaySubtitle("Total Frames", frames.Count.ToString());
SpectreConsoleOutput.DisplayTitleH3("Video Analysis using OpenCV done!");

//////////////////////////////////////////////////////
/// Microsoft.Extensions.AI using Ollama
//////////////////////////////////////////////////////
SpectreConsoleOutput.DisplayTitleH1("Video Analysis using Microsoft.Extensions.AI using Ollama");

IChatClient chatClientImages =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "llama3.2-vision");
IChatClient chatClient =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "phi4");

// for the ollama process we use only 5 frames
// change this value to get more frames for a more detailed analysis
var numberOfFrames = 20; 

List<string> imageAnalysisResponses = new();
int step = (int)Math.Ceiling((double)frames.Count / numberOfFrames);

// show the total number of frames and the step to get the desired number of frames using spectre console
SpectreConsoleOutput.DisplaySubtitle("Process", $"Get 1 frame every [{step}] to get the [{numberOfFrames}] frames for analysis");

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

        int frameNumber = 0;

        for (int i = 0; i < frames.Count; i += step)
        {
            frameNumber++;
            // save the frame to the "data/frames" folder
            string framePath = Path.Combine(dataFolderPath, "frames", $"{i.ToString("D3")}.jpg");
            Cv2.ImWrite(framePath, frames[i]);

            // read the image bytes, create a new image content part and add it to the messages
            AIContent aic = new ImageContent(File.ReadAllBytes(framePath), "image/jpeg");
            List<ChatMessage> messages =
            [
                new ChatMessage(ChatRole.User, @$"The image represents a frame of a video. Describe the image in a single sentence for the frame Number: [{i}]
Include detailed information for an insurance agent, like car model, mileage, color, and any visible damage. 
Include all the text that you can extract from the image, and also what is the text representing.
Do not include [IMAGE DESCRIPTION START] and [IMAGE DESCRIPTION END].
Generate just text, do not generate markdown or html.
In example:
[IMAGE DESCRIPTION START]
Frame 1: The rear side of a blue Toyota Camry is shown with visible scratches and scuffs on the bumper and body panel..
[IMAGE DESCRIPTION END]
[IMAGE DESCRIPTION START]
Frame 2: The image shows the rear of a blue Toyota car, featuring a dented trunk lid and a Minnesota license plate with the number 'MAT 251'.
[IMAGE DESCRIPTION END]
[IMAGE DESCRIPTION START]
Frame 3: The image shows a car's speedometer and odometer, indicating a speed of 0 MPH, an odometer reading of 151,856 miles, and an outside temperature of 55.4°F.
[IMAGE DESCRIPTION END]"),
        new ChatMessage(ChatRole.User, [aic])
             ];
            // send the messages to the assistant            
            stopwatch.Restart();
            var imageAnalysis = await chatClientImages.CompleteAsync(messages);
            var imageAnalysisResponse = $"{imageAnalysis.Message.Text}\n";
            imageAnalysisResponses.Add(imageAnalysisResponse);
            stopwatch.Stop();
            var elapsedTime = stopwatch.Elapsed;
            
            // add row
            var shortResponse = imageAnalysisResponse.Length > 150 ? imageAnalysisResponse.Substring(0, 150) + "..." : imageAnalysisResponse;
            tableImageAnalysis.AddRow(new Text($"{frameNumber.ToString("D2")} / {i.ToString("D3")}"), new Text(elapsedTime.ToString("ss\\.fff")), new Text(shortResponse));
            ctx.Refresh();
        }
        ctx.Refresh();
    });

SpectreConsoleOutput.DisplayTitleH3("Frame by frame Analysis using vision models done!");

SpectreConsoleOutput.DisplayTitleH2("Start build prompt");
var imageAnalysisResponseCollection = string.Empty;

foreach (var desc in imageAnalysisResponses)
{
    imageAnalysisResponseCollection += $"\n[FRAME ANALYSIS START]{desc}[FRAME ANALYSIS END]";
}

var userPrompt = $"{PromptsHelper.UserPromptInsuranceCarAnalysisOllama}\n{imageAnalysisResponseCollection}";

SpectreConsoleOutput.DisplayTitleH3("Start build prompt done!");


SpectreConsoleOutput.DisplayTitleH2("Start video analysis using LLM");

// send the messages to the assistant
var response = await chatClient.CompleteAsync(userPrompt);

var panelResponse = new Panel(response.Message.ToString())
{
    Header = new PanelHeader("MEAI Chat Client using Ollama Response")
};
AnsiConsole.Write(panelResponse);

var userPromptJson = $"{PromptsHelper.UserPromptInsuranceCarAnalysisOllamaJSON}\n{imageAnalysisResponseCollection}";

SpectreConsoleOutput.DisplayTitleH2("Start video analysis using LLM");

// send the messages to the assistant
var responseJson = await chatClient.CompleteAsync(userPromptJson);
Console.Write(responseJson.Message.ToString());