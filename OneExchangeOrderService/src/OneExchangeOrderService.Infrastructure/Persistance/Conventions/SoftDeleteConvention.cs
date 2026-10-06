using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace OneExchangeOrderService.Infrastructure.Persistance.Conventions
{
    internal sealed class SoftDeleteConvention : IModelFinalizingConvention
    {
        public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            //Expression<Func<BaseEntity, bool>> filterExpr = bm => bm.Status;
            //foreach (var entityType in modelBuilder.Metadata.GetEntityTypes().Where(x => typeof(BaseEntity).IsAssignableFrom(x.ClrType)))
            //{
            //    var parameter = Expression.Parameter(entityType.ClrType);
            //    var body = ReplacingExpressionVisitor.Replace(filterExpr.Parameters[0], parameter, filterExpr.Body);
            //    var lambdaExpression = Expression.Lambda(body, parameter);

            //    entityType.SetQueryFilter(lambdaExpression);
            //}
        }
    }
}
