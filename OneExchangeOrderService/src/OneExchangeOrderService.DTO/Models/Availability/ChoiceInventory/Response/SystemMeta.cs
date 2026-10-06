using System.Text.Json.Serialization;

namespace OneExchangeOrderService.DTO.Models.Availability.ChoiceInventory.Response
{
    public class SystemMeta
    {
        [JsonPropertyName("tenantID")]
        public string TenantID { get; set; }

        [JsonPropertyName("_insertedDateTime")]
        public string InsertedDateTime { get; set; }
        [JsonPropertyName("_insertedBy")]
        public string InsertedBy { get; set; }
        [JsonPropertyName("_updatedDateTime")]
        public string UpdatedDateTime { get; set; }
        [JsonPropertyName("_updatedBy")]
        public string UpdatedBy { get; set; }
    }
}
