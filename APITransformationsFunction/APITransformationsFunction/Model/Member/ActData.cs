#nullable enable
using APITransformationsFunction.Model.Shared;
using System;
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Member
{
    public class ActData
    {
        public required SystemMeta SystemMeta { get; set; }
        public MembershipInformation? MembershipInformation { get; set; }
    }

    public class MembershipInformation
    {
        public int? PastDue { get; set; }

        public string? MembershipLevel { get; set; }

        public string? OwnerType { get; set; }

        public TravelersPlus? TravelersPlus { get; set; }

        public string? AccountNumber { get; set; }

        public int? Points { get; set; }

        public int? PremierGenCode { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class TravelersPlus
    {
        public DateTime ExpirationDate { get; set; }
    }
}
