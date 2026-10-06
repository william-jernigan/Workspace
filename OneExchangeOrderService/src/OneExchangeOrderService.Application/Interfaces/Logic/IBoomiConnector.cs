using OneExchangeOrderService.Domain.Models.V2.Cancellation.Request;
using OneExchangeOrderService.Domain.Models.V2.Cancellation.Response;
using OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request;
using OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Response;

namespace OneExchangeOrderService.Application.Interfaces.Logic
{
    public interface IBoomiConnector
    {
        public Task<ReservationConnectorResponse> ReservationConnectorCreateAsync(ReservationConnectorRequest request);
        public Task<CancellationResponse> CancellationConnectorCancelAsync(CancellationRequest request);
    }
}
