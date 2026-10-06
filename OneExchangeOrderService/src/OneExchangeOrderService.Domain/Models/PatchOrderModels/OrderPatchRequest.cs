using OneExchangeOrderService.Domain.Models.PatchOrderModels;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class OrderPatchRequest
    {
        [JsonPropertyName("op")]
        public string Op { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("value")]
        public OrderDetail Value { get; set; }
    }
}
