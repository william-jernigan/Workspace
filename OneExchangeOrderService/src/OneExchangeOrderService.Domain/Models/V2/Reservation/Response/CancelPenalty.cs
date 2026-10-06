using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Response
{
    public class CancelPenalty
    {
        [JsonPropertyName("nonRefundable")] public bool? NonRefundable { get; set; }
        [JsonPropertyName("deadline")] public CancelDeadline? Deadline { get; set; }
        [JsonPropertyName("amountPercent")] public CancelAmount? AmountPercent { get; set; }
        [JsonPropertyName("penaltyDescription")] public string? PenaltyDescription { get; set; }
    }
}
