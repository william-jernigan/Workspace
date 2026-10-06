using System.Text.Json.Serialization;

namespace APITransformationsFunction.Model.Reservation.Create.Request
{
    public class ExternalReference
    {
        [JsonPropertyName("sourceSystem")] public string SourceSystem { get; set; }
        [JsonPropertyName("targetSystem")] public string TargetSystem { get; set; }
        [JsonPropertyName("sourceReferenceId")] public string SourceReferenceId { get; set; }
        [JsonPropertyName("targetReferenceId")] public string TargetReferenceId { get; set; }
    }
}
