using Telepathic.Shared.Services;
using Telepathic.Shared.Models;
using Telepathic.Web.Data.Repositories;

namespace Telepathic.Web.Services;

public class TeamDataService : ITeamDataService
{

    private readonly IRepository<TeamMember> _teamMemberRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Category> _categoryRepository;

    public TeamDataService(
        IRepository<TeamMember> teamMemberRepository,
        IRepository<Project> projectRepository,
        IRepository<Category> categoryRepository)
    {
        _teamMemberRepository = teamMemberRepository;
        _projectRepository = projectRepository;
        _categoryRepository = categoryRepository;
    }


    public async Task<TeamMember> GetTeamMemberByIdAsync(int id)
    {
        return await _teamMemberRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<TeamMember>> GetAllTeamMembersAsync()
    {        
        return await _teamMemberRepository.GetAllAsync();
    }
    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        
        return await _projectRepository.GetAllAsync();
    }
    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        
        return await _categoryRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Project>> GetProjectsByTeamMemberIdAsync(int teamMemberId)
    {        
        return await _projectRepository.FindAsync(p => p.TeamMember != null && p.TeamMember.ID == teamMemberId);
    }

    public async Task<IEnumerable<Project>> GetProjectsByCategoryIdAsync(int categoryId)
    {
        return await _projectRepository.FindAsync(p => p.Category != null && p.Category.ID == categoryId);
    }
}
