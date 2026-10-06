using System.Text.Json.Serialization;
namespace OneExchangeOrderService.Application.Models.Resort
{
    public class Location
    {
        [JsonPropertyName("street1")]
        public string Street1 { get; set; }

        [JsonPropertyName("street2")]
        public string Street2 { get; set; }

        [JsonPropertyName("street3")]
        public string Street3 { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("zipCode")]
        public string ZipCode { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }
}
