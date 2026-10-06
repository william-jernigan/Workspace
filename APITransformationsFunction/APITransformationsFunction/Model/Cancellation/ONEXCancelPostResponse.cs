#nullable enable
using System;
using System.Collections.Generic;

namespace APITransformationsFunction.Model.Cancellation
{
    public class ONEXCancelPostResponse
    {
        public string? Id { get; set; }
        public string? Source { get; set; }
        public string Specversion { get; set; } = "1.0";
        public string Type { get; set; } = "Reservation-Response";
        public string Datacontenttype { get; set; } = "application/json";
        public string Dataschema { get; set; } = "https://schemas.onetruth.example/reservations/response/2020-12";
        public string? Subject { get; set; }
        public DateTimeOffset Time { get; set; }
        public bool Ispartial { get; set; }
        public CancelItemResponse Data { get; set; } = new();
    }
    public class CancelItemResponse
    {
        public SystemMeta? SystemMeta { get; set; }
        public ExternalReference? ExternalReference { get; set; }
        public string? BookingId { get; set; }
        public string? CancellationReason { get; set; }
        public DateTimeOffset CancellationDate { get; set; }
        public decimal? RefundedAmount { get; set; }
        public string? CancellationStatus { get; set; }
        public List<BookingItem>? BookingItems { get; set; }
        public ErrorInfo? Error { get; set; }
    }
}
