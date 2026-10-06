using System.Collections.Generic;

namespace APITransformationsFunction.Model.Shared
{
    public class MemberProfile
    {
        public List<SourceSystemId> SourceSystemIds { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BirthDate { get; set; }
        public string Gender { get; set; }
        public string MemberTier { get; set; }
        public string ProfileType { get; set; }
        public string MarketingKeyCode { get; set; }
        public List<AdditionalIdentifier> AdditionalIdentifiers { get; set; }
    }

    public class SourceSystemId
    {
        public string SourceSystem { get; set; }
        public string Id { get; set; }
    }
}
