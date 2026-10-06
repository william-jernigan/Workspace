using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request
{
    [ExcludeFromCodeCoverage]
    public class ReservationConnectorRequestData
    {
        public SystemMeta SystemMeta { get; set; }
        public ExternalReference ExternalReference { get; set; }
        public List<BookingItem> BookingItems { get; set; }
    }
}
