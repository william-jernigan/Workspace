using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation
{
    [ExcludeFromCodeCoverage]
    public class CancelOrderReservationReturn
    {
        [JsonPropertyName("returnNumber")]
        public string? ReturnNumber { get; set; }

        [JsonPropertyName("returnDate")]
        public DateTime ReturnDate { get; set; }

        [JsonPropertyName("salesPersonProfile")]
        public CancelSalesPersonProfile? SalesPersonProfile { get; set; }

        [JsonPropertyName("returnFeeCurrency")]
        public string? ReturnFeeCurrency { get; set; }

        [JsonPropertyName("returnFeeAmount")]
        public string? ReturnFeeAmount { get; set; }

        [JsonPropertyName("memo")]
        public string? Memo { get; set; }

        [JsonPropertyName("waiveFee")]
        public bool WaiveFee { get; set; }

        public bool? IsPointsTobeRefund { get; set; }
    }

    public class CancelSalesPersonProfile
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime ReturnDate { get; set; }
        public List<CancelDigitalAddress>? DigitalAddresses { get; set; }
    }

    public class CancelDigitalAddress
    {
        public string? DigitalAdd { get; set; }
    }
}
