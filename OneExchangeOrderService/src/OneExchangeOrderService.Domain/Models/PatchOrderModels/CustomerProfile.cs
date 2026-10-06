using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


namespace OneExchangeOrderService.Domain.Models.PatchOrderModels
{
    [ExcludeFromCodeCoverage]
    public class CustomerProfile
    {
        [JsonPropertyName("addresses")]
        public List<CustomerAddress> Addresses { get; set; }
    }
}
