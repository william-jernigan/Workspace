using Newtonsoft.Json;
using System.Text.Json.Serialization;
namespace OneExchangeOrderService.Application.Models.Resort
{
    public class Overview
    {
        [JsonPropertyName("location")]
        public Location Location { get; set; }


        [JsonProperty("checkInTime")]
        public string CheckInTime { get; set; }

        [JsonProperty("checkOutTime")]
        public string CheckOutTime { get; set; }

        [JsonProperty("contactPhoneNumber")]
        public string ContactPhoneNumber { get; set; }
    }
}
