using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Property
{
    [ExcludeFromCodeCoverage]
    public class ONEXPropertySearchResponse
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string Specversion { get; set; }
        public string Type { get; set; }
        public string CorrelationId { get; set; }
        public string Datacontenttype { get; set; }
        public string Dataschema { get; set; }
        public string Subject { get; set; }
        public string Time { get; set; }
        public bool? Ispartial { get; set; }
        public string Authorization { get; set; }
        public Data Data { get; set; } = null!;
    }
}
