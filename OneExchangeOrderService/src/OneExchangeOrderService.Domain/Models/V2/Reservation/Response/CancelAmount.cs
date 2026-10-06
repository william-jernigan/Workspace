using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Response
{
    public class CancelAmount
    {
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("feesInclusive")] public bool? FeesInclusive { get; set; }
        [JsonPropertyName("taxInclusive")] public bool? TaxInclusive { get; set; }
    }
}