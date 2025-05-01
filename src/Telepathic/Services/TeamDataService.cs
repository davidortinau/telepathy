using System.Net.Http.Json;
using System.Diagnostics;
using Telepathic.Shared.Models;
using Telepathic.Shared.Services;
using Telepathic.Utilities;

namespace Telepathic.Services;

public class TeamDataService : ITeamDataService
{
    public async Task<IEnumerable<TeamTaskLoad>> GetTeamTaskLoadAsync()
    {
        IEnumerable<TeamTaskLoad> teamTaskLoad = Array.Empty<TeamTaskLoad>();
        try
        {
            var httpClient = HttpClientHelper.GetHttpClient();
            var Url = HttpClientHelper.TeamTaskLoadUrl;

            teamTaskLoad = await httpClient.GetFromJsonAsync<IEnumerable<TeamTaskLoad>>(Url) ?? [];
        }
        catch (HttpRequestException httpEx)
        {
            Debug.WriteLine($"HTTP Request error: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred: {ex.Message}");
        }
        return teamTaskLoad;
    }

    public async Task<IEnumerable<CategoryTaskLoad>> GetCategoryTaskLoadAsync()
    {
       IEnumerable<CategoryTaskLoad> categoryTaskLoad = Array.Empty<CategoryTaskLoad>();
        try
        {
            var httpClient = HttpClientHelper.GetHttpClient();
            var Url = HttpClientHelper.CategoryTaskLoadUrl;
            categoryTaskLoad = await httpClient.GetFromJsonAsync<IEnumerable<CategoryTaskLoad>>(Url) ?? [];
        }
        catch (HttpRequestException httpEx)
        {
            Debug.WriteLine($"HTTP Request error: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred: {ex.Message}");
        }
        return categoryTaskLoad;
    }

    public async Task<IEnumerable<TasksDue>> GetTasksDueAsync()
    {
        IEnumerable<TasksDue> tasksDue = Array.Empty<TasksDue>();
        try
        {
            var httpClient = HttpClientHelper.GetHttpClient();
            var Url = HttpClientHelper.TasksDueUrl;
            tasksDue = await httpClient.GetFromJsonAsync<IEnumerable<TasksDue>>(Url) ?? [];
        }
        catch (HttpRequestException httpEx)
        {
            Debug.WriteLine($"HTTP Request error: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred: {ex.Message}");
        }
        return tasksDue;
    }
}
