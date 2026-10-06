#nullable enable
using APITransformationsFunction.Model.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APITransformationsFunction.Model.Search
{
    public class OTAInventorySearchRequest
    {
        public required string CorrelationID { get; set; }
        public required string TradingAgreementCode { get; set; }
        public bool IncludePropertyMetadata { get; set; } = true;
        public bool IncludeRateMetadata { get; set; } = true;
        public bool IncludeContactInfo { get; set; } = true;
        public bool IncludeUnitMetadata { get; set; } = true;
        public bool IncludeAvailablePropertiesOnly { get; set; } = true;
        public required OTAPropertySearchCriteria PropertySearchCriteria { get; set; }
    }

    public class OTAPropertySearchCriteria
    {
        public bool PropertiesOnly { get; set; } = true;
        public OTAOrderBy OrderBy { get; set; } = new();
        public List<string>? PropertyCodes { get; set; }
        public OTAGeoLocation? GeoLocation { get; set; }
        public required OTAStayDateRange StayDateRange { get; set; }
        public OTARateRange? RateRange { get; set; }
        public List<OTARatePlan>? RatePlans { get; set; }
        public List<OTAPropertyAmenity>? PropertyAmenities { get; set; }
        public required List<OTAGuestCount> GuestCounts { get; set; }
        public List<OTALoyaltyProgram>? LoyaltyPrograms { get; set; }
    }

    public class OTAGeoLocation
    {
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(0, 80)]
        public double RadiusDistance { get; set; }

        [RegularExpression("MI")]
        public required string DistanceUnit { get; set; }
    }
    public class OTAStayDateRange
    {
        [JsonConverter(typeof(StrictDateOnlyConverter))]
        public DateTime Start { get; set; }

        [JsonConverter(typeof(StrictDateOnlyConverter))]
        public DateTime End { get; set; }

        [Range(0, 10)]
        public int? FlexibleDays { get; set; }
    }

    public class OTARateRange
    {

        [Range(0, double.MaxValue)]
        public required double MinRate { get; set; }

        [Range(0, double.MaxValue)]
        public required double MaxRate { get; set; }

        [RegularExpression("USD")]
        public required string CurrencyCode { get; set; }

        [EnumDataType(typeof(RateTimeUnit))]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required RateTimeUnit RateTimeUnit { get; set; }
    }

    public class OTARatePlan
    {
        [StringLength(10)]
        public required string RatePlanCode { get; set; }
    }
    public class OTAPropertyAmenity
    {
        [StringLength(20)]
        public string? Code { get; set; }
    }

    public class OTAGuestCount
    {
        [Range(1, int.MaxValue)]
        public int? Count { get; set; }

        [RegularExpression(@"^(7|8|9|10|11|12)$")]
        public int? AgeQualifyingCode { get; set; }
    }
    public class OTAOrderBy
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(OrderByField))]
        public OrderByField Field { get; set; } = OrderByField.property;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(OrderByDirection))]
        public OrderByDirection Direction { get; set; } = OrderByDirection.asc;
    }
    public class OTALoyaltyProgram
    {
        public string? ProgramCode { get; set; }
    }
}
