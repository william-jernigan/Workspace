using OneExchangeOrderService.Domain.Entities.Common;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Models.Eventhub.BookingAlternateEmail;
using OneExchangeOrderService.Domain.Persistence.Repositories.Common;

namespace OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder
{
    public interface IOrderRepository : IBaseRepository<BaseEntity>
    {
        public Task<string> CreateOrderAsync(Order order);
        public Task<IEnumerable<OrderResponse>> GetOrdersAsync(OrderRequest request, List<string> orderStatus);
        public Task<Order> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<(string? OrderStatus, string? ExternalReferenceId, string? orderId, string? StatusReason)?> GetOrderStatusAsync(string orderNumber);
        public Task<Order?> GetOrderByOrderNumberAsync(string orderNumber);
        public Task<bool> HasExceededWaiverLimitForCurrentYearAsync(string customerExternalReferenceId, string fromDate);
        public Task UpdateOrderAsync(Order order);
        public Task<string> UpdateOrderStatusAsync(Order order);
        public Task<IEnumerable<OrderResponse>> GetOrdersFreeTextSearchAsync(OrderRequest request);
        Task<OrderDetailsResponse?> GetOrderDetailsByConfirmationNumberAsync(string orderId);
        public Task<Order?> GetOrderByExternalReferenceIdAsync(string externalReferenceId, CancellationToken cancellationToken = default);
        public Task<(IEnumerable<Order> Orders, int TotalCount)> SearchOrdersAsync(
        string? status,
        string? reservationId,
        List<Guid>? profileIds,
        DateTime? reservationDate,
        int? reservationDateInterval,
        DateTime? arrivalDate,
        int? arrivalDateInterval,
        int page,
        int pageSize,
        string? orderByField,
        string? orderByDirection,
        CancellationToken ct);
    }
}
