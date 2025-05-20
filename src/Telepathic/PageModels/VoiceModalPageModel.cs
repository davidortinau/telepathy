using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using Telepathic.Models;
using Telepathic.Services;

namespace Telepathic.PageModels;

public enum VoicePhase { Recording, Transcribing, Reviewing }

public partial class VoiceModalPageModel : ObservableObject, IProjectTaskPageModel
{
    readonly IAudioManager _audioManager;

    IAudioSource? _audioSource = null;

    IAudioRecorder? _recorder;
    readonly ITranscriptionService _transcriber;
    readonly ModalErrorHandler _errorHandler;
    readonly IChatClientService _chatClientService;
    readonly ILogger<VoiceModalPageModel> _logger;

    [ObservableProperty] bool isRecording;
    [ObservableProperty] bool isBusy;
    [ObservableProperty] VoicePhase phase = VoicePhase.Recording;
    [ObservableProperty] string recordButtonText = "🎤 Record";
    [ObservableProperty] string transcript = string.Empty;

    // Status indicator properties
    [ObservableProperty] bool isAnalyzingContext;
    [ObservableProperty] string analysisStatusTitle = "Processing";
    [ObservableProperty] string analysisStatusDetail = "Preparing to analyze your recording...";

    // Extracted projects and tasks
    [ObservableProperty] List<Project> projects = new();
    
    // Priority options for pickers
    public ObservableCollection<int?> PriorityOptions { get; } = new() { null, 1, 2, 3, 4, 5 };

    private readonly ProjectRepository _projectRepository;
    private readonly TaskRepository _taskRepository;

    // Stopwatch for measuring performance
    private Stopwatch _stopwatch = new();

    public VoiceModalPageModel(
        IAudioManager audioManager,
        ITranscriptionService transcriber,
        ModalErrorHandler errorHandler,
        IChatClientService chatClientService,
        ProjectRepository projectRepository,
        TaskRepository taskRepository,
        ILogger<VoiceModalPageModel> logger)
    {
        // _audio = audio;
        _audioManager = audioManager;
        _transcriber = transcriber;
        _errorHandler = errorHandler;
        _chatClientService = chatClientService;
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _logger = logger;

        _logger.LogInformation("Voice Modal Page Model initialized");
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
    private async Task ToggleRecordingAsync()
    {
        if (!IsRecording)
        {
            try
            {
                // Check for microphone permissions first
                _logger.LogInformation("Checking microphone permissions");
                var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    _logger.LogInformation("Requesting microphone permissions");
                    status = await Permissions.RequestAsync<Permissions.Microphone>();
                    if (status != PermissionStatus.Granted)
                    {
                        _logger.LogWarning("Microphone permission denied");
                        // Permission denied - offer fallback
                        bool navigateToManual = await Shell.Current.DisplayAlertAsync(
                            "Microphone Access Denied",
                            "Voice recording requires microphone access. Would you like to enter tasks manually instead?",
                            "Enter Manually", "Cancel");

                        if (navigateToManual)
                        {
                            _logger.LogInformation("User chose manual task entry after permission denial");
                            // Navigate to manual task entry
                            await Shell.Current.GoToAsync("task");
                            await Shell.Current.Navigation.PopModalAsync(); // Close this modal
                        }
                        return;
                    }
                }

                _logger.LogInformation("Starting voice recording");
                _stopwatch.Restart();
                _recorder = _audioManager.CreateRecorder();
                await _recorder.StartAsync();
                // await _audio.StartRecordingAsync(CancellationToken.None);
                IsRecording = true;
                RecordButtonText = "⏹ Stop";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting recording");
                _errorHandler.HandleError(ex);
            }
        }
        else
        {
            try
            {
                if (_recorder == null)
                {
                    _logger.LogWarning("Recorder is null - cannot stop recording");
                    return;
                }

                _audioSource = await _recorder.StopAsync();
                IsRecording = false;
                RecordButtonText = "🎤 Record";

                // Log recording duration
                _stopwatch.Stop();
                _logger.LogInformation("Voice recording completed in {RecordingDuration}ms", _stopwatch.ElapsedMilliseconds);

                Phase = VoicePhase.Transcribing;

                // Now we'll actually transcribe the audio!
                await TranscribeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping recording");
                _errorHandler.HandleError(ex);
                IsRecording = false;
                RecordButtonText = "🎤 Record";
            }
        }
    }

