using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request
{
    [ExcludeFromCodeCoverage]
    public class ExternalReference
    {
        public string SourceSystem { get; set; }
        public string TargetSystem { get; set; }
        public string SourceReferenceId { get; set; }
        public string TargetReferenceId { get; set; }
    }
}
