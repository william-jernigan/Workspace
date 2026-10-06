#nullable enable
using APITransformationsFunction.Model.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace APITransformationsFunction.Model.Search
{
    public class ONEXInventorySearchRequest
    {
        public required string Id { get; set; }
        public required string Source { get; set; }
        public required string Specversion { get; set; }
        public required string Type { get; set; }
        public string? CorrelationId { get; set; }
        public string? Datacontenttype { get; set; }
        public required Uri? Dataschema { get; set; }
        public string? Subject { get; set; }
        public DateTime? Time { get; set; }
        public bool? Ispartial { get; set; }
        public required ONEXInventorySearchData Data { get; set; }
    }

    public class ONEXInventorySearchData
    {
        public required ONEXSystemMeta SystemMeta { get; set; }
        public required ONEXSearchCriteria SearchCriteria { get; set; }
    }
    public class ONEXSystemMeta
    {
        public string? TenantID { get; set; }

        [JsonPropertyName("_insertedDateTime")]
        public DateTime? InsertedDateTime { get; set; }

        [JsonPropertyName("_insertedBy")]
        public string? InsertedBy { get; set; }

        [JsonPropertyName("_updatedDateTime")]
        public DateTime? UpdatedDateTime { get; set; }

        [JsonPropertyName("_updatedBy")]
        public string? UpdatedBy { get; set; }
        public bool? Status { get; set; }
        public string? StatusReason { get; set; }
    }
    public class ONEXSearchCriteria
    {
        public required string TradingAgreementCode { get; set; }
        public bool PropertiesOnly { get; set; } = true;
        public List<string>? PropertyCodes { get; set; }
        public string? UnitTypeCode { get; set; }
        public ONEXGeoLocation? GeoLocation { get; set; }
        public required ONEXStayDateRange StayDateRange { get; set; }
        public ONEXRateRange? RateRange { get; set; }
        public List<ONEXRatePlan>? RatePlans { get; set; }
        public List<ONEXPropertyAmenity>? PropertyAmenities { get; set; }
        public List<ONEXGuestCount> GuestCounts { get; set; } = [];
        public List<ONEXLoyaltyProgram>? LoyaltyPrograms { get; set; }
        public ONEXOrderBy SortBy { get; set; } = new();
        public ONEXIncludeFlags Include { get; set; } = new();
    }

    public class ONEXGeoLocation
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

    public class ONEXStayDateRange
    {
        [JsonConverter(typeof(StrictDateOnlyConverter))]
        public DateTime Start { get; set; }

        [JsonConverter(typeof(StrictDateOnlyConverter))]
        public DateTime End { get; set; }

        [Range(0, 10)]
        public int? FlexibleDays { get; set; }
    }

    public class ONEXRateRange
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

    public class ONEXRatePlan
    {
        [StringLength(10)]
        public required string RatePlanCode { get; set; }
    }

    public class ONEXPropertyAmenity
    {
        [StringLength(20)]
        public required string Code { get; set; }
    }
    public class ONEXGuestCount
    {
        [Range(1, int.MaxValue)]
        public int? Count { get; set; } = 1;

        [RegularExpression(@"^(7|8|9|10|11|12)$")]
        public int? AgeQualifyingCode { get; set; } = 10;
    }

    public class ONEXLoyaltyProgram
    {
        [StringLength(10)]
        public required string ProgramCode { get; set; }
    }

    public class ONEXIncludeFlags
    {
        public bool PropertyMetadata { get; set; } = true;
        public bool RateMetadata { get; set; } = true;
        public bool ContactInfo { get; set; } = true;
        public bool UnitMetadata { get; set; } = true;
        public bool AvailablePropertiesOnly { get; set; } = true;
    }
    public class ONEXOrderBy
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(OrderByField))]
        public OrderByField Field { get; set; } = OrderByField.property;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(OrderByDirection))]
        public OrderByDirection Direction { get; set; } = OrderByDirection.asc;
    }
}
