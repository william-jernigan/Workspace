using APITransformationsFunction.Model.Shared;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Member
{
    [ExcludeFromCodeCoverage]
    public class OTAUpdateMemberResponse
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public OTAUpdateMemberProfile MemberProfile { get; set; }
        public string MemberStatus { get; set; }
        public string Message { get; set; }
        public MemberInfo MemberInfo { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class OTAUpdateMemberProfile
    {
        public List<SourceSystemId> SourceSystemIds { get; set; }
    }
}
