using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Telepathic.Models;
using Telepathic.Services;

namespace Telepathic.PageModels;

public enum PhotoPhase { Analyzing, Reviewing }

public partial class PhotoPageModel : ObservableObject, IProjectTaskPageModel, IQueryAttributable
{
    private readonly ProjectRepository _projectRepository;
    private readonly TaskRepository _taskRepository;
    private readonly IChatClientService _chatClientService;
    private readonly ModalErrorHandler _errorHandler;
    private readonly ILogger<PhotoPageModel> _logger;
    private readonly TaskAssistHandler _taskAssistHandler;
    private readonly Stopwatch _stopwatch = new();

    [ObservableProperty] private string _imagePath = string.Empty;
    [ObservableProperty] private ImageSource? _imageSource;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private PhotoPhase _phase = PhotoPhase.Analyzing;
    
    // Status indicator properties
    [ObservableProperty] private bool _isAnalyzingContext = true;
    [ObservableProperty] private string _analysisStatusTitle = "Processing Photo";
    [ObservableProperty] private string _analysisStatusDetail = "Preparing to analyze your image...";
    [ObservableProperty] private string _analysisInstructions = "";
    
    // Extracted projects and tasks
    [ObservableProperty] private List<Project> _projects = new();

    IAsyncRelayCommand<ProjectTask> IProjectTaskPageModel.NavigateToTaskCommand => NavigateToTaskCommand;
    IAsyncRelayCommand<ProjectTask> IProjectTaskPageModel.AcceptRecommendationCommand => AcceptRecommendationCommand;
    IAsyncRelayCommand<ProjectTask> IProjectTaskPageModel.RejectRecommendationCommand => RejectRecommendationCommand;
    IAsyncRelayCommand<ProjectTask> IProjectTaskPageModel.AssistCommand => AssistCommand;

