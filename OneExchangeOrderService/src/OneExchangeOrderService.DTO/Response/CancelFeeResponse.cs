
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.DTO.Response
{
    [ExcludeFromCodeCoverage]
    public class CancelFeeResponse
    {
        [JsonPropertyName("CancellationFee")]
        public double CancellationFee { get; set; }

        [JsonPropertyName("RefundPoints")]
        public bool IsRefundPoints { get; set; }

        public List<ValueAttributeResponse>? Messages { get; set; }
    }
}
