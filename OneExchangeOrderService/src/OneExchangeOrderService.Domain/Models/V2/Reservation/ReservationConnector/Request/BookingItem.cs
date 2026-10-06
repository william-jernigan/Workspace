using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request
{
    [ExcludeFromCodeCoverage]
    public class BookingItem
    {
        public string ItemCode { get; set; }
        public string ItemClass { get; set; }
        public string ParentCode { get; set; }
        public string MemberID { get; set; }
        public GuestInfo GuestInfo { get; set; }
        public GuestCounts GuestCounts { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Action { get; set; }
        public PriceLevel PriceLevel { get; set; }
        public List<AttributePair> BookingAttributes { get; set; }
        public List<AttributePair> CancellationAttributes { get; set; }
    }
}
