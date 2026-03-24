using System.Linq.Expressions;
using MyToDo.Api.Entities;

namespace MyToDo.Api.Repositories
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        Task<T?> GetAsync(int id);
        Task<IList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(int id);
    }
}
