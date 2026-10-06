using Microsoft.EntityFrameworkCore;
using OneExchangeOrderService.Domain.Entities.Common;
using OneExchangeOrderService.Domain.Persistence.Repositories.Common;
using OneExchangeOrderService.Infrastructure.DbContexts;
using System.Linq.Expressions;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public abstract class BaseExchangeOrderRepository<T>(ExchangeOrderDbContext context) : IBaseRepository<T> where T : BaseEntity
    {
#pragma warning disable IDE1006 // Naming Styles
        protected ExchangeOrderDbContext Context { get; } = context;

#pragma warning restore IDE1006 // Naming Styles
        public async Task Add(T entity) => await Context.Set<T>().AddAsync(entity);
        public async Task AddRange(IEnumerable<T> entities) => await Context.Set<T>().AddRangeAsync(entities);
        public async Task<IEnumerable<T>> Find(Expression<Func<T, bool>> expression) => await Context.Set<T>().Where(expression).ToListAsync();
        public async Task<IEnumerable<T>> GetAll() => await Context.Set<T>().ToListAsync();
        public async Task<T?> GetById(Guid id)
        {
            var entity = await Context.Set<T>()
                .TagWith(nameof(GetById))
                .SingleOrDefaultAsync(q => q.Id == id);

            return entity ?? throw new KeyNotFoundException($"{typeof(T).Name} with id '{id}' was not found.");
        }
        public void Update(T entity) => Context.Set<T>().Update(entity);
        public void Attach(T entity) => Context.Set<T>().Attach(entity);
        public void Remove(T entity) => Context.Set<T>().Remove(entity);
        public void RemoveRange(IEnumerable<T> entities) => Context.Set<T>().RemoveRange(entities);
    }
}
