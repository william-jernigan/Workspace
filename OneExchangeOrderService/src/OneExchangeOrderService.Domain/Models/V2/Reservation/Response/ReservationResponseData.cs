using OneExchangeOrderService.Domain.Models.V2.Reservation.Request;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Response
{

    public sealed class ReservationResponseData
    {
        [JsonPropertyName("systemMeta")] public SystemMeta SystemMeta { get; set; } = new();
        [JsonPropertyName("externalReference")] public ExternalReference ExternalReference { get; set; } = new();
        [JsonPropertyName("bookingStatus")] public string BookingStatus { get; set; } = "CONFIRMED";
        [JsonPropertyName("bookingId")] public string BookingId { get; set; } = default!;
        [JsonPropertyName("paymentStatus")] public string PaymentStatus { get; set; } = "PAID";
        [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }

        [JsonPropertyName("bookingItems")] public List<BookingItemResponse> BookingItems { get; set; } = [];
        [JsonPropertyName("error")] public ErrorInfo Error { get; set; } = new();
        [JsonPropertyName("guaranteeAndCancelPolicy")] public GuaranteeAndCancelPolicy GuaranteeAndCancelPolicy { get; set; } = new();
    }
}

