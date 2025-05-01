using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Telepathic.Web.Models;

namespace Telepathic.Web.Data.Repositories;

public class TeamMemberRepository : IRepository<TeamMember>
{
    private readonly TelepathicDbContext _context;

    public TeamMemberRepository(TelepathicDbContext context)
    {
        _context = context;
    }

    public IEnumerable<TeamMember> GetAll()
    {
        return _context.TeamMembers
            .Include(t => t.Projects)
            .ThenInclude(p => p.ProjectTasks)
            .ToList();
    }

    public async Task<IEnumerable<TeamMember>> GetAllAsync()
    {
        return await _context.TeamMembers
          .Include(t => t.Projects)
          .ThenInclude(p => p.ProjectTasks)
          .ToListAsync();
        //return await _context.TeamMembers            
        //    .Include(t => t.Projects)
        //        .ThenInclude(p => p.Category)
        //    .Include(t => t.Projects)
        //        .ThenInclude(p => p.ProjectTasks)
        //    .ToListAsync();
    }

    public TeamMember? GetById(int id)
    {
        return _context.TeamMembers
            .Include(t => t.Projects)
            .ThenInclude(t => t.ProjectTasks)
            .FirstOrDefault(t => t.ID == id);
    }

    public async Task<TeamMember?> GetByIdAsync(int id)
    {
        return await _context.TeamMembers
            .Include(t => t.Projects)
            .FirstOrDefaultAsync(t => t.ID == id);
    }

    public IEnumerable<TeamMember> Find(Expression<Func<TeamMember, bool>> predicate)
    {
        return _context.TeamMembers
            .Include(t => t.Projects)
            .Where(predicate)
            .ToList();
    }

    public async Task<IEnumerable<TeamMember>> FindAsync(Expression<Func<TeamMember, bool>> predicate)
    {
        return await _context.TeamMembers
            .Include(t => t.Projects)
            .Where(predicate)
            .ToListAsync();
    }

    public void Add(TeamMember entity)
    {
        _context.TeamMembers.Add(entity);
    }

    public async Task AddAsync(TeamMember entity)
    {
        await _context.TeamMembers.AddAsync(entity);
    }

    public void Update(TeamMember entity)
    {
        _context.TeamMembers.Update(entity);
    }

    public void Delete(TeamMember entity)
    {
        _context.TeamMembers.Remove(entity);
    }

    public void DeleteById(int id)
    {
        var teamMember = _context.TeamMembers.Find(id);
        if (teamMember != null)
        {
            _context.TeamMembers.Remove(teamMember);
        }
    }

    public async Task DeleteByIdAsync(int id)
    {
        var teamMember = await _context.TeamMembers.FindAsync(id);
        if (teamMember != null)
        {
            _context.TeamMembers.Remove(teamMember);
        }
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}