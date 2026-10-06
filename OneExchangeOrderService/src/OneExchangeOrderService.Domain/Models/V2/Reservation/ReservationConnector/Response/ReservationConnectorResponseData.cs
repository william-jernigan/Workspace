using OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request;
using OneExchangeOrderService.Domain.Models.V2.Reservation.Response;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Response
{
    [ExcludeFromCodeCoverage]
    public class ReservationConnectorResponseData
    {
        public string BookingStatus { get; set; }
        public string BookingId { get; set; }
        public ExternalReference ExternalReference { get; set; } = new();
        public List<BookingItemSummary> BookingItems { get; set; }
        public GuaranteeAndCancelPolicy GuaranteeAndCancelPolicy { get; set; }
        public ErrorInfo? Error { get; set; }
    }
}
