using OneExchangeOrderService.Domain.Models.V2.Reservation.Request;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Cancellation.Request
{
    [ExcludeFromCodeCoverage]
    public class CancelItemRequest
    {
        [JsonPropertyName("systemMeta")]
        public required SystemMeta SystemMeta { get; set; }

        [JsonPropertyName("externalReference")]
        public required ExternalReference ExternalReference { get; set; }

        [JsonPropertyName("bookingId")]
        public string? BookingId { get; set; }

        [JsonPropertyName("cancellationReason")]
        public required string CancellationReason { get; set; }

        [JsonPropertyName("cancellationDate")]
        public required DateTime CancellationDate { get; set; }

        [JsonPropertyName("refundedAmount")]
        public decimal? RefundedAmount { get; set; }

        [JsonPropertyName("cancellationStatus")]
        public required string CancellationStatus { get; set; }

        [JsonPropertyName("bookingItems")]
        public List<BookingItem>? BookingItems { get; set; }

    }
}