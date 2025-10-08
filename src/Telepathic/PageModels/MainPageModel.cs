using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.CalendarStore;
using System.Collections.ObjectModel;
using Telepathic.Models;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Telepathic.ViewModels;
using Telepathic.Tools;

namespace Telepathic.PageModels;

public partial class MainPageModel : ObservableObject, IProjectTaskPageModel
{
	private bool _isNavigatedTo;
	private bool _dataLoaded;
	private readonly ProjectRepository _projectRepository;
	private readonly TaskRepository _taskRepository;
	private readonly CategoryRepository _categoryRepository;
	private readonly ModalErrorHandler _errorHandler;
	private readonly SeedDataService _seedDataService;
	private readonly ICalendarStore _calendarStore;
	private readonly IChatClientService _chatClientService;
	private readonly ILogger _logger;
	private readonly TaskAssistHandler _taskAssistHandler;
	private readonly LocationTools _locationTools;
	private CancellationTokenSource? _cancelTokenSource;
	private DateTime _lastPriorityCheck = DateTime.MinValue;
	private const int PRIORITY_CHECK_HOURS = 4;

	[ObservableProperty]
	private List<CategoryChartData> _todoCategoryData = [];

	[ObservableProperty]
	private List<Brush> _todoCategoryColors = [];

	[ObservableProperty]
	private List<ProjectTask> _tasks = [];

	[NotifyPropertyChangedFor(nameof(ShouldShowPriorityTasks))]
	[NotifyPropertyChangedFor(nameof(HasPriorityTasks))]
	[ObservableProperty]
	private ObservableCollection<ProjectTaskViewModel> _priorityTasks = [];

	[ObservableProperty]
	private List<Project> _projects = [];

	[ObservableProperty]
	bool _isBusy;

	[ObservableProperty]
	bool _isRefreshing;

	[ObservableProperty]
	bool _isAnalyzingContext;

	[ObservableProperty]
	string _analysisStatusTitle = "Scanning your task universe";

	[ObservableProperty]
	string _analysisStatusDetail = "Gathering your location, time, and calendar events to determine which tasks require your immediate attention...";

	[NotifyPropertyChangedFor(nameof(ShouldShowPriorityTasks))]
	[ObservableProperty]
	bool _hasPriorityTasks;

	[ObservableProperty]
	private string _today = DateTime.Now.ToString("dddd, MMM d");

	[ObservableProperty]
	private string _personalizedGreeting = "Greetings, Space Adventurer!";

	[ObservableProperty]
	private bool _isSettingsSheetOpen;



	[ObservableProperty]
	private string _openAIApiKey = Preferences.Default.Get("openai_api_key", string.Empty);
	[ObservableProperty]
	private string _googlePlacesApiKey = Preferences.Default.Get("google_places_api_key", string.Empty);

	[ObservableProperty]
	private string _foundryEndpoint = Preferences.Default.Get("foundry_endpoint", string.Empty);

	[ObservableProperty]
	private string _foundryApiKey = Preferences.Default.Get("foundry_api_key", string.Empty);

    [ObservableProperty]
    private string _foundryModel = Preferences.Default.Get("foundry_model", "o4-mini");

    [ObservableProperty]
    private string _foundryDeploymentName = Preferences.Default.Get("foundry_deployment_name", "o4-mini");

    // Entry fields for UI binding
    [ObservableProperty]
	private string _openAIApiKeyEntry;
	[ObservableProperty]
	private string _googlePlacesApiKeyEntry;

	[ObservableProperty]
	private string _foundryEndpointEntry;

	[ObservableProperty]
	private string _foundryApiKeyEntry;

    [ObservableProperty]
    private string _foundryModelEntry;

    [ObservableProperty]
    private string _foundryDeploymentNameEntry;

    [NotifyPropertyChangedFor(nameof(ShouldShowPriorityTasks))]
	[ObservableProperty]
	private bool _isTelepathyEnabled = Preferences.Default.Get("telepathy_enabled", false);

	public bool ShouldShowPriorityTasks => HasPriorityTasks && IsTelepathyEnabled;

	[ObservableProperty]
	private string _calendarButtonText = Preferences.Default.Get("calendar_connected", false) ? "Disconnect" : "Connect";

	[ObservableProperty]
	private string _aboutMeText = Preferences.Default.Get("about_me_text", string.Empty);

	[ObservableProperty]
	private ObservableCollection<CalendarInfo> _userCalendars = new();

	[ObservableProperty]
	private bool _isLoadingCalendars;

	[ObservableProperty]
	private bool _hasLoadedCalendars;

	[ObservableProperty]
	private bool _isLocationEnabled = Preferences.Default.Get("location_enabled", false);

