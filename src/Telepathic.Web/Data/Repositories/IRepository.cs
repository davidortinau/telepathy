using System.Linq.Expressions;

namespace Telepathic.Web.Data.Repositories;

public interface IRepository<T> where T : class
{
    IEnumerable<T> GetAll();
    Task<IEnumerable<T>> GetAllAsync();
    T? GetById(int id);
    Task<T?> GetByIdAsync(int id);
    IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    void Add(T entity);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    void DeleteById(int id);
    Task DeleteByIdAsync(int id);
    int SaveChanges();
    Task<int> SaveChangesAsync();
}