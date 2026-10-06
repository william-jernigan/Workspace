using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Search
{
    public class OTAInventorySearchResponse
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public Pagination Pagination { get; set; }
        public List<PropertyItem> Properties { get; set; }
        public List<RatePlan>? RatePlans { get; set; }
    }

    public class Pagination
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class PropertyItem
    {
        public BasicPropertyInfoSummary BasicPropertyInfoSummary { get; set; }
        public List<UnitType> UnitTypes { get; set; }
    }

    public class BasicPropertyInfoSummary
    {
        public string PropertyCode { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string ChainCode { get; set; }
        public string BrandCode { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ContactInfo
    {
        public string LocationType { get; set; }
        public List<Email> Emails { get; set; }
        public List<Phone> Phones { get; set; }
        public List<Address> Addresses { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Email
    {
        public string EmailAddress { get; set; }
        public string EmailType { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Phone
    {
        public string PhoneNumber { get; set; }
        public string PhoneTechType { get; set; }
        public string CountryCode { get; set; }
        public string AreaCode { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Address
    {
        public string AddressType { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string PostalCode { get; set; }
        public string StateProv { get; set; }
        public string State { get; set; }
        public string CountryCode { get; set; }
        public string Country { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class OTAInventoryPropertyAmenityResponse
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public int? Quantity { get; set; }
        public string ExistsCode { get; set; }
        public string CategoryCode { get; set; }
    }

    public class UnitType
    {
        public int? NumberOfUnits { get; set; }
        public string UnitTypeCode { get; set; }
        public bool? SmokingInd { get; set; }
        public bool? CompositeInd { get; set; }
        public UnitDescription? Description { get; set; }
        public UnitSize? UnitSize { get; set; }
        public UnitOccupancy? UnitOccupancy { get; set; }
        public BedType? BedType { get; set; }
        public Availability Availability { get; set; }
    }

    public class UnitDescription
    {
        public string Name { get; set; }
        public string Text { get; set; }
    }

    public class UnitSize
    {
        public double Size { get; set; }
        public string UnitOfMeasure { get; set; }
    }

    public class UnitOccupancy
    {
        public int? MinOccupancy { get; set; }
        public int? MaxOccupancy { get; set; }
        public int? StandardOccupancy { get; set; }
    }

    public class BedType
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    public class Availability
    {
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int? Nights { get; set; }
        public int? Quantity { get; set; }
        public List<RoomRate> RoomRates { get; set; }
    }

    public class RoomRate
    {
        public string RatePlanCode { get; set; }
        public List<Rate> Rates { get; set; }
        public List<NightlyRate> NightlyRates { get; set; }
    }

    public class Rate
    {
        public BaseRate Base { get; set; }
        public List<Tax> Taxes { get; set; }
        public List<Fee> Fees { get; set; }
        public TotalRate Total { get; set; }
    }

    public class BaseRate
    {
        public string AmountBeforeTax { get; set; }
        public string CurrencyCode { get; set; }
        public int? DecimalPlaces { get; set; } = 2;
    }

    public class Tax
    {
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int? DecimalPlaces { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
    }

    public class Fee
    {
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int? DecimalPlaces { get; set; }
        public string Description { get; set; }
    }
    public class TotalRate
    {
        public string AmountAfterTax { get; set; }
        public string CurrencyCode { get; set; }
        public int DecimalPlaces { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class NightlyRate
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal FeeAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrencyCode { get; set; }
    }

    public class RatePlan
    {
        public string AvailabilityStatus { get; set; }
        public bool IdRequiredInd { get; set; }
        public string RatePlanCode { get; set; }
        public Guarantee Guarantee { get; set; }
        public List<CancelPenalty> CancelPenalties { get; set; }
        public RatePlanDescription RatePlanDescription { get; set; }
    }

    public class Guarantee
    {
        public string GuaranteeType { get; set; }
        public List<GuaranteeAccepted> GuaranteesAccepted { get; set; }
        public GuaranteeDescription GuaranteeDescription { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class GuaranteeAccepted
    {
        public string GuaranteeTypeCode { get; set; }
    }

    public class GuaranteeDescription
    {
        public string Text { get; set; }
    }

    public class CancelPenalty
    {
        public bool NonRefundable { get; set; }
        public Deadline Deadline { get; set; }
        public List<PenaltyDescription> PenaltyDescription { get; set; }
    }

    public class Deadline
    {
        public DateTime AbsoluteDeadline { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class PenaltyDescription
    {
        public string Text { get; set; }
    }

    public class RatePlanDescription
    {
        public string Name { get; set; }
        public string Text { get; set; }
    }
}
