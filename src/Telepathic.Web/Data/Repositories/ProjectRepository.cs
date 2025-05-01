using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Telepathic.Shared.Models;

namespace Telepathic.Web.Data.Repositories;

public class ProjectRepository : IRepository<Project>
{
    private readonly TelepathicDbContext _context;

    public ProjectRepository(TelepathicDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Project> GetAll()
    {
        return _context.Projects
            .Include(p => p.Category)
            .Include(p => p.ProjectTasks)
            .ToList();
    }

    public async Task<IEnumerable<Project>> GetAllAsync()
    {
        return await _context.Projects
            .Include(p => p.Category)
            .Include(p => p.ProjectTasks)
            .ToListAsync();
    }

    public Project? GetById(int id)
    {
        return _context.Projects
            .Include(p => p.Category)
            .Include(p => p.ProjectTasks)
            .FirstOrDefault(p => p.ID == id);
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        return await _context.Projects
            .Include(p => p.Category)
            .Include(p => p.ProjectTasks)
            .FirstOrDefaultAsync(p => p.ID == id);
    }

    public IEnumerable<Project> Find(Expression<Func<Project, bool>> predicate)
    {
        return _context.Projects
            .Include(p => p.Category)
            .Include(p => p.ProjectTasks)
            .Where(predicate)
            .ToList();
    }

    public async Task<IEnumerable<Project>> FindAsync(Expression<Func<Project, bool>> predicate)
    {
        return await _context.Projects
            .Include(p => p.Category)
            .Include(p => p.ProjectTasks)
            .Where(predicate)
            .ToListAsync();
    }

    public void Add(Project entity)
    {
        _context.Projects.Add(entity);
    }

    public async Task AddAsync(Project entity)
    {
        await _context.Projects.AddAsync(entity);
    }

    public void Update(Project entity)
    {
        _context.Projects.Update(entity);
    }

    public void Delete(Project entity)
    {
        _context.Projects.Remove(entity);
    }

    public void DeleteById(int id)
    {
        var project = _context.Projects.Find(id);
        if (project != null)
        {
            _context.Projects.Remove(project);
        }
    }

    public async Task DeleteByIdAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            _context.Projects.Remove(project);
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