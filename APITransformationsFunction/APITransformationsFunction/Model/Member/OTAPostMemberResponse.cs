using APITransformationsFunction.Model.Shared;
using System.Collections.Generic;

namespace APITransformationsFunction.Model.Member
{
    public class OTAPostMemberResponse
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public OTAMemberProfile MemberProfile { get; set; }
        public string MemberStatus { get; set; }
        public string Message { get; set; }
        public MemberInfo MemberInfo { get; set; }
    }

    public class OTAMemberProfile
    {
        public List<SourceSystemId> SourceSystemIds { get; set; }
    }

}
