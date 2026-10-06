using System.Text.Json.Serialization;

namespace APITransformationsFunction.Model.Reservation.Create.Response
{
    public class CancelAmount
    {
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("feesInclusive")] public bool? FeesInclusive { get; set; }
        [JsonPropertyName("taxInclusive")] public bool? TaxInclusive { get; set; }
    }
}
