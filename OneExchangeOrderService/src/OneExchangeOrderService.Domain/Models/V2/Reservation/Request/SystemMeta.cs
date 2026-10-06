using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Request
{
    public class SystemMeta
    {
        [JsonPropertyName("tenantID")] public string TenantID { get; set; } = default!;
        [JsonPropertyName("_insertedDateTime")] public DateTime InsertedDateTime { get; set; }
        [JsonPropertyName("_insertedBy")] public string? InsertedBy { get; set; }
        [JsonPropertyName("_updatedDateTime")] public DateTime UpdatedDateTime { get; set; }
        [JsonPropertyName("_updatedBy")] public string? UpdatedBy { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("statusReason")] public string? StatusReason { get; set; }
    }
}