using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request
{
    [ExcludeFromCodeCoverage]
    public class GuestInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
