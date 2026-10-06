using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation
{
    [ExcludeFromCodeCoverage]
    public class CancelSystemMetaEvent
    {
        [JsonPropertyName("tenantID")]
        public string TenantID { get; set; }

        [JsonPropertyName("_insertedBy")]
        public string InsertedBy { get; set; }

        [JsonPropertyName("status")]
        public bool Status { get; set; } = true;

        [JsonPropertyName("statusReason")]
        public string StatusReason { get; set; } = "Active";
    }
}
