using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Telepathic.Web.Models;

namespace Telepathic.Web.Data.Repositories;

public class ProjectTaskRepository : IRepository<ProjectTask>
{
    private readonly TelepathicDbContext _context;

    public ProjectTaskRepository(TelepathicDbContext context)
    {
        _context = context;
    }

    public IEnumerable<ProjectTask> GetAll()
    {
        return _context.ProjectTasks.ToList();
    }

    public async Task<IEnumerable<ProjectTask>> GetAllAsync()
    {
        return await _context.ProjectTasks.ToListAsync();
    }

    public ProjectTask? GetById(int id)
    {
        return _context.ProjectTasks.FirstOrDefault(t => t.ID == id);
    }

    public async Task<ProjectTask?> GetByIdAsync(int id)
    {
        return await _context.ProjectTasks.FirstOrDefaultAsync(t => t.ID == id);
    }

    public IEnumerable<ProjectTask> Find(Expression<Func<ProjectTask, bool>> predicate)
    {
        return _context.ProjectTasks.Where(predicate).ToList();
    }

    public async Task<IEnumerable<ProjectTask>> FindAsync(Expression<Func<ProjectTask, bool>> predicate)
    {
        return await _context.ProjectTasks.Where(predicate).ToListAsync();
    }

    public void Add(ProjectTask entity)
    {
        _context.ProjectTasks.Add(entity);
    }

    public async Task AddAsync(ProjectTask entity)
    {
        await _context.ProjectTasks.AddAsync(entity);
    }

    public void Update(ProjectTask entity)
    {
        _context.ProjectTasks.Update(entity);
    }

    public void Delete(ProjectTask entity)
    {
        _context.ProjectTasks.Remove(entity);
    }

    public void DeleteById(int id)
    {
        var projectTask = _context.ProjectTasks.Find(id);
        if (projectTask != null)
        {
            _context.ProjectTasks.Remove(projectTask);
        }
    }

    public async Task DeleteByIdAsync(int id)
    {
        var projectTask = await _context.ProjectTasks.FindAsync(id);
        if (projectTask != null)
        {
            _context.ProjectTasks.Remove(projectTask);
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