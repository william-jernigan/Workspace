#nullable enable
using APITransformationsFunction.Model.Shared;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APITransformationsFunction.Model.Member
{
    public class ResData
    {
        public required SystemMeta SystemMeta { get; set; }
        public ExternalReference? ExternalReference { get; set; }

        [JsonPropertyName("memberResponse")]
        public MemberResponseMessage? MemberResponseMessage { get; set; }

        public List<Error>? Errors { get; set; }
    }
}
