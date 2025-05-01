using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telepathic.Shared.Models;
using Telepathic.Shared.Services;

namespace Telepathic.Services
{
    public class TeamDataService : ITeamDataService
    {
        //TODO: Implement the methods to interact with the data source (WebAPIs)
        public Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TeamMember>> GetAllTeamMembersAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Project>> GetProjectsByCategoryIdAsync(int categoryId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Project>> GetProjectsByTeamMemberIdAsync(int teamMemberId)
        {
            throw new NotImplementedException();
        }

        public Task<TeamMember> GetTeamMemberByIdAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
