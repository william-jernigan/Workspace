using System.Linq.Expressions;

namespace OneExchangeOrderService.Domain.Persistence.Repositories.Common
{
    public interface IBaseRepository<T>
    {
        public Task Add(T entity);
        public Task AddRange(IEnumerable<T> entities);
        public Task<IEnumerable<T>> Find(Expression<Func<T, bool>> expression);
        public Task<IEnumerable<T>> GetAll();
        public Task<T?> GetById(Guid id);
        public void Update(T entity);
        public void Attach(T entity);
        public void Remove(T entity);
        public void RemoveRange(IEnumerable<T> entities);
    }
}
