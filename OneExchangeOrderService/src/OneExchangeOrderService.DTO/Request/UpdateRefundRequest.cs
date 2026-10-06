
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.DTO.Request
{
    [ExcludeFromCodeCoverage]
    public class UpdateRefundRequest
    {
        [Required]
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("refundAmount")]
        public decimal RefundAmount { get; set; }

        [Required]
        [JsonPropertyName("originalPaymentId")]
        public string OriginalPaymentId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("confirmationNumber")]
        public string ConfirmationNumber { get; set; } = string.Empty;


        [JsonPropertyName("salesPersonEmail")]
        public string SalesPersonEmail { get; set; }

    }
}