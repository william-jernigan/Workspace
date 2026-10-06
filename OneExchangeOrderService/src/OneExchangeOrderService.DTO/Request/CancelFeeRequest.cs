
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.DTO.Request
{
    [ExcludeFromCodeCoverage]
    public class CancelFeeRequest
    {
        public required string TradingAgreementCode { get; set; }
        public required DateTime ReservationTime { get; set; }
        public required string CheckInDate { get; set; }
        public required DateTime CurrentTime { get; set; }
        public required string MembershipClub { get; set; }
        public required string MembershipLevel { get; set; }
    }
}
