using OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation;

namespace OneExchangeOrderService.Application.Interfaces
{
    public interface IOrderReservationEventPublisher
    {
        public Task PublishOrderAsync(OrderRequestedEvent evt);
    }
}
