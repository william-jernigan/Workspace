using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Response
{
    public class GuaranteeAndCancelPolicy
    {
        [JsonPropertyName("guaranteeDescription")] public string? GuaranteeDescription { get; set; }
        [JsonPropertyName("guaranteeType")] public string? GuaranteeType { get; set; }
        [JsonPropertyName("cancelPenalties")] public List<CancelPenalty> CancelPenalties { get; set; } = [];
    }
}
