using APITransformationsFunction.Model.Shared;

namespace APITransformationsFunction.Model.Member
{
    public class OnexGetMemberResponse
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string SpecVersion { get; set; }
        public string Type { get; set; }
        public string CorrelationId { get; set; }
        public Data Data { get; set; }
    }
}
