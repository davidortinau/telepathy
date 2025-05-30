using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Telepathic.Models;
using Telepathic.Services;

namespace Telepathic.PageModels;

/// <summary>
/// Service for analyzing task text to detect potential assist opportunities
/// </summary>
public class TaskAssistAnalyzer
{
    private readonly IChatClientService _chatClientService;
    private readonly ILogger<TaskAssistAnalyzer> _logger;

    public TaskAssistAnalyzer(IChatClientService chatClientService, ILogger<TaskAssistAnalyzer> logger)
    {
        _chatClientService = chatClientService;
        _logger = logger;
    }

    /// <summary>
    /// Analyze a task's text to detect assist opportunities
    /// </summary>
    /// <param name="task">The task to analyze</param>
    /// <returns>True if assist opportunities were found, false otherwise</returns>
    public async Task<bool> AnalyzeTaskAsync(ProjectTask task)
    {
        if (task == null || string.IsNullOrWhiteSpace(task.Title))
            return false;

        if (!_chatClientService.IsInitialized)
        {
            _logger.LogWarning("Cannot analyze task: chat client is not initialized");
            return false;
        }

        try
        {
            var prompt = $@"
Analyze the following task text to identify if it's related to one of these categories:
1. Calendar/Event - Contains a date, time, or event that could be added to a calendar
2. Location/Address - Contains a location, address, or place that could be opened in maps
3. Phone - Contains a phone number or mention of calling someone
4. Email - Contains an email address or mention of emailing someone
5. Browser - Contains a reference to a website, web search, or online information that should be opened in a browser

Task text: ""{task.Title}""

Return a JSON object with the following structure:
{{
    ""assistType"": ""None|Calendar|Maps|Phone|Email|Browser"",
    ""assistData"": ""The extracted data that would be needed (phone number, email, address, search query, etc.)""
}}

For Browser type tasks, the assistData should be what the user is trying to find or access online, not a full URL.

Only specify assistType different from None if you are confident about the detection.
";

            var chatClient = _chatClientService.GetClient();
            var response = await chatClient.GetResponseAsync<AssistAnalysisResult>(prompt).ConfigureAwait(false);

            if (response?.Result != null)
            {
                // Update the task with assist info
                task.AssistType = response.Result.AssistType;
                task.AssistData = response.Result.AssistData ?? string.Empty;
                
                _logger.LogInformation("Task analysis complete. AssistType: {AssistType}, AssistData: {AssistData}", 
                    task.AssistType, task.AssistData);
                
                return task.AssistType != AssistType.None;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing task for assist opportunities");
        }

        return false;
    }

    private class AssistAnalysisResult
    {
        public AssistType AssistType { get; set; } = AssistType.None;
        public string? AssistData { get; set; }
    }
}
