using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Telepathic.Shared.Models;

namespace Telepathic.Web.Data.Repositories;

public class CategoryRepository : IRepository<Category>
{
    private readonly TelepathicDbContext _context;

    public CategoryRepository(TelepathicDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Category> GetAll()
    {
        return _context.Categories.ToList();
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _context.Categories.ToListAsync();
    }

    public Category? GetById(int id)
    {
        return _context.Categories.FirstOrDefault(c => c.ID == id);
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.ID == id);
    }

    public IEnumerable<Category> Find(Expression<Func<Category, bool>> predicate)
    {
        return _context.Categories.Where(predicate).ToList();
    }

    public async Task<IEnumerable<Category>> FindAsync(Expression<Func<Category, bool>> predicate)
    {
        return await _context.Categories.Where(predicate).ToListAsync();
    }

    public void Add(Category entity)
    {
        _context.Categories.Add(entity);
    }

    public async Task AddAsync(Category entity)
    {
        await _context.Categories.AddAsync(entity);
    }

    public void Update(Category entity)
    {
        _context.Categories.Update(entity);
    }

    public void Delete(Category entity)
    {
        _context.Categories.Remove(entity);
    }

    public void DeleteById(int id)
    {
        var category = _context.Categories.Find(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
        }
    }

    public async Task DeleteByIdAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
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