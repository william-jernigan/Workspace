using OneExchangeOrderService.Domain.Models.V2.Cancellation.Request;
using OneExchangeOrderService.Domain.Models.V2.Reservation.Request;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Cancellation.Response
{
    public class CancelItemResponse
    {
        [JsonPropertyName("systemMeta")]
        public SystemMeta? SystemMeta { get; set; }

        [JsonPropertyName("externalReference")]
        public ExternalReference? ExternalReference { get; set; }

        [JsonPropertyName("bookingId")]
        public string? BookingId { get; set; }

        [JsonPropertyName("cancellationReason")]
        public string? CancellationReason { get; set; }

        [JsonPropertyName("cancellationDate")]
        public DateTime? CancellationDate { get; set; }

        [JsonPropertyName("refundedAmount")]
        public decimal? RefundedAmount { get; set; }

        [JsonPropertyName("cancellationStatus")]
        public string? CancellationStatus { get; set; }

        [JsonPropertyName("bookingItems")]
        public List<BookingItem>? BookingItems { get; set; }

        public ErrorInfo? Error { get; set; }
    }
}
