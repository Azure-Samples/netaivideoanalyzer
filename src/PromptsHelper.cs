/// <summary>
/// Helper class for prompts related to video analysis.
/// </summary>
public static class PromptsHelper
{
    /// <summary>
    /// Defines the number of frames that the model will analyze.
    /// </summary>
    public static int NumberOfFrames = 10;

    /// <summary>
    /// The system prompt for the assistant.
    /// </summary>
    public static string SystemPrompt = @"You are a useful assistant. When you receive a group of images, they are frames of a unique video.";

    /// <summary>
    /// The user prompt to describe the whole video story.
    /// </summary>
    public static string UserPromptDescribeVideo = @"The attached frames represent a video. Describe the whole video story, do not describe frame by frame.";

    /// <summary>
    /// The user prompt to create an incident report for car damage analysis.
    /// </summary>
    public static string UserPromptInsuranceCarAnalysis = @"You are an expert in evaluating car damage from car accidents for auto insurance reporting. 
The attached images represents frames from a video.
Create an incident report for the accident shown in the video with 3 sections. 
- Section 1 will include the car details (license plate, car make, car model, approximate model year, color, mileage).
- Section 2 list the car damage, per damage in a list.
- Section 3 will only include exactly 6 sentence description of the car damage.";

    public static string UserPromptInsuranceCarAnalysisOllama = @"You are an expert in evaluating car damage from car accidents for auto insurance reporting. 
Using the information below, create an incident report for the accident with 3 sections. 
- Section 1 will include the car details (license plate, car make, car model, approximate model year, color, mileage).
- Section 2 list the car damage, per damage in a list.
- Section 3 will only include exactly 6 sentence description of the car damage.
====================================
The texts below represets a the video analysis from different frames from the video with the car information.";

    public static string UserPromptInsuranceCarAnalysisOllamaJSON = @"You are an expert in evaluating car damage from car accidents for auto insurance reporting. 
Using the information below, create an incident report for the accident with 3 sections. 
- Section 1 will include the car details (license plate, car make, car model, approximate model year, color, mileage).
- Section 2 list the car damage, per damage in a list.
- Section 3 will only include exactly 6 sentence description of the car damage.

Generate the Output content in JSON format
Generate the content of the sections in English and Spanish.
Do not return markdown or html content.
Only return a JSON object with a root element named 'incident_report'.
====================================
The texts below represets a the video analysis from different frames from the video with the car information.";

    /// <summary>
    /// The user prompt to create an incident report for car damage analysis and return a JSON object.
    /// </summary>
    public static string UserPromptInsuranceCarAnalysisJson = @"You are an expert in evaluating car damage from car accidents for auto insurance reporting. 
Create an incident report for the accident shown in the video with 3 sections in JSON format. 
- Section 1 will include the car details (license plate, car make, car model, approximate model year, color, mileage).
- Section 2 list the car damage, per damage in a list.
- Section 3 will only include exactly 6 sentence description of the car damage.

The output should be a JSON object, and it also should include the information in English, and Spanish.";
}
