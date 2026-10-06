using System;
using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Create.Request
{
    public class SystemMeta
    {
        [JsonPropertyName("tenantID")] public string TenantID { get; set; } = default!;
        [JsonPropertyName("_insertedDateTime")] public DateTimeOffset? InsertedDateTime { get; set; }
        [JsonPropertyName("_insertedBy")] public string InsertedBy { get; set; }
        [JsonPropertyName("_updatedDateTime")] public DateTimeOffset? UpdatedDateTime { get; set; }
        [JsonPropertyName("_updatedBy")] public string UpdatedBy { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("statusReason")] public string StatusReason { get; set; }
    }
}
