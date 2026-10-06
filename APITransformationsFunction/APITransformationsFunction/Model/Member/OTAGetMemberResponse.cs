using APITransformationsFunction.Model.Shared;
using System.Collections.Generic;

namespace APITransformationsFunction.Model.Member
{
    public class OTAGetMemberResponse
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public MemberProfile MemberProfile { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public string MemberStatus { get; set; }
        public List<LoyaltyProgram> LoyaltyPrograms { get; set; }
    }
}
