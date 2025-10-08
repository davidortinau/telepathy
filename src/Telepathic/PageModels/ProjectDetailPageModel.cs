using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Telepathic.Models;
using Telepathic.Services;
using System.Collections.ObjectModel;
using Microsoft.Extensions.AI;
using Microsoft.Maui.ApplicationModel; // PhoneDialer, Email, Launcher
using Microsoft.Maui.ApplicationModel.Communication; // PhoneDialer, EmailMessage, Email
using Microsoft.Maui.Controls; // Shell
using Plugin.Maui.CalendarStore; // Calendar support

namespace Telepathic.PageModels;

public partial class ProjectDetailPageModel : ObservableObject, IQueryAttributable, IProjectTaskPageModel
{
	private Project? _project;
	private readonly ProjectRepository _projectRepository;
	private readonly TaskRepository _taskRepository;
	private readonly CategoryRepository _categoryRepository;
	private readonly TagRepository _tagRepository;
	private readonly ModalErrorHandler _errorHandler;
	private readonly IChatClientService _chatClientService;
	private readonly ICalendarStore _calendarStore;
	private readonly TaskAssistHandler _taskAssistHandler;

	// Interface implementations
	IAsyncRelayCommand<ProjectTask> IProjectTaskPageModel.NavigateToTaskCommand => NavigateToTaskCommand;
	IAsyncRelayCommand<ProjectTask> IProjectTaskPageModel.AcceptRecommendationCommand => AcceptRecommendationCommand;
	IAsyncRelayCommand<ProjectTask> IProjectTaskPageModel.RejectRecommendationCommand => RejectRecommendationCommand;
	IAsyncRelayCommand<ProjectTask> IProjectTaskPageModel.AssistCommand => AssistCommand;
	bool IProjectTaskPageModel.IsBusy => IsBusy;

	[ObservableProperty]
	private bool _hasRecommendations;

	[ObservableProperty]
	private string _name = string.Empty;

	[ObservableProperty]
	private string _description = string.Empty;

	[ObservableProperty]
	private List<ProjectTask> _tasks = [];

	[ObservableProperty]
	private List<ProjectTask> _recommendedTasks = [];

	[ObservableProperty]
	private List<Category> _categories = [];

	[ObservableProperty]
	private Category? _category;

	[ObservableProperty]
	private int _categoryIndex = -1;

	[ObservableProperty]
	private List<Tag> _allTags = [];

	[ObservableProperty]
	private string _icon = FluentUI.ribbon_24_regular;

	[ObservableProperty]
	bool _isBusy;

	[ObservableProperty]
	string _busyTitle = "Loading...";

	[ObservableProperty]
	string _busyDetails = "Please wait.";

	[ObservableProperty]
	bool _isTelepathyEnabled = Preferences.Default.Get("telepathy_enabled", false);

	[ObservableProperty]
	private List<string> _icons =
	[
		FluentUI.ribbon_24_regular,
		FluentUI.ribbon_star_24_regular,
		FluentUI.trophy_24_regular,
		FluentUI.badge_24_regular,
		FluentUI.book_24_regular,
		FluentUI.people_24_regular,
		FluentUI.bot_24_regular
	];

	public bool IsNewProject => _project?.IsNullOrNew() ?? true;

	public bool HasCompletedTasks
		=> _project?.Tasks.Any(t => t.IsCompleted) ?? false;

	public bool HasRecommendedTasks
	 	=> RecommendedTasks?.Count > 0;
	public ProjectDetailPageModel(ProjectRepository projectRepository, TaskRepository taskRepository, CategoryRepository categoryRepository, TagRepository tagRepository, ModalErrorHandler errorHandler, IChatClientService chatClientService, ICalendarStore calendarStore, TaskAssistHandler taskAssistHandler)
	{
		_projectRepository = projectRepository;
		_taskRepository = taskRepository;
		_categoryRepository = categoryRepository;
		_tagRepository = tagRepository;
		_errorHandler = errorHandler;
		_chatClientService = chatClientService;
		_calendarStore = calendarStore;
		_taskAssistHandler = taskAssistHandler;
	}

	partial void OnNameChanged(string value)
	{
		// No longer triggers recommendations here
	}

	[RelayCommand]
	private void NameUnfocused()
	{
		// Only trigger if new project and no tasks
		if (_project != null && _project.IsNullOrNew() && !string.IsNullOrWhiteSpace(Name) && (Tasks == null || Tasks.Count == 0))
		{
			_ = GetRecommendationsAsync(Name,Description);
		}
	}

