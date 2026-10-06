using APITransformationsFunction.Model.Shared;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Member
{
    [ExcludeFromCodeCoverage]
    public class OTAPatchMemberRequest
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public MemberProfile MemberProfile { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public List<LoyaltyProgram> LoyaltyPrograms { get; set; }
        public string MemberStatus { get; set; }
    }
}
