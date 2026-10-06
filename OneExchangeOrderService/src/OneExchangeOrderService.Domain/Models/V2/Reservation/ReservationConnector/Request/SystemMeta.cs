using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request
{
    [ExcludeFromCodeCoverage]
    public class SystemMeta
    {
        public string TenantID { get; set; }
        [JsonProperty("_insertedDateTime")]
        public string InsertedDateTime { get; set; }
        [JsonProperty("_insertedBy")]
        public string InsertedBy { get; set; }
        [JsonProperty("_updatedDateTime")]
        public string UpdatedDateTime { get; set; }
        [JsonProperty("_updatedBy")]
        public string UpdatedBy { get; set; }
        public bool Status { get; set; }
        public string StatusReason { get; set; }
    }
}
