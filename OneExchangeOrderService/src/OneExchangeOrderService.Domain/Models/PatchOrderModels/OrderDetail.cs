
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.PatchOrderModels
{
    [ExcludeFromCodeCoverage]
    public class OrderDetail
    {
        [JsonPropertyName("orderStatus")]
        public string OrderStatus { get; set; }

        [JsonPropertyName("memo")]
        public string Memo { get; set; }

        [JsonPropertyName("waiveFee")]
        public bool WaiveFee { get; set; }

        [JsonPropertyName("paymentAccountToken")]
        public string PaymentAccountToken { get; set; }

        [JsonPropertyName("customerProfile")]
        public CustomerProfile CustomerProfile { get; set; }

        [JsonPropertyName("salesPersonProfile")]
        public SalesPersonProfile SalesPersonProfile { get; set; }

        [JsonPropertyName("expirationDate")]
        public string ExpirationDate { get; set; }

        [JsonPropertyName("isImpersonationExpiredMember")]
        public bool? IsImpersonationExpiredMember { get; set; } = false;
    }
}
