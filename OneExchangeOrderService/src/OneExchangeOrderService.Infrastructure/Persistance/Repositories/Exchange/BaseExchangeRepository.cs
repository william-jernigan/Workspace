using Microsoft.EntityFrameworkCore;
using OneExchangeOrderService.Domain.Entities.Common;
using OneExchangeOrderService.Domain.Persistence.Repositories.Common;
using OneExchangeOrderService.Infrastructure.DbContexts;
using System.Linq.Expressions;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.Exchange
{
    public abstract class BaseExchangeRepository<T>(ExchangeDbContext context) : IBaseRepository<T> where T : BaseEntity
    {
#pragma warning disable IDE1006 // Naming Styles
        protected ExchangeDbContext Context { get; } = context;

#pragma warning restore IDE1006 // Naming Styles
        public async Task Add(T entity) => await Context.Set<T>().AddAsync(entity);
        public async Task AddRange(IEnumerable<T> entities) => await Context.Set<T>().AddRangeAsync(entities);
        public async Task<IEnumerable<T>> Find(Expression<Func<T, bool>> expression) => await Context.Set<T>().Where(expression).ToListAsync();
        public async Task<IEnumerable<T>> GetAll() => await Context.Set<T>().ToListAsync();
        public void Update(T entity) => Context.Set<T>().Update(entity);
        public void Attach(T entity) => Context.Set<T>().Attach(entity);
        public void Remove(T entity) => Context.Set<T>().Remove(entity);
        public void RemoveRange(IEnumerable<T> entities) => Context.Set<T>().RemoveRange(entities);
        public Task<T> GetById(Guid id) => throw new NotImplementedException();
        public Task<T> GetByClustId(long id) => throw new NotImplementedException();
    }
}
