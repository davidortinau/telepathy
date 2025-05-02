using Telepathic.Shared.Services;
using Telepathic.Shared.Models;
using Telepathic.Web.Models;
using Telepathic.Web.Data.Repositories;

namespace Telepathic.Web.Services;

public class TeamDataService : ITeamDataService
{

    private readonly IRepository<TeamMember> _teamMemberRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<ProjectTask> _projectTaskRepository;
    private readonly IRepository<Category> _categoryRepository;
    private List<TeamTaskLoad> _teamTaskLoad = new();
    private List<CategoryTaskLoad> _categoryTaskLoad = new();
    private List<TasksDue> _tasksDue = new();

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
        if (_teamTaskLoad.Count == 0)
        {
            var team = await _teamMemberRepository.GetAllAsync();

            foreach (var member in team)
            {
                var count = 0;
                foreach (var project in member.Projects)
                {
                    count += project.ProjectTasks.Count();
                }
                _teamTaskLoad.Add(new TeamTaskLoad
                {
                    Name = member.Name,
                    TaskCount = count
                });
            }
        }
        return _teamTaskLoad;
    }

    public async Task<IEnumerable<CategoryTaskLoad>> GetCategoryTaskLoadAsync()
    {       
        if (_categoryTaskLoad.Count == 0)
        {
            var categories = await _categoryRepository.GetAllAsync();
            foreach (var category in categories)
            {
                var count = 0;
                var projects = await _projectRepository.FindAsync(c=> c.CategoryID == category.ID);
            
                foreach (var project in projects)
                {
                    count += project.ProjectTasks.Count();
                }
                _categoryTaskLoad.Add(new CategoryTaskLoad
                {
                    Title = category.Title,
                    Color = category.Color,
                    TaskCount = count
                });
            }
        }
        return _categoryTaskLoad;
    }

    public async Task<IEnumerable<TasksDue>> GetTasksDueAsync()
    {
        if (_tasksDue.Count == 0)
        {
            var tasks = await _projectTaskRepository.GetAllAsync();
            var completedTasks = tasks.Where(t => t.IsCompleted).ToList();
            var orderedTasks = tasks.OrderBy(t => t.DueDate).ToList();

            var uniqueDates = tasks.Select(t => t.DueDate?.Date).OrderBy(t => t).Distinct().ToList();

            foreach (var date in uniqueDates)
            {
                var tasksOnDate = orderedTasks.Where(t => t.DueDate?.Date == date).ToList();
                if (tasksOnDate.Any())
                {             
                    _tasksDue.Add(new TasksDue
                    {
                        DueDate = date,
                        CompletedCount = completedTasks.Count(d => d.DueDate?.Date == date?.Date),
                        TaskCount = tasks.Count(t => t.DueDate?.Date == date?.Date),
                    });
                }
            }
        }
        return _tasksDue;
    }
}   