    private async Task TranscribeAsync()
    {
        try
        {
            IsBusy = true;
            IsAnalyzingContext = true;
            AnalysisStatusTitle = "Processing Audio";
            AnalysisStatusDetail = "Preparing your recording for transcription...";
            
            // Create a temporary file path to save our recording
            string audioFilePath = Path.Combine(FileSystem.CacheDirectory, $"recording_{DateTime.Now:yyyyMMddHHmmss}.wav");
            
            _logger.LogInformation("Saving audio to temporary file at {FilePath}", audioFilePath);
            
            // Save the audio source to a file
            if (_audioSource != null)
            {
                AnalysisStatusDetail = "Saving audio recording...";
                await using (var fileStream = File.Create(audioFilePath))
                {
                    var audioStream = _audioSource.GetAudioStream();
                    await audioStream.CopyToAsync(fileStream);
                }
                
                _logger.LogInformation("Audio successfully saved to file");
            }
            else
            {
                _logger.LogError("Audio source is null - no recording available");
                throw new InvalidOperationException("No recording is available to transcribe");
            }
            
            // Verify the file exists
            if (!File.Exists(audioFilePath))
            {
                _logger.LogError("Recorded audio file not found at {FilePath}", audioFilePath);
                throw new FileNotFoundException("Recorded audio file not found");
            }

            // Transcribe the audio using Whisper
            _logger.LogInformation("Starting audio transcription");
            _stopwatch.Restart();
            
            AnalysisStatusTitle = "Transcribing";
            AnalysisStatusDetail = "Converting your voice to text using AI...";
            
            Transcript = await _transcriber.TranscribeAsync(audioFilePath, CancellationToken.None);
            _stopwatch.Stop();
            _logger.LogInformation("Audio transcription completed in {TranscriptionDuration}ms, length: {TranscriptLength}",
                _stopwatch.ElapsedMilliseconds, Transcript?.Length ?? 0);

            AnalysisStatusTitle = "Analyzing";
            AnalysisStatusDetail = "Identifying projects and tasks from your recording...";
            
            // Extract projects and tasks from the transcript
            await ExtractTasksAsync();

            // After successful transcription and extraction, move to review phase
            Phase = VoicePhase.Reviewing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed");
            _errorHandler.HandleError(ex);
            // Return to recording phase if transcription fails
            Phase = VoicePhase.Recording;
        }
        finally
        {
            IsBusy = false;
            IsAnalyzingContext = false;
        }
    }

    /// <summary>
    /// Extract projects and tasks from the transcript using AI
    /// </summary>
    private async Task ExtractTasksAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Transcript) || !_chatClientService.IsInitialized)
            {
                _logger.LogWarning("Cannot extract tasks: transcript is empty or chat client is not initialized");
                return;
            }

            // Clear previous extraction results
            Projects.Clear();

            _logger.LogInformation("Starting task extraction from transcript");
            _stopwatch.Restart();
            
            AnalysisStatusTitle = "Analyzing Content";
            AnalysisStatusDetail = "Using AI to identify tasks and projects in your recording...";

            // ignore the audio and just see if we can get something meaningful from this text
            Transcript = "This week we are going to the Good Friday service at church, but we need to get Nolan from the airport around 9:30. This weekend we have an easter egg hunt at church and then after church Sunday morning we are going to Mammy's house for lunch and an egg hunt. We need to take a dish and the bag of candy for filling eggs.";

            // Create a prompt that will extract projects and tasks from the transcript
            var prompt = $@"
Extract projects and tasks from this voice memo transcript. 
Analyze the text to identify actionable tasks I need to keep track of. Use the following instructions:
1. Tasks are actionable items that can be completed, such as 'Buy groceries' or 'Call Mom'.
2. Projects are larger tasks that may contain multiple smaller tasks, such as 'Plan birthday party' or 'Organize closet'.
3. Tasks must be grouped under a project and cannot be grouped under multiple projects.
4. Any mentioned due dates use the YYYY-MM-DD format

