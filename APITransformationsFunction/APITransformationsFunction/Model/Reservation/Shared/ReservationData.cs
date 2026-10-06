using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Reservation.Shared
{
    public class Profile
    {
        public MemberProfile MemberProfile { get; set; }
    }

    public class UnitStay
    {
        public string UnitTypeCode { get; set; }
        public string UnitDescription { get; set; }
        public List<RatePlan> RatePlan { get; set; }
        public List<SpecialRequest> SpecialRequests { get; set; }
        public List<RoomRate> RoomRates { get; set; }
        public List<GuestCount> GuestCounts { get; set; }
        public UnitStayDates UnitStayDates { get; set; }
        public Guarantee Guarantee { get; set; }
    }


    public class RatePlan
    {
        public string RatePlanCode { get; set; }
        public string EffectiveDate { get; set; }
        public bool GuaranteeRequired { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class SpecialRequest
    {
        public string RequestCode { get; set; }
        public string Text { get; set; }
    }


    public class RoomRate
    {
        public int NumberOfUnits { get; set; }
        public List<RateDetail> Rates { get; set; }
        public List<NightlyRate> NightlyRates { get; set; }
    }

    public class GuestCount
    {
        public int Count { get; set; }
        public string AgeQualifyingCode { get; set; }
    }

    public class UnitStayDates
    {
        public string Start { get; set; }
        public string End { get; set; }
    }


    public class Guarantee
    {
        public string GuaranteeType { get; set; }
        public double Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string Deadline { get; set; }
        public bool GuaranteedAccepted { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class NightlyRate
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public double BaseAmount { get; set; }
        public double TaxAmount { get; set; }
        public double FeeAmount { get; set; }
        public double TotalAmount { get; set; }
        public string CurrencyCode { get; set; }
    }

    public class RateDetail
    {
        public Base Base { get; set; }
        public List<Tax> Taxes { get; set; }
        public List<Fee> Fees { get; set; }
        public Total Total { get; set; }
    }

    public class Total
    {
        public double? AmountAfterTax { get; set; }
        public string CurrencyCode { get; set; }
        public int? DecimalPlaces { get; set; }
    }

    public class Base
    {
        public double? AmountBeforeTax { get; set; }
        public string CurrencyCode { get; set; }
        public int? DecimalPlaces { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Tax
    {
        public double Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int DecimalPlaces { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Fee
    {
        public double Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int DecimalPlaces { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
    }
    public class MemberProfile
    {
        public List<SourceSystemId> SourceSystemIds { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DateOfBirth { get; set; }
        public string Sex { get; set; }
        public bool IsOccupied { get; set; }
        public string ProfileType { get; set; }
        public ContactInfo ContactInfo { get; set; }
    }
    public class SourceSystemId
    {
        public string SourceSystem { get; set; }
        public string Id { get; set; }
    }

    public class ContactInfo
    {
        public List<EmailDetails> Emails { get; set; }
        public List<Phone> Phones { get; set; }
        public List<Address> Addresses { get; set; }
    }

    public class EmailDetails
    {
        public string Email { get; set; }
        public string EmailType { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class Phone
    {
        public string PhoneNumber { get; set; }
        public string PhoneTechType { get; set; }
        public string CountryCode { get; set; }
        public string AreaCode { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class Address
    {
        public string AddressType { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string CityName { get; set; }
        public string PostalCode { get; set; }
        public string StateProv { get; set; }
        public string State { get; set; }
        public string CountryCode { get; set; }
        public string Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class PropertyDetails
    {
        public BasicPropertyInfoSummary BasicPropertyInfoSummary { get; set; }
    }

    public class BasicPropertyInfoSummary
    {
        public string PropertyCode { get; set; }
        public string PropertyCategory { get; set; }
        public string PropertySegment { get; set; }
        public string ChainCode { get; set; }
        public string BrandCode { get; set; }
        public string MarketingKeyCode { get; set; }
        public string PmsID { get; set; }
        public bool? GuaranteeRequired { get; set; }
        public Policies Policies { get; set; }
        public List<UnitStay> UnitStays { get; set; }
        public LoyaltyRedemption LoyaltyRedemption { get; set; }
        public object GuestProfiles { get; set; } // check
        public List<Profile> Profiles { get; set; }

    }

    public class Policies
    {
        public CancellationPolicy CancellationPolicy { get; set; }
    }
    public class CancellationPolicy
    {
        public string PolicyCode { get; set; }
        public string Description { get; set; }
        public string Deadline { get; set; }
    }

    public class CancellationInfo
    {
        public string CancelNumber { get; set; }
        public string CancelDate { get; set; }
        public string CancelReason { get; set; }
        public string Status { get; set; }
    }

    public class LoyaltyRedemption
    {
        public string CertificateNumber { get; set; }
        public string ProgramName { get; set; }
        public string MembershipId { get; set; }
        public string PromotionCode { get; set; }
        public double? RedemptionQuantity { get; set; }
        public string CurrencyCode { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ErrorDetails
    {
        public string Type { get; set; }
        public string Code { get; set; }
        public int Status { get; set; }
        public string ShortText { get; set; }
    }

}
