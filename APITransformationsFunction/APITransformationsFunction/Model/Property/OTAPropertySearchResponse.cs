using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Property
{
    public class OTAPropertySearchResponse
    {
        public string CorrelationID { get; set; }
        public string TradingAgreementCode { get; set; }
        public Pagination Pagination { get; set; }
        public List<PropertyResponseItem> Properties { get; set; }
    }

    public class Pagination
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class PropertyResponseItem
    {
        public BasicPropertyInfoSummary BasicPropertyInfoSummary { get; set; }
        public List<UnitDetail> UnitDetails { get; set; }
    }

    public class BasicPropertyInfoSummary
    {
        public string PropertyCode { get; set; }
        public string PropertyCategory { get; set; }
        public string PropertySegment { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string ChainCode { get; set; }
        public string BrandCode { get; set; }
        public string CityCode { get; set; }
        public string ShortDescription { get; set; }
        public string LongDescription { get; set; }
        public string Website { get; set; }
        public List<string> CheckInDay { get; set; }
        public string CheckInTime { get; set; }
        public List<PropertyContactInfo> ContactInfo { get; set; }
        public Policies Policies { get; set; }
        public List<PropertyActivity> PropertyActivities { get; set; }
        public List<Advisory> Advisories { get; set; }
        public List<PropertyAmenity> PropertyAmenities { get; set; }
        public List<MultimediaDescription> MultimediaDescriptions { get; set; }
    }

    public class UnitDetail
    {
        public int NumberOfUnits { get; set; }
        public string UnitTypeCode { get; set; }
        public Description Description { get; set; }
        public UnitSize UnitSize { get; set; }
        public UnitOccupancy UnitOccupancy { get; set; }
        public BedType BedType { get; set; }
        public List<FeatureAccessibility> FeatureAccessibilities { get; set; }
        public List<FeatureSecurity> FeatureSecurities { get; set; }
        public List<PropertyAmenity> UnitAmenities { get; set; }
        public List<MultimediaDescription> MultimediaDescriptions { get; set; }
    }
    public class Description
    {
        public string Name { get; set; }
        public string Text { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class UnitSize
    {
        public double Size { get; set; }
        public string UnitOfMeasure { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class UnitOccupancy
    {
        public int MinOccupancy { get; set; }
        public int MaxOccupancy { get; set; }
        public int StandardOccupancy { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class BedType
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class MultimediaDescription
    {
        public string MediaType { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string Format { get; set; }
        public string Category { get; set; }
        public string Id { get; set; }
    }
    public class PropertyContactInfo
    {
        public string LocationType { get; set; }
        public List<EmailInfo> Emails { get; set; }
        public List<PhoneInfo> Phones { get; set; }
        public List<AddressInfo> Addresses { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class EmailInfo
    {
        public string Email { get; set; }
        public string EmailType { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class PhoneInfo
    {
        public string PhoneNumber { get; set; }
        public string PhoneTechType { get; set; }
        public string CountryCode { get; set; }
        public string AreaCode { get; set; }
    }

    public class AddressInfo
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
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Policies
    {
        public List<CancellationPolicy> CancellationPolicy { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class CancellationPolicy
    {
        public string PolicyCode { get; set; }
        public string Description { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class PropertyActivity
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Advisory
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class PropertyAmenity
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string ExistsCode { get; set; }
        public string CategoryCode { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class FeatureAccessibility
    {
        public int Quantity { get; set; }
        public string Description { get; set; }
        public string AccessibilityFeature { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class FeatureSecurity
    {
        public int Quantity { get; set; }
        public string Description { get; set; }
        public string SecurityFeature { get; set; }
    }
}
