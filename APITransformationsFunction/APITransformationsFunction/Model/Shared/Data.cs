#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace APITransformationsFunction.Model.Shared
{
    public class Data
    {
        public required SystemMeta SystemMeta { get; set; }

        public required ExternalReference ExternalReference { get; set; }

        public PersonName? PersonName { get; set; }

        public List<PostalAddress>? PostalAddresses { get; set; }

        public List<PhoneNumber>? PhoneNumbers { get; set; }

        public List<DigitalAddresses>? DigitalAddresses { get; set; }

        public MembershipInformation? MembershipInformation { get; set; }

        public List<OtherAttribute>? OtherAttributes { get; set; }

        [JsonPropertyName("memberResponse")]
        public MemberResponseMessage? MemberResponseMessage { get; set; }

        public List<Error>? Errors { get; set; }
    }
}
