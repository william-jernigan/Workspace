using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request
{
    [ExcludeFromCodeCoverage]
    public class GuestCounts
    {
        public int? Adults { get; set; }
        public int? Children { get; set; }
    }
}
