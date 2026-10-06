#nullable enable
using System.ComponentModel.DataAnnotations;

namespace APITransformationsFunction.Model.Member
{
    public class MemberActRequest
    {
        public string? Id { get; set; }

        public required string Source { get; set; }

        public required string SpecVersion { get; set; }

        public required string Type { get; set; }

        public string? Datacontenttype { get; set; }
        public string? CorrelationId { get; set; }

        [Url]
        public string? Dataschema { get; set; }

        public string? Subject { get; set; }
        public string? Time { get; set; }

        public bool? Ispartial { get; set; }

        public required ActData Data { get; set; }
    }
}