    public PhotoPageModel(
        ProjectRepository projectRepository,
        TaskRepository taskRepository,
        IChatClientService chatClientService,
        ModalErrorHandler errorHandler,
        TaskAssistHandler taskAssistHandler,
        ILogger<PhotoPageModel> logger)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _chatClientService = chatClientService;
        _errorHandler = errorHandler;
        _taskAssistHandler = taskAssistHandler;
        _logger = logger;
    }
    
    [RelayCommand]
    private async Task PageAppearing()
    {
        // If we already have an image, analyze it
        if (!string.IsNullOrEmpty(ImagePath))
        {
            await AnalyzeImageAsync();
            return;
        }

        try
        {
            FileResult? result = null;
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select a photo"
                });
            }
            else
            { // Mobile
                // Launch camera capture on appearing
                if (!MediaPicker.IsCaptureSupported)
                {
                    _errorHandler.HandleError(new Exception("Camera is not available on this device"));
                    await GoBackAsync();
                    return;
                }

                result = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = "Take a photo"
                });
            }

            if (result != null)
                {
                    // Save the file into local storage and set ImageSource
                    ImagePath = Path.Combine(FileSystem.CacheDirectory, result.FileName);

                    using Stream sourceStream = await result.OpenReadAsync();
                    using FileStream localFileStream = File.OpenWrite(ImagePath);

                    await sourceStream.CopyToAsync(localFileStream);
                    ImageSource = ImageSource.FromFile(ImagePath);

                    // await AnalyzeImageAsync();
                }
                else
                {
                    // User cancelled
                    await GoBackAsync();
                }
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
            await GoBackAsync();
        }
    }

    [RelayCommand]
    async Task AnalyzeImageAsync()
    {
        try
        {
            IsBusy = true;
            IsAnalyzingContext = true;
            AnalysisStatusTitle = "Processing Photo";
            AnalysisStatusDetail = "Detecting text and visual content...";

            _stopwatch.Restart();

            // Analyze the image using AI
            await ExtractTasksFromImageAsync();

            _stopwatch.Stop();
            _logger.LogInformation("Photo analysis completed in {AnalysisDuration}ms",
                _stopwatch.ElapsedMilliseconds);

            // Set phase to reviewing to show the results
            Phase = PhotoPhase.Reviewing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image");
            _errorHandler.HandleError(ex);
        }
        finally
        {
            IsAnalyzingContext = false;
            IsBusy = false;
        }
    }
    
    private async Task ExtractTasksFromImageAsync()
    {
        if (!_chatClientService.IsInitialized)
        {
            _logger.LogError("ChatClient is not initialized");
            AnalysisStatusDetail = "Error: AI services not initialized";
            throw new InvalidOperationException("Chat client not initialized");
        }
        
        try
        {
            AnalysisStatusDetail = "Extracting tasks from image content...";
            
            // Build the prompt for the AI model
            var prompt = new System.Text.StringBuilder();
            prompt.AppendLine("# Image Analysis Task");
            prompt.AppendLine("Analyze the image for task lists, to-do items, notes, or any content that could be organized into projects and tasks.");
            prompt.AppendLine();
            prompt.AppendLine("## Instructions:");
            prompt.AppendLine("1. Identify any projects and tasks (to-do items) visible in the image");
            prompt.AppendLine("2. Format handwritten text, screenshots, or photos of physical notes into structured data");
            prompt.AppendLine("3. Group related tasks into projects when appropriate");

            if (!string.IsNullOrEmpty(AnalysisInstructions))
            {
                prompt.AppendLine($"4. {AnalysisInstructions}");
            }
            prompt.AppendLine();
            prompt.AppendLine("If no projects/tasks are found, return an empty projects array.");
            
            // Call the AI service with the image
            var client = _chatClientService.GetClient();
            if (client == null)
            {
                throw new InvalidOperationException("Could not get chat client");
            }

            byte[] imageBytes = await File.ReadAllBytesAsync(ImagePath);
            
            var msg = new Microsoft.Extensions.AI.ChatMessage(ChatRole.User,
            [
                new TextContent(prompt.ToString()),
                new DataContent(imageBytes, mediaType: "image/png")
            ]);
            
            var apiResponse = await client.GetResponseAsync<ProjectsJson>(msg);
            
            if (apiResponse?.Result?.Projects != null)
            {
                // Transform the API response into our model
                Projects = apiResponse.Result.Projects
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                    .ToList();
                
                // For projects that don't have a name, add a default name
                for (int i = 0; i < Projects.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(Projects[i].Name))
                    {
                        Projects[i].Name = $"Project {i + 1}";
                    }
                }
            }
            
            if (Projects.Count > 0)
            {
                AnalysisStatusDetail = $"Successfully extracted {Projects.Count} projects and {Projects.Sum(p => p.Tasks.Count)} tasks!";
            }
            else
            {
                AnalysisStatusDetail = "No tasks found in the image. Try again with a clearer image.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting tasks from image");
            AnalysisStatusDetail = "Error extracting tasks: " + ex.Message;
            throw;
        }
    }

    [RelayCommand]
    private Task TaskCompleted(ProjectTask task)
    {
        task.IsCompleted = !task.IsCompleted;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Navigate to the task detail page for a specific task
    /// </summary>
    [RelayCommand]
    private Task NavigateToTask(ProjectTask? task)
    {
        if (task == null) return Task.CompletedTask;

        _logger.LogInformation("Navigating to task details page for task: {TaskTitle}", task.Title);
        return Shell.Current.GoToAsync($"task?id={task.ID}");
    }

    [RelayCommand]
    private Task AcceptRecommendation(ProjectTask task)
    {
        // Mark as not a recommendation anymore
        task.IsRecommendation = false;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task RejectRecommendation(ProjectTask task)
    {
        // Find and remove the task from its project
        foreach (var project in Projects)
        {
            if (project.Tasks.Contains(task))
            {
                project.Tasks.Remove(task);
                break;
            }
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void DeleteProject(Project? project)
    {
        if (project != null && Projects.Contains(project))
        {
            Projects.Remove(project);
        }
    }

    [RelayCommand]
    private void DeleteTask(ProjectTask? task)
    {
        if (task == null) return;
        
        foreach (var project in Projects)
        {
            if (project.Tasks.Contains(task))
            {
                project.Tasks.Remove(task);
                break;
            }
        }
    }

    [RelayCommand]
    private async Task ReanalyzeAsync()
    {
        Phase = PhotoPhase.Analyzing;
        Projects.Clear();
        await AnalyzeImageAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            AnalysisStatusTitle = "Saving Tasks";
            AnalysisStatusDetail = "Adding your tasks to the database...";
            
            int savedProjects = 0;
            int savedTasks = 0;
            
            // Save each project and its tasks
            foreach (var project in Projects.Where(p => p.Tasks.Any()))
            {
                // Save project
                await _projectRepository.SaveItemAsync(project);
                savedProjects++;
                
                // Save tasks
                foreach (var task in project.Tasks)
                {
                    task.ProjectID = project.ID;
                    await _taskRepository.SaveItemAsync(task);
                    savedTasks++;
                }
            }
            
            // Show completion message
            await AppShell.DisplayToastAsync($"Saved {savedProjects} projects with {savedTasks} tasks!");
            
            // Return to main page
            await GoBackAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tasks from photo");
            _errorHandler.HandleError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // No longer needed for photo capture navigation
    }

    /// <summary>
    /// Handle assist action on a task using the TaskAssistHandler service
    /// </summary>
    [RelayCommand]
    private async Task Assist(ProjectTask task)
    {
        if (task == null || task.AssistType == AssistType.None)
            return;
            
        try
        {
            IsBusy = true;
            await _taskAssistHandler.HandleAssistAsync(task, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing assist action");
            _errorHandler.HandleError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
