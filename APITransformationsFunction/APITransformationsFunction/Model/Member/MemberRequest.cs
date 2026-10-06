using APITransformationsFunction.Model.Shared;

namespace APITransformationsFunction.Model.Member
{
    public class MemberRequest
    {
        public required string Id { get; set; }
        public required string Source { get; set; }
        public required string SpecVersion { get; set; }
        public required string Type { get; set; }
        public string CorrelationId { get; set; }
        public required Data Data { get; set; }
    }
}
