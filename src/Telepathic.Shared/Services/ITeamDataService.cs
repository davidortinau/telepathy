using Telepathic.Shared.Models; 

namespace Telepathic.Shared.Services;

public interface ITeamDataService
{

    Task<IEnumerable<TeamTaskLoad>> GetTeamTaskLoadAsync();
    Task<IEnumerable<CategoryTaskLoad>> GetCategoryTaskLoadAsync();    
    Task<IEnumerable<TasksDue>> GetTasksDueAsync();
}
