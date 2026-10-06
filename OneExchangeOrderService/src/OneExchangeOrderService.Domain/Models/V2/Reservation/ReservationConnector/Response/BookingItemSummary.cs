using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Response
{
    [ExcludeFromCodeCoverage]
    public class BookingItemSummary
    {
        public string ItemCode { get; set; }
        public string ParentCode { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}
