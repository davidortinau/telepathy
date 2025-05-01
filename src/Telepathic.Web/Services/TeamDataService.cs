using Telepathic.Shared.Services;
using Telepathic.Shared.Models;
using Telepathic.Web.Models;
using Telepathic.Web.Data.Repositories;
using Microsoft.VisualBasic;

namespace Telepathic.Web.Services;

public class TeamDataService : ITeamDataService
{

    private readonly IRepository<TeamMember> _teamMemberRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<ProjectTask> _projectTaskRepository;
    private readonly IRepository<Category> _categoryRepository;

    public TeamDataService(
        IRepository<TeamMember> teamMemberRepository,
        IRepository<Project> projectRepository,
        IRepository<ProjectTask> projectTaskRepository, 
        IRepository<Category> categoryRepository)
    {
        _teamMemberRepository = teamMemberRepository;
        _projectRepository = projectRepository;
        _projectTaskRepository = projectTaskRepository; 
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<TeamTaskLoad>> GetTeamTaskLoadAsync()
    {
        var team = await _teamMemberRepository.GetAllAsync();
        var teamTaskLoad = new List<TeamTaskLoad>();

        foreach (var member in team)
        {
            var count = 0;
            foreach (var project in member.Projects)
            {
                //count += project.ProjectTasks.Count(t => !t.IsCompleted);
                count += project.ProjectTasks.Count();
            }
            teamTaskLoad.Add(new TeamTaskLoad
            {
                Name = member.Name,
                TaskCount = count
            });
        }
        return teamTaskLoad;
    }

    public async Task<IEnumerable<CategoryTaskLoad>> GetCategoryTaskLoadAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        var categoryTasks = new List<CategoryTaskLoad>();

        foreach (var category in categories)
        {
            var count = 0;
            var projects = await _projectRepository.FindAsync(c=> c.CategoryID == category.ID);
            
            foreach (var project in projects)
            {
                //count += project.ProjectTasks.Count(t => !t.IsCompleted);
                count += project.ProjectTasks.Count();
            }
            categoryTasks.Add(new CategoryTaskLoad
            {
                Title = category.Title,
                Color = category.Color,
                TaskCount = count
            });
        }
        return categoryTasks;
    }

    public async Task<IEnumerable<TasksDue>> GetTasksDueAsync()
    {
        var tasks = await _projectTaskRepository.GetAllAsync();
        var tasksDue = new List<TasksDue>();
        var completedTasks = tasks.Where(t => t.IsCompleted).ToList();
        var orderedTasks = tasks.OrderBy(t => t.DueDate).ToList();

        var uniqueDates = tasks.Select(t => t.DueDate?.Date).OrderBy(t => t).Distinct().ToList();

        foreach (var date in uniqueDates)
        {
            var tasksOnDate = orderedTasks.Where(t => t.DueDate?.Date == date).ToList();
            if (tasksOnDate.Any())
            {             
                tasksDue.Add(new TasksDue
                {
                    DueDate = date,
                    CompletedCount = completedTasks.Count(d => d.DueDate?.Date == date?.Date),
                    TaskCount = tasks.Count(t => t.DueDate?.Date == date?.Date),
                });
            }
        }
        return tasksDue;
    }
}   
