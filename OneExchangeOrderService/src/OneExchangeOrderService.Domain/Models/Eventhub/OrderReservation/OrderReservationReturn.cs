using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation
{
    [ExcludeFromCodeCoverage]

    public class OrderReservationReturn
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("returnNumber")]
        public string? ReturnNumber { get; set; }

        [JsonPropertyName("returnDate")]
        public DateTime ReturnDate { get; set; }

        [JsonPropertyName("salesPersonProfile")]
        public SalesPersonProfile? SalesPersonProfile { get; set; }

        [JsonPropertyName("returnFeeCurrency")]
        public string? ReturnFeeCurrency { get; set; }

        [JsonPropertyName("returnFeeAmount")]
        public string? ReturnFeeAmount { get; set; }

        [JsonPropertyName("memo")]
        public string? Memo { get; set; }
        public bool? IsPointsTobeRefund { get; set; }
    }

    public class SalesPersonProfile
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime ReturnDate { get; set; }
        public List<DigitalAddresses>? DigitalAddresses { get; set; }
    }

    public class DigitalAddresses
    {
        public string? DigitalAddress { get; set; }
    }

}
