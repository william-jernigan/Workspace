using APITransformationsFunction.Model.Reservation.Create.Request;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APITransformationsFunction.Model.Reservation.Create.Response
{

    public sealed class ReservationResponseData
    {
        [JsonPropertyName("systemMeta")] public SystemMeta SystemMeta { get; set; } = new();
        [JsonPropertyName("externalReference")] public ExternalReference ExternalReference { get; set; } = new();
        [JsonPropertyName("bookingStatus")] public string BookingStatus { get; set; } = "CONFIRMED";
        [JsonPropertyName("bookingId")] public string BookingId { get; set; } = default!;
        [JsonPropertyName("paymentStatus")] public string PaymentStatus { get; set; } = "PAID";
        [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("bookingItems")] public List<BookingItemResponse> BookingItems { get; set; } = [];
        [JsonPropertyName("error")] public ErrorInfo Error { get; set; } = new();
        [JsonPropertyName("guaranteeAndCancelPolicy")] public GuaranteeAndCancelPolicy GuaranteeAndCancelPolicy { get; set; } = new();
    }
}

