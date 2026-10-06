using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Cancellation.Request
{
    [ExcludeFromCodeCoverage]
    public class CancellationRequest
    {
        [JsonPropertyName("id")] public required string Id { get; set; } = default!;
        [JsonPropertyName("source")] public required string Source { get; set; } = default!;
        [JsonPropertyName("specversion")] public required string Specversion { get; set; } = "1.0";
        [JsonPropertyName("type")] public required string Type { get; set; } = default!;
        [JsonPropertyName("datacontenttype")] public string? Datacontenttype { get; set; }
        [JsonPropertyName("dataschema")] public string? Dataschema { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("time")] public DateTime? Time { get; set; }
        [JsonPropertyName("ispartial")] public bool? Ispartial { get; set; }
        [JsonPropertyName("data")] public CancelItemRequest Data { get; set; } = default!;
    }
}