	[ObservableProperty]
	private string _currentLocation = "Location not available";
	[ObservableProperty]
	private bool _isGettingLocation;

	[ObservableProperty]
	private int _cardVisibleIndex = 0;

	public bool HasCompletedTasks
		=> Tasks?.Any(t => t.IsCompleted) ?? false;

	[RelayCommand]	Task AcceptRecommendation(ProjectTask? task)
	{
		if (task != null)
			_logger.LogInformation("Accepting recommendation for task: {TaskTitle}", task.Title);
		return Task.CompletedTask;
	}

	[RelayCommand]
	Task RejectRecommendation(ProjectTask? task)	{
		if (task != null)
			_logger.LogInformation("Rejecting recommendation for task: {TaskTitle}", task.Title);
		return Task.CompletedTask;
	}

	[RelayCommand]
	async Task Assist(ProjectTask task)
	{
		if (task == null || task.AssistType == AssistType.None)
			return;
		try
		{
			IsBusy = true;
			await _taskAssistHandler.HandleAssistAsync(task, IsLocationEnabled);
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
		finally
		{
			IsBusy = false;
		}
	}

	private async Task OpenMaps(string assistData, string taskTitle)
	{
		try
		{
			// Check if AssistData contains lat/long coordinates
			if (!string.IsNullOrWhiteSpace(assistData) && assistData.Contains(","))
			{
				// Try to parse coordinates format "lat, long"
				var parts = assistData.Split(',');
				if (parts.Length == 2 &&
					double.TryParse(parts[0].Trim(), out double lat) &&
					double.TryParse(parts[1].Trim(), out double lng))
				{
					// We have valid coordinates - set course for this sector!
					var location = new Location(lat, lng);
					var options = new MapLaunchOptions { Name = taskTitle };
					await Map.Default.OpenAsync(location, options);
				}
				else
				{
					var placemark = new Placemark
					{
						CountryName = "United States",
						AdminArea = "",
						Thoroughfare = assistData,
						Locality = ""
					};
					// Not valid coordinates - treat as address/place name
					await Map.Default.OpenAsync(placemark);
				}
			}
			else if (!string.IsNullOrWhiteSpace(assistData))
			{
				// Use as place name or address
				var placemark = new Placemark
				{
					CountryName = "United States",
					AdminArea = "",
					Thoroughfare = assistData,
					Locality = ""
				};
				// Not valid coordinates - treat as address/place name
				await Map.Default.OpenAsync(placemark);
			}
			else if (IsLocationEnabled)
			{
				// No location specified - use current cosmic coordinates
				var currentLocation = await Geolocation.GetLastKnownLocationAsync();
				if (currentLocation != null)
				{
					var location = new Location(currentLocation.Latitude, currentLocation.Longitude);
					var options = new MapLaunchOptions { Name = "Current Location" };
					await Map.Default.OpenAsync(location, options);
				}
				else
				{
					await AppShell.DisplayToastAsync("Could not determine your location coordinates. Enable location services or specify a location.");
				}
			}
			else
			{
				await AppShell.DisplayToastAsync("No location specified and location services are disabled.");
			}
		}
		catch (Exception ex)
		{
			await AppShell.DisplayToastAsync($"Failed to navigate to location: {ex.Message}");
		}
	}

	public MainPageModel(SeedDataService seedDataService, ProjectRepository projectRepository,
	TaskRepository taskRepository, CategoryRepository categoryRepository, ModalErrorHandler errorHandler,
	ICalendarStore calendarStore, ILogger<MainPageModel> logger, IChatClientService chatClientService,
	TaskAssistHandler taskAssistHandler, LocationTools locationTools)
	{
		_projectRepository = projectRepository;
		_taskRepository = taskRepository;
		_categoryRepository = categoryRepository;
		_errorHandler = errorHandler;
		_seedDataService = seedDataService;
		_calendarStore = calendarStore;
		_chatClientService = chatClientService;
		_logger = logger;
		_taskAssistHandler = taskAssistHandler;
		_locationTools = locationTools;
		// Sync entry fields for UI
		OpenAIApiKeyEntry = OpenAIApiKey;
		GooglePlacesApiKeyEntry = GooglePlacesApiKey;
		FoundryEndpointEntry = FoundryEndpoint;
		FoundryApiKeyEntry = FoundryApiKey;
        FoundryModelEntry = FoundryModel;
        FoundryDeploymentNameEntry = FoundryDeploymentName;

        _locationTools.SetGooglePlacesApiKey(GooglePlacesApiKey);

		// Load saved calendar choices
		LoadSavedCalendars();

		// Initialize location if enabled
		if (IsLocationEnabled)
		{
			_ = GetCurrentLocationAsync();
		}
	}

	partial void OnOpenAIApiKeyEntryChanged(string value)
	{
		OpenAIApiKey = value;
	}
	partial void OnGooglePlacesApiKeyEntryChanged(string value)
	{
		GooglePlacesApiKey = value;
	}

	partial void OnFoundryEndpointEntryChanged(string value)
	{
		FoundryEndpoint = value;
	}

	partial void OnFoundryApiKeyEntryChanged(string value)
	{
		FoundryApiKey = value;
	}

    partial void OnFoundryModelEntryChanged(string value)
    {
        FoundryModel = value;
    }

    partial void OnFoundryDeploymentNameEntryChanged(string value)
    {
        FoundryDeploymentName = value;
    }

    /// <summary>
    /// Retrieves calendar events for the connected calendars for today
    /// </summary>
    private async Task<List<CalendarEvent>> GetCalendarEventsAsync()
	{
		var results = new List<CalendarEvent>();

		try
		{
			// Only proceed if we have selected calendars
			if (!UserCalendars.Any(c => c.IsSelected))
				return results;

			// Get events for today
			var today = DateTime.Today;
			var tomorrow = today.AddDays(1);

			// Get all calendars first
			var calendars = await _calendarStore.GetCalendars();

			// Filter to only selected calendars by ID
			var selectedCalendarIds = UserCalendars.Where(c => c.IsSelected).Select(c => c.Id).ToHashSet();
			var selectedCalendars = calendars.Where(c => selectedCalendarIds.Contains(c.Id)).ToList();

			foreach (var calendar in selectedCalendars)
			{
				try
				{
					// Get events from the calendar for today
					var events = await _calendarStore.GetEvents(calendar.Id, today, tomorrow);
					results.AddRange(events);
				}				catch (Exception ex)
				{
					// Log but continue with other calendars
					_logger.LogWarning(ex, "Error getting events for calendar {CalendarName}", calendar.Name);
				}
			}
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}

		return results;
	}

	private async Task LoadData()
	{
		try
		{
			IsBusy = true;

			Projects = await _projectRepository.ListAsync();

			var chartData = new List<CategoryChartData>();
			var chartColors = new List<Brush>();

			var categories = await _categoryRepository.ListAsync();
			foreach (var category in categories)
			{
				chartColors.Add(category.ColorBrush);

				var ps = Projects.Where(p => p.CategoryID == category.ID).ToList();
				int tasksCount = ps.SelectMany(p => p.Tasks).Count();

				chartData.Add(new(category.Title, tasksCount));
			}

			TodoCategoryData = chartData;
			TodoCategoryColors = chartColors;

			Tasks = await _taskRepository.ListAsync();
		}
		finally
		{
			IsBusy = false;
			OnPropertyChanged(nameof(HasCompletedTasks));
		}
	}

	private async Task InitData(SeedDataService seedDataService)
	{
		bool isSeeded = Preferences.Default.ContainsKey("is_seeded");

		if (!isSeeded)
		{
			await seedDataService.LoadSeedDataAsync();
			Preferences.Default.Set("is_seeded", true);
			await Refresh();
		}
		// If already seeded, do not call Refresh here; Appearing/Refresh will handle it.
	}

	[RelayCommand]
	private async Task Refresh(bool hideIndicator = false)
	{
		try
		{
			if (!hideIndicator)
				IsRefreshing = true;

			await LoadData();
			await AnalyzeAndPrioritizeTasks();
		}
		catch (Exception e)
		{
			_errorHandler.HandleError(e);
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	[RelayCommand]
	private void NavigatedTo() =>
		_isNavigatedTo = true;

	[RelayCommand]
	private void NavigatedFrom() =>
		_isNavigatedTo = false;
	[RelayCommand]
	private async Task Appearing()
	{
		if (!_dataLoaded)
		{
			await InitData(_seedDataService);
			_dataLoaded = true;
			await Refresh(true); // Always refresh after InitData to load data and trigger AI
		}
		else if (!_isNavigatedTo)
		{
			await Refresh(true);
		}
		// No direct call to AnalyzeAndPrioritizeTasks here; Refresh handles it.
	}
	[RelayCommand]
	private async Task Completed(ProjectTask task)
	{
		// Get the task from the database first to ensure we're working with the most up-to-date version
		var dbTask = await _taskRepository.GetAsync(task.ID);
		if (dbTask != null)
		{
			// Update the database task with the completion status
			dbTask.IsCompleted = task.IsCompleted;
			await _taskRepository.SaveItemAsync(dbTask);
		}
		else
		{
			// If not found in DB, save the passed task
			await _taskRepository.SaveItemAsync(task);
		}

		// Always update both collections to keep them in sync, regardless of where the change originated

		// Update the task in the main Tasks collection
		var mainTask = Tasks.FirstOrDefault(t => t.ID == task.ID);
		if (mainTask != null)
		{
			mainTask.IsCompleted = task.IsCompleted;
		}

		// Update the task in the PriorityTasks collection if present
		var priorityTask = PriorityTasks.FirstOrDefault(t => t.ID == task.ID);
		if (priorityTask != null)
		{
			// Updating through the property ensures OnPropertyChanged is called
			priorityTask.IsCompleted = task.IsCompleted;
		}

		// Always refresh both collections to ensure UI is consistent
		Tasks = new(Tasks);
		PriorityTasks = new(PriorityTasks);

		// Update the HasCompletedTasks property
		OnPropertyChanged(nameof(HasCompletedTasks));
	}

	[RelayCommand]
	private async Task AddTask()
	{
		try
		{
			await Shell.Current.GoToAsync($"task");
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
	}


	[RelayCommand]
	private Task NavigateToProject(Project project)
		=> Shell.Current.GoToAsync($"project?id={project.ID}");

	[RelayCommand]
	private async Task NavigateToTask(ProjectTask? task)
	{
		try
		{
			if (task != null)
			{
				await Shell.Current.GoToAsync($"task?id={task.ID}");
			}
		}
		catch (Exception ex)
		{
			await AppShell.DisplayToastAsync($"Navigation error: {ex.Message}");
			_logger.LogError(ex, "Failed to navigate to task ID {TaskId}", task?.ID);
			_errorHandler.HandleError(ex);
		}
	}

	[RelayCommand]
	private async Task CleanTasks()
	{
		// Get all completed tasks
		var completedTasks = Tasks.Where(t => t.IsCompleted).ToList();

		// Delete completed tasks from database
		foreach (var task in completedTasks)
		{
			await _taskRepository.DeleteItemAsync(task);
		}

		// Create fresh filtered collections instead of modifying existing ones
		var incompleteTasks = Tasks.Where(t => !t.IsCompleted).ToList();
		Tasks = new(incompleteTasks);

		// Get IDs of all completed tasks for efficient lookup
		var completedTaskIds = completedTasks.Select(t => t.ID).ToHashSet();

		// Filter out completed tasks from priority tasks
		var remainingPriorityTasks = PriorityTasks
			.Where(pt => !completedTaskIds.Contains(pt.ID))
			.ToList();

		// Replace entire collection
		PriorityTasks = new ObservableCollection<ProjectTaskViewModel>(remainingPriorityTasks);

		// Now the HasCompletedTasks property should return false since we've removed all completed tasks
		OnPropertyChanged(nameof(HasCompletedTasks));

		await AppShell.DisplayToastAsync("All cleaned up!");
	}

	partial void OnIsTelepathyEnabledChanged(bool value)
	{
		Preferences.Default.Set("telepathy_enabled", value);
		OnPropertyChanged(nameof(ShouldShowPriorityTasks));
	}


	partial void OnOpenAIApiKeyChanged(string value)
	{
		_logger.LogInformation($"OpenAI API Key changed");
		Preferences.Default.Set("openai_api_key", value);
	}
	partial void OnGooglePlacesApiKeyChanged(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
			Preferences.Default.Set("google_places_api_key", value);
		else
			Preferences.Default.Remove("google_places_api_key");
		_locationTools.SetGooglePlacesApiKey(value);
	}

	partial void OnFoundryEndpointChanged(string value)
	{
		_logger.LogInformation($"Foundry Endpoint changed");
		if (!string.IsNullOrWhiteSpace(value))
			Preferences.Default.Set("foundry_endpoint", value);
		else
			Preferences.Default.Remove("foundry_endpoint");
	}

	partial void OnFoundryApiKeyChanged(string value)
	{
		_logger.LogInformation($"Foundry API Key changed");
		if (!string.IsNullOrWhiteSpace(value))
			Preferences.Default.Set("foundry_api_key", value);
		else
			Preferences.Default.Remove("foundry_api_key");
	}

    partial void OnFoundryModelChanged(string value)
    {
        _logger.LogInformation($"Foundry Model changed");
        if (!string.IsNullOrWhiteSpace(value))
            Preferences.Default.Set("foundry_model", value);
        else
            Preferences.Default.Remove("foundry_model");
    }

    partial void OnFoundryDeploymentNameChanged(string value)
    {
        _logger.LogInformation($"Foundry Deployment Name changed");
        if (!string.IsNullOrWhiteSpace(value))
            Preferences.Default.Set("foundry_deployment_name", value);
        else
            Preferences.Default.Remove("foundry_deployment_name");
    }

	partial void OnAboutMeTextChanged(string value)
	{
		Preferences.Default.Set("about_me_text", value);
	}

	[RelayCommand]
	private async Task ToggleSettings()
	{
		// Toggle the settings sheet open/closed state
		IsSettingsSheetOpen = !IsSettingsSheetOpen;
		// When closing settings panel, check if we need to analyze tasks
		if (!IsSettingsSheetOpen && IsTelepathyEnabled && !HasPriorityTasks)
		{
			await AnalyzeAndPrioritizeTasks();
		}
	}
	[RelayCommand]
	private async Task SaveApiKey()
	{
		_logger.LogInformation($"API Keys and settings saved");
		
		// Save all API keys and settings
		Preferences.Default.Set("openai_api_key", OpenAIApiKey);
		if (!string.IsNullOrWhiteSpace(FoundryEndpoint))
			Preferences.Default.Set("foundry_endpoint", FoundryEndpoint);
		if (!string.IsNullOrWhiteSpace(FoundryApiKey))
			Preferences.Default.Set("foundry_api_key", FoundryApiKey);
        if (!string.IsNullOrWhiteSpace(FoundryModel))
            Preferences.Default.Set("foundry_model", FoundryModel);
        if (!string.IsNullOrWhiteSpace(FoundryDeploymentName))
            Preferences.Default.Set("foundry_deployment_name", FoundryDeploymentName);

        // Update the chat client with the new settings
        try
		{
			// Determine which provider to use based on available settings
			if (!string.IsNullOrWhiteSpace(FoundryEndpoint) && !string.IsNullOrWhiteSpace(FoundryApiKey) && !string.IsNullOrWhiteSpace(FoundryModel) && !string.IsNullOrWhiteSpace(FoundryDeploymentName) )
			{
				_chatClientService.UpdateClient(FoundryApiKey, "foundry", FoundryEndpoint, FoundryModel,FoundryDeploymentNameEntry);
				await AppShell.DisplayToastAsync("Foundry settings saved and chat client updated!");
			}
			else if (!string.IsNullOrWhiteSpace(OpenAIApiKey))
			{
				_chatClientService.UpdateClient(OpenAIApiKey);
				await AppShell.DisplayToastAsync("OpenAI API Key saved and chat client updated!");
			}
			else
			{
				await AppShell.DisplayToastAsync("Settings saved! Please provide either OpenAI or Foundry credentials to enable AI features.");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to update chat client with new settings");
			await AppShell.DisplayToastAsync("Settings saved, but failed to initialize chat client. Please check your credentials and try again.");
		}
	}

	private void LoadSavedCalendars()
	{
		var savedCalendarsJson = Preferences.Default.Get("saved_calendars", string.Empty);
		if (!string.IsNullOrEmpty(savedCalendarsJson))
		{
			try
			{
				var savedCalendars = System.Text.Json.JsonSerializer.Deserialize<List<CalendarInfo>>(savedCalendarsJson);
				if (savedCalendars != null)
				{
					UserCalendars = new ObservableCollection<CalendarInfo>(savedCalendars);
					HasLoadedCalendars = true;
				}
			}
			catch
			{
				// Silent fail, will reload calendars
			}
		}
	}

	private void SaveSelectedCalendars()
	{
		var calendarsJson = System.Text.Json.JsonSerializer.Serialize(UserCalendars);
		Preferences.Default.Set("saved_calendars", calendarsJson);
	}

	[RelayCommand]
	private async Task LoadCalendars()
	{
		if (IsLoadingCalendars)
			return;

		try
		{
			IsLoadingCalendars = true;

			var calendars = await _calendarStore.GetCalendars();
			var tempCalendars = new List<CalendarInfo>();

			// If we already have saved calendars, preserve selections
			var existingCalendars = UserCalendars.ToDictionary(c => c.Id, c => c);

			foreach (var calendar in calendars)
			{
				bool isSelected = existingCalendars.ContainsKey(calendar.Id) && existingCalendars[calendar.Id].IsSelected;
				tempCalendars.Add(new CalendarInfo(calendar.Id, calendar.Name, isSelected));
			}

			UserCalendars = new ObservableCollection<CalendarInfo>(tempCalendars);
			SaveSelectedCalendars();

			HasLoadedCalendars = true;
			CalendarButtonText = UserCalendars.Any(c => c.IsSelected) ? "Manage Calendars" : "Connect";
			Preferences.Default.Set("calendar_connected", UserCalendars.Any(c => c.IsSelected));
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
		finally
		{
			IsLoadingCalendars = false;
		}
	}

	[RelayCommand]
	private async Task ToggleCalendarSelection(CalendarInfo calendar)
	{
		SaveSelectedCalendars();

		CalendarButtonText = UserCalendars.Any(c => c.IsSelected) ? "Manage Calendars" : "Connect";
		Preferences.Default.Set("calendar_connected", UserCalendars.Any(c => c.IsSelected));
		await AppShell.DisplayToastAsync($"Calendar '{calendar.Name}' {(calendar.IsSelected ? "connected" : "disconnected")}!");
	}

	[RelayCommand]
	private async Task ToggleCalendar()
	{
		await LoadCalendars();
		// if (!HasLoadedCalendars)
		// {
		// 	await LoadCalendars();
		// }
		// else
		// {
		// 	// We already have calendars, so this is just to show the calendar section
		// }
	}

	[RelayCommand]
	private async Task DisconnectAllCalendars()
	{
		foreach (var calendar in UserCalendars)
		{
			calendar.IsSelected = false;
		}

		SaveSelectedCalendars();
		CalendarButtonText = "Connect";
		Preferences.Default.Set("calendar_connected", false);
		await AppShell.DisplayToastAsync("All calendars disconnected!");
	}

	partial void OnIsLocationEnabledChanged(bool value)
	{
		Preferences.Default.Set("location_enabled", value);

		if (value)
		{
			_ = GetCurrentLocationAsync();
		}
		else
		{
			CurrentLocation = "Location not available";
			if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested)
				_cancelTokenSource.Cancel();
		}
	}

	[RelayCommand]
	public async Task RefreshLocationAsync()
	{
		if (IsLocationEnabled)
		{
			await GetCurrentLocationAsync();
		}
	}

	[RelayCommand]
	private async Task ForcePriorityTaskRefresh()
	{
		// This method will force a refresh of priority tasks, ignoring the time constraint
		// by resetting _lastPriorityCheck to a distant past value and calling AnalyzeAndPrioritizeTasks
		_lastPriorityCheck = DateTime.MinValue;
		await AnalyzeAndPrioritizeTasks();
	}

	public async Task GetCurrentLocationAsync()
	{
		try
		{
			IsGettingLocation = true;
			_cancelTokenSource = new CancellationTokenSource();

			var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
			var location = await Geolocation.GetLocationAsync(request, _cancelTokenSource.Token);

			if (location != null)
			{
				// Update display string
				CurrentLocation = $"Lat: {location.Latitude:F4}, Long: {location.Longitude:F4}";

				// Update the location in the location service
				_locationTools.SetCurrentLocation(location.Latitude, location.Longitude);
			}
			else
			{
				CurrentLocation = "Location unavailable";
			}
		}
		catch (FeatureNotSupportedException)
		{
			CurrentLocation = "Location not supported on this device";
		}
		catch (PermissionException)
		{
			CurrentLocation = "Location permission not granted";
		}
		catch (Exception ex)
		{
			CurrentLocation = $"Error: {ex.Message}";
		}
		finally
		{
			IsGettingLocation = false;
		}
	}

	/// <summary>
	/// Analyzes tasks based on calendar events, location, time of day, and personal preferences
	/// to identify priority tasks that should be highlighted to the user.
	/// </summary>
	private async Task AnalyzeAndPrioritizeTasks()
	{
		// Early exit if telepathy is disabled or we're missing the API client
		if (!IsTelepathyEnabled || !_chatClientService.IsInitialized || (string.IsNullOrWhiteSpace(OpenAIApiKey) && string.IsNullOrWhiteSpace(FoundryApiKey)))
		{
			PriorityTasks = [];
			HasPriorityTasks = false;
			IsAnalyzingContext = false;
			return;
		}
		// Check if we already have priority tasks that aren't completed yet
		var incompletePriorityTasks = PriorityTasks.Where(t => !t.IsCompleted).ToList();
		var timeToRecheck = (DateTime.Now - _lastPriorityCheck).TotalHours >= PRIORITY_CHECK_HOURS;

		// Don't reanalyze if we have incomplete priority tasks and it hasn't been 4 hours
		if (incompletePriorityTasks.Any() && !timeToRecheck)
		{
			// Keep existing prioritized tasks
			HasPriorityTasks = incompletePriorityTasks.Any();
			return;
		}

		// Continue with analysis - we need to generate new priority tasks
		PriorityTasks.Clear();
		HasPriorityTasks = false;

		try
		{
			IsAnalyzingContext = true;
			AnalysisStatusTitle = "Scanning your task universe";
			AnalysisStatusDetail = "Gathering your location, time, and calendar events to determine which tasks require your immediate attention...";

			// Get calendar events
			var events = await GetCalendarEventsAsync();
			AnalysisStatusDetail = "Analyzing calendar events and tasks...";

			// Create a context description for the AI
			var sb = new System.Text.StringBuilder();

			// Add basic context information
			sb.AppendLine("CONTEXT INFORMATION:");

			// Current time
			var now = DateTime.Now;
			sb.AppendLine($"Current Date and Time: {now}");
			sb.AppendLine($"Day of Week: {now.DayOfWeek}");
			sb.AppendLine($"Time of Day: {GetTimeOfDayDescription(now)}");

			// Location
			if (IsLocationEnabled)
			{
				sb.AppendLine($"Current Location: {CurrentLocation}");

				// Update the location in the location service if it's not already set
				// This ensures that even if GetCurrentLocationAsync hasn't been called yet,
				// we still set the location for the location service
				if (CurrentLocation != "Location not available" &&
					!CurrentLocation.StartsWith("Error:") &&
					CurrentLocation.Contains(","))
				{
					try
					{
						// Parse the location string "Lat: 12.3456, Long: 78.9012"
						var parts = CurrentLocation.Split(',');
						if (parts.Length == 2)
						{
							double lat = double.Parse(parts[0].Trim().Replace("Lat:", "").Trim());
							double lng = double.Parse(parts[1].Trim().Replace("Long:", "").Trim());
							_locationTools.SetCurrentLocation(lat, lng);
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to parse location string: {Location}", CurrentLocation);
					}
				}
			}

			// Calendar events
			sb.AppendLine($"Calendar Events for Today ({events.Count}):");
			if (events.Any())
			{
				foreach (var evt in events.OrderBy(e => e.StartDate))
				{
					sb.AppendLine($"- {evt.Title} ({evt.StartDate:t} - {evt.EndDate:t})");
				}
			}
			else
			{
				sb.AppendLine("- No calendar events for today");
			}

			// About me
			if (!string.IsNullOrWhiteSpace(AboutMeText))
			{
				sb.AppendLine("\nABOUT ME:");
				sb.AppendLine(AboutMeText);
			}
			// Add only incomplete tasks - no need to process completed ones!
			sb.AppendLine("\nACTIVE TASKS:");
			foreach (var task in Tasks.Where(t => !t.IsCompleted))
			{
				var projectName = "";
				var project = Projects.FirstOrDefault(p => p.ID == task.ProjectID);
				if (project != null)
				{
					projectName = project.Name;
				}

				sb.AppendLine($"- Task '{task.Title}', Project: '{projectName}'");
			}
			// Instructions for the AI
			sb.AppendLine("\nINSTRUCTIONS:");
			sb.AppendLine($"Based on all the context above, identify which tasks should be prioritized. Consider:");
			// sb.AppendLine("1. Tasks that are due soon or today");
			sb.AppendLine("- Tasks that relate to upcoming calendar events in the next 24 hours");
			sb.AppendLine("- Tasks that might be relevant to my current location should come first and exclude all other tasks unrelated to the location");
			sb.AppendLine("- Tasks that align with my personal preferences in the 'About Me' section, unless they don't meet the location or timeframe criteria");
			sb.AppendLine("- ONLY recommend tasks appropriate for this time of day - e.g. don't suggest evening activities in the morning");

			// Add instructions about using the IsNearby function
			if (IsLocationEnabled)
			{
				sb.AppendLine("\nYou have access to a special function called IsNearby that can tell you if the user is near a specific type of location:");
				sb.AppendLine("- Use the IsNearby function to check if the user is near places mentioned in tasks");
				sb.AppendLine("- Example: To check if the user is near a coffee shop, call IsNearby(\"coffee shop\")");
				sb.AppendLine("- Example points of interest to check: coffee shops, grocery stores, retail stores, restaurants, banks, etc.");
				sb.AppendLine("- Extract the relevant location type from task titles or descriptions");
				sb.AppendLine("- The function returns true if the user is within 100 meters of the type of location");
				sb.AppendLine("- Prioritize tasks related to locations that IsNearby returns true for");
				sb.AppendLine("- Mention in the priorityReasoning when a task is prioritized because the user is near a relevant location");
			}

			sb.AppendLine("\n- For each task you prioritize, provide a brief reason WHY it's being prioritized now");
			sb.AppendLine("- Don't include more than 3 tasks in the response");
			sb.AppendLine("- Provide a personalized greeting using my name if available in the 'About Me' section, or a fun, space/cosmic themed greeting");
			sb.AppendLine("- Do not guess at tasks. It's ok to not include any prioritized tasks if none are relevant");
			sb.AppendLine("- When choosing between tasks, order them by best fit. Do not choose randomly");

			_logger.LogInformation($"AI Context: {sb.ToString()}");

			AnalysisStatusDetail = "Applying futuristic intelligence to your tasks...";
			// Send to AI for analysis with MCP tools included
			var chatClient = _chatClientService.GetClient();
			if (chatClient != null)
			{
				try
				{
					var apiResponse = await chatClient.GetResponseAsync<PriorityTaskResult>(sb.ToString());
					if (apiResponse?.Result != null)
					{
						// Update personalized greeting if it exists
						if (!string.IsNullOrEmpty(apiResponse.Result.PersonalizedGreeting))
						{
							PersonalizedGreeting = apiResponse.Result.PersonalizedGreeting;
						}
						else
						{
							// Generate a default time-based greeting
							var timeOfDay = GetTimeOfDayDescription(DateTime.Now);
							PersonalizedGreeting = $"Good {timeOfDay}, Adventurer!";
						}

						if (apiResponse.Result.PriorityTasks != null)
						{

							PriorityTasks.Clear();
							foreach (var aiTask in apiResponse.Result.PriorityTasks)
							{
								var task = Tasks.FirstOrDefault(t => t.Title == aiTask.Title && !t.IsCompleted);
								if (task != null)
								{
									task.PriorityReasoning = aiTask.PriorityReasoning;
									task.AssistType = aiTask.AssistType;									task.AssistData = aiTask.AssistData;
									_logger.LogDebug("Task '{TaskTitle}' prioritized because: {PriorityReasoning}, AssistType: {AssistType}, AssistData: {AssistData}", 
										task.Title, aiTask.PriorityReasoning, aiTask.AssistType, aiTask.AssistData);

									var taskViewModel = new ProjectTaskViewModel(task);
									var project = Projects.FirstOrDefault(p => p.ID == task.ProjectID);
									if (project != null)
									{
										taskViewModel.ProjectName = project.Name;
									}
									PriorityTasks.Add(taskViewModel);
								}
							}
						}
						else
						{
							AnalysisStatusTitle = "All clear!";
							AnalysisStatusDetail = "Your task universe is clear of immediate threats.";
						}
					}

					HasPriorityTasks = PriorityTasks.Count > 0;

					// Record when we last checked priorities
					_lastPriorityCheck = DateTime.Now;
				}

				catch (Exception ex)
				{
					_logger.LogError($"Error calling AI for task prioritization: {ex.Message}");
				}
			}
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
		finally
		{
			await Task.Delay(1000); // Give the user a moment to read the status
			IsAnalyzingContext = false;
		}
	}

	private string GetTimeOfDayDescription(DateTime time)
	{
		var hour = time.Hour;
		if (hour >= 5 && hour < 12)
			return "Morning";
		else if (hour >= 12 && hour < 17)
			return "Afternoon";
		else if (hour >= 17 && hour < 21)
			return "Evening";
		else
			return "Night";
	}

	[RelayCommand]
	private async Task VoiceRecord()
	{
		await AppShell.Current.GoToAsync("voice");
	}

	[RelayCommand]
	async Task PickPhotoAsync()
	{
		if (IsBusy)
			return;

		try
		{
			IsBusy = true;

			var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
			{
				Title = "Select a photo"
			});

			if (result != null)
			{
				// Navigate to the PhotoPage with the image
				var parameters = new Dictionary<string, object>
				{
					{ "ImageSource", result.FullPath }
				};
				await Shell.Current.GoToAsync("photo", parameters);
			}
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	async Task TakePhotoAsync()
	{
		if (IsBusy)
			return;

		try
		{
			IsBusy = true;
			// Navigate directly to PhotoPage; photo capture will occur on appearing
			await Shell.Current.GoToAsync("photo");
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	async Task CheckIsNearby(ProjectTaskViewModel task)
	{
		if (task == null)// || string.IsNullOrWhiteSpace(task.Task.AssistData)
			return;

		
		var chatClient = _chatClientService.GetClient();
		if (chatClient != null)
		{
			var sb = new System.Text.StringBuilder();

			await GetCurrentLocationAsync(); // set the _locationTools

			// Add basic context information
			sb.AppendLine("List any specific location that is nearby that could help me with this task.");
			sb.AppendLine($"Task: {task.Title}");
			sb.AppendLine($"Details: {task.Task.AssistData}");
			sb.AppendLine($"Current location: {_locationTools.GetCurrentLocation().Latitude}, {_locationTools.GetCurrentLocation().Longitude}");
			sb.AppendLine($"IsNearby a coffee shop");			_logger.LogDebug("AI Task Analysis Prompt: {Prompt}", sb.ToString());			

			try
			{
				// var options = new ChatOptions
				// {
				// 	Tools = [AIFunctionFactory.Create(_locationTools.IsNearby)]
				// };
				var apiResponse = await chatClient.GetResponseAsync<string>(sb.ToString());
				if (apiResponse?.Result != null)
				{
					await AppShell.Current.DisplayAlert("Nearby Location", apiResponse.Result, "OK");
				}
			}catch (Exception ex)
			{
				_logger.LogError($"Error calling AI for task prioritization: {ex.Message}");
			}
		}
	}
}