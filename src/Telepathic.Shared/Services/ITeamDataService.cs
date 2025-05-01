using Telepathic.Shared.Models; 

namespace Telepathic.Shared.Services;

public interface ITeamDataService
{
    Task<TeamMember> GetTeamMemberByIdAsync(int id);
    Task<IEnumerable<TeamMember>> GetAllTeamMembersAsync();
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<IEnumerable<Project>> GetAllProjectsAsync();
    Task<IEnumerable<Project>> GetProjectsByTeamMemberIdAsync(int teamMemberId);
    Task<IEnumerable<Project>> GetProjectsByCategoryIdAsync(int categoryId);
    
}
