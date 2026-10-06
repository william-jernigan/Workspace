using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request
{
    [ExcludeFromCodeCoverage]
    public class AttributePair
    {
        public string Attribute { get; set; }
        public string Value { get; set; }
    }
}