	private async Task GetRecommendationsAsync(string projectName, string projectDescription)
	{
		try
		{
			var categoryTitles = Categories?.Select(c => c.Title).ToList() ?? new List<string>();

			IsBusy = true;
			BusyTitle = "Getting task recommendations.";
			BusyDetails = $"Given a project named '{projectName}', and these categories: {string.Join(", ", categoryTitles)}, looking up required documents.";
			
			string _aboutMeText = Preferences.Default.Get("about_me_text", string.Empty);
            var prompt = $"Given a project named '{projectName}'," +
                $" and these categories: {string.Join(", ", categoryTitles)}," +
                $" pick the best matching set of documents for the architectural process of a solution and suggest 3-7 documents for this project." +
                $" Use these details about the '{projectDescription}' so the writing sounds like profesional architect ITIL 4 certified" +
                $" Take into consideration the types of documents every architect needs to know.";// Respond as JSON: {{\"category\":\"category name\",\"tasks\":[\"task1\",\"task2\"]}}

			await Task.Delay(2000);

			var chatClient = _chatClientService.GetClient();
			var response = await chatClient.GetResponseAsync<RecommendationResponse>(prompt);

			BusyDetails = "Processing the recommendations.";
			BusyDetails = $"We have {response?.Result.Tasks.Count} documents to recommend that we think could be amazing.";

			await Task.Delay(2000);

			if (response?.Result != null)
			{
				var rec = response.Result;
				var bestCategory = Categories?.FirstOrDefault(c => c.Title.Equals(rec.Category, StringComparison.OrdinalIgnoreCase));
				if (bestCategory != null)
				{
					Category = bestCategory;
					CategoryIndex = Categories?.IndexOf(bestCategory) ?? -1;
				}
				var recommendedTasks = new List<ProjectTask>();
				foreach (var t in rec.Tasks)
				{
					recommendedTasks.Add(new ProjectTask { Title = t, IsRecommendation = true });
				}
				RecommendedTasks = recommendedTasks;
				HasRecommendations = RecommendedTasks.Count > 0;
			}
		}		catch (InvalidOperationException ex)
		{
			_errorHandler.HandleError(new Exception("Chat client is not initialized. Please add your OpenAI API key in settings.", ex));
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
	private Task AcceptRecommendation(ProjectTask task)
	{
		if (!_project.IsNullOrNew() && task != null)
		{
			_project.Tasks.Add(task);
			Tasks = new List<ProjectTask>(_project.Tasks);

			// Remove from recommendations since we've added it directly to tasks
			var updatedRecommendations = RecommendedTasks.ToList();
			updatedRecommendations.Remove(task);
			RecommendedTasks = updatedRecommendations;
			HasRecommendations = RecommendedTasks.Count > 0;
		}
		return Task.CompletedTask;
	}

	[RelayCommand]
	private Task RejectRecommendation(ProjectTask task)
	{
		if (task != null && RecommendedTasks.Contains(task))
		{
			var updatedTasks = RecommendedTasks.ToList();
			updatedTasks.Remove(task);
			RecommendedTasks = updatedTasks;
			HasRecommendations = RecommendedTasks.Count > 0;
		}
		
		return Task.CompletedTask;
	}

	[RelayCommand]
	void AcceptAllRecommendations()
	{
		if (!_project.IsNullOrNew() && RecommendedTasks.Count > 0)
		{
			foreach (var task in RecommendedTasks.ToList())
			{
				_project.Tasks.Add(task);
			}
			Tasks = new List<ProjectTask>(_project.Tasks);
			RecommendedTasks = new List<ProjectTask>();
			HasRecommendations = false;
		}
	}

	private class RecommendationResponse
	{
		public string Category { get; set; } = string.Empty;
		public List<string> Tasks { get; set; } = new();
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.ContainsKey("id"))
		{
			int id = Convert.ToInt32(query["id"]);
			LoadData(id).FireAndForgetSafeAsync(_errorHandler);
		}
		else if (query.ContainsKey("refresh"))
		{
			RefreshData().FireAndForgetSafeAsync(_errorHandler);
		}
		else
		{
			Task.WhenAll(LoadCategories(), LoadTags()).FireAndForgetSafeAsync(_errorHandler);
			_project = new();
			_project.Tags = [];
			_project.Tasks = [];
			Tasks = _project.Tasks;
		}
	}

	private async Task LoadCategories() =>
		Categories = await _categoryRepository.ListAsync();

	private async Task LoadTags() =>
		AllTags = await _tagRepository.ListAsync();

	private async Task RefreshData()
	{
		if (_project.IsNullOrNew())
		{
			if (_project is not null)
				Tasks = new(_project.Tasks);

			return;
		}

		Tasks = await _taskRepository.ListAsync(_project.ID);
		_project.Tasks = Tasks;
	}

	private async Task LoadData(int id)
	{
		try
		{
			IsBusy = true;

			_project = await _projectRepository.GetAsync(id);

			if (_project.IsNullOrNew())
			{
				_errorHandler.HandleError(new Exception($"Project with id {id} could not be found."));
				return;
			}

			Name = _project.Name;
			Description = _project.Description;
			Tasks = _project.Tasks;

			Icon = _project.Icon;

			Categories = await _categoryRepository.ListAsync();
			Category = Categories?.FirstOrDefault(c => c.ID == _project.CategoryID);
			CategoryIndex = Categories?.FindIndex(c => c.ID == _project.CategoryID) ?? -1;

			var allTags = await _tagRepository.ListAsync();
			foreach (var tag in allTags)
			{
				tag.IsSelected = _project.Tags.Any(t => t.ID == tag.ID);
			}
			AllTags = new(allTags);
		}
		catch (Exception e)
		{
			_errorHandler.HandleError(e);
		}
		finally
		{
			IsBusy = false;
			OnPropertyChanged(nameof(HasCompletedTasks));
			OnPropertyChanged(nameof(IsNewProject));
		}
	}

	[RelayCommand]
	private async Task TaskCompleted(ProjectTask task)
	{
		await _taskRepository.SaveItemAsync(task);
		OnPropertyChanged(nameof(HasCompletedTasks));
	}


	[RelayCommand]
	private async Task Save()
	{
		if (_project is null)
		{
			_errorHandler.HandleError(
				new Exception("Project is null. Cannot Save."));

			return;
		}

		_project.Name = Name;
		_project.Description = Description;
		_project.CategoryID = Category?.ID ?? 0;
		_project.Icon = Icon ?? FluentUI.ribbon_24_regular;
		await _projectRepository.SaveItemAsync(_project);

		if (_project.IsNullOrNew())
		{
			foreach (var tag in AllTags)
			{
				if (tag.IsSelected)
				{
					await _tagRepository.SaveItemAsync(tag, _project.ID);
				}
			}
		}
		foreach (var task in _project.Tasks)
		{
			if (task.ID == 0)
			{
				task.ProjectID = _project.ID;
				await _taskRepository.SaveItemAsync(task);
			}
		}		// Save any remaining recommended tasks
		foreach (var recommendedTask in RecommendedTasks.Where(t => t.IsRecommendation))
		{
			// Once we save it as part of the project, it's no longer just a recommendation
			recommendedTask.IsRecommendation = false;
			recommendedTask.ProjectID = _project.ID;
			_project.Tasks.Add(recommendedTask);
			await _taskRepository.SaveItemAsync(recommendedTask);
		}

		// Clear recommendations since they've all been saved
		RecommendedTasks = new List<ProjectTask>();
		
		await Shell.Current.GoToAsync("..");
		await AppShell.DisplayToastAsync("Project saved");
	}

	[RelayCommand]
	private async Task AddTask()
	{
		if (_project is null)
		{
			_errorHandler.HandleError(
				new Exception("Project is null. Cannot navigate to task."));

			return;
		}

		// Pass the project so if this is a new project we can just add
		// the tasks to the project and then save them all from here.
		await Shell.Current.GoToAsync($"task",
			new ShellNavigationQueryParameters(){
				{TaskDetailPageModel.ProjectQueryKey, _project}
			});
	}

	[RelayCommand]
	private async Task Delete()
	{
		if (_project.IsNullOrNew())
		{
			await Shell.Current.GoToAsync("..");
			return;
		}

		await _projectRepository.DeleteItemAsync(_project);
		await Shell.Current.GoToAsync("..");
		await AppShell.DisplayToastAsync("Project deleted");
	}
	[RelayCommand]
	private Task NavigateToTask(ProjectTask task) =>
		Shell.Current.GoToAsync($"task?id={task.ID}");
	[RelayCommand]
	private async Task Assist(ProjectTask task)
	{
		if (task == null || task.AssistType == AssistType.None)
			return;
		try
		{
			IsBusy = true;
			BusyTitle = "Performing assist action";
			
			// Use the existing TaskAssistHandler service to handle all assist action types
			await _taskAssistHandler.HandleAssistAsync(task, false);
		}
		catch (Exception ex)
		{
			_errorHandler.HandleError(ex);
			await AppShell.DisplayToastAsync("Error performing assist action. See logs for details.");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task ToggleTag(Tag tag)
	{
		tag.IsSelected = !tag.IsSelected;

		if (!_project.IsNullOrNew())
		{
			if (tag.IsSelected)
			{
				await _tagRepository.SaveItemAsync(tag, _project.ID);
			}
			else
			{
				await _tagRepository.DeleteItemAsync(tag, _project.ID);
			}
		}

		AllTags = new(AllTags);
	}

	[RelayCommand]
	private async Task CleanTasks()
	{
		var completedTasks = Tasks.Where(t => t.IsCompleted).ToArray();
		foreach (var task in completedTasks)
		{
			await _taskRepository.DeleteItemAsync(task);
			Tasks.Remove(task);
		}

		Tasks = new(Tasks);
		OnPropertyChanged(nameof(HasCompletedTasks));
		await AppShell.DisplayToastAsync("All cleaned up!");
	}
}