Here's the transcript: {Transcript}";

            // Get response from the AI service
            var chatClient = _chatClientService.GetClient();
            var response = await chatClient.GetResponseAsync<ProjectsJson>(prompt);

            _stopwatch.Stop();
            _logger.LogInformation("Task extraction completed in {ExtractionDuration}ms", _stopwatch.ElapsedMilliseconds);
            

            if (response?.Result != null)
            {
                // Mark all extracted tasks as recommendations
                foreach (var project in response.Result.Projects)
                {
                    foreach (var task in project.Tasks)
                    {
                        task.IsRecommendation = true;
                    }
                }

                Projects = response.Result.Projects;
                
                _logger.LogInformation("Found {NumberOfProjects} projects", Projects.Count);
                _logger.LogInformation("Found {NumberOfTasks} tasks", Projects.Sum(p => p.Tasks.Count));

                // Add all projects and tasks to observable collections
                // foreach (var aiProject in response.Result.Projects)
                // {
                //     // Convert AIProject to ProjectVm
                //     var project = new ProjectVm
                //     {
                //         Name = aiProject.Name,
                //         Tasks = aiProject.Tasks.Select(t => new TaskVm
                //         {
                //             Title = t.Title,
                //             DueDate = t.DueDate,
                //             Priority = t.Priority
                //         }).ToList()
                //     };
                //     Projects.Add(project);
                // }

                // foreach (var aiTask in response.Result.StandaloneTasks)
                // {
                //     // Convert AITask to TaskVm
                //     var task = new TaskVm
                //     {
                //         Title = aiTask.Title,
                //         DueDate = aiTask.DueDate,
                //         Priority = aiTask.Priority
                //     };
                //     StandaloneTasks.Add(task);
                // }


                // Check if no projects or tasks were detected
                if (Projects.Count == 0)
                {
                    _logger.LogWarning("No projects or tasks detected in transcript");
                    await Shell.Current.DisplayAlertAsync(
                        "No Tasks Detected",
                        "No projects or tasks were detected in your voice memo. Would you like to try again?",
                        "OK");

                    // Return to recording phase
                    await ReRecordAsync();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task extraction failed");
            _errorHandler.HandleError(ex);
        }
    }

    /// <summary>
    /// Delete a project and its tasks from the list
    /// </summary>
    [RelayCommand]
    private void DeleteProject(Project? project)
    {
        if (project != null)
        {
            Projects.Remove(project);
            _logger.LogInformation("Deleted project: {ProjectName}", project.Name);
        }
    }

    /// <summary>
    /// Delete a task from its project
    /// </summary>
    [RelayCommand]
    private void DeleteTask(ProjectTask? task)
    {
        if (task == null) return;

        foreach (var project in Projects)
        {
            if (project.Tasks.Contains(task))
            {
                project.Tasks.Remove(task);
                _logger.LogInformation("Deleted task: {TaskTitle} from project: {ProjectName}",
                    task.Title, project.Name);
                break;
            }
        }
    }

    [RelayCommand]
	private Task TaskCompleted(ProjectTask task)
	{
		return _taskRepository.SaveItemAsync(task);
	}

    /// <summary>
    /// Accept a recommended task and add it to its project
    /// </summary>
    [RelayCommand]
    private Task AcceptRecommendation(ProjectTask task)
    {

        // Mark the task as no longer a recommendation
        task.IsRecommendation = false;

        _logger.LogInformation("Accepted recommended task: {TaskTitle}", task.Title);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reject a recommended task
    /// </summary>
    [RelayCommand]
    private Task RejectRecommendation(ProjectTask task)
    {
        // Find and remove the task from its project
        foreach (var project in Projects)
        {
            if (project.Tasks.Contains(task))
            {
                project.Tasks.Remove(task);
                _logger.LogInformation("Rejected recommended task: {TaskTitle} from project: {ProjectName}",
                    task.Title, project.Name);
                break;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Start the recording process over
    /// </summary>
    [RelayCommand]
    private async Task ReRecordAsync()
    {
        _logger.LogInformation("Re-starting recording process");

        // Reset everything back to initial state
        Phase = VoicePhase.Recording;
        Transcript = string.Empty;
        Projects.Clear();

        // Wait a moment to ensure UI updates
        await Task.Delay(100);
    }

    /// <summary>
    /// Save all projects and tasks
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            _logger.LogInformation("Starting save operation for voice memo");
            _stopwatch.Restart();
            IsBusy = true;

            int projectCount = 0;
            int taskCount = 0;

            // Save each project and its tasks
            foreach (var projectVm in Projects)
            {

                // Save the project to get its ID
                await _projectRepository.SaveItemAsync(projectVm);
                projectCount++;

                // Save each task associated with this project
                foreach (var taskVm in projectVm.Tasks)
                {
                    taskVm.ProjectID = projectVm.ID; // Set the project ID for the task
                    await _taskRepository.SaveItemAsync(taskVm);
                    taskCount++;
                }
            }

            _stopwatch.Stop();
            _logger.LogInformation("Voice memo saved successfully: {ProjectCount} projects and {TaskCount} tasks in {SaveDuration}ms",
                projectCount, taskCount, _stopwatch.ElapsedMilliseconds);

            // Close the modal
            await Shell.Current.GoToAsync("..");

            // Notify the user that everything was saved
            await Shell.Current.DisplayAlertAsync("Success", "Your projects and tasks are save and secure.", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving voice memo data");
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
        _logger.LogInformation("Navigating back from voice modal");
        await Shell.Current.GoToAsync("..");
    }
}
