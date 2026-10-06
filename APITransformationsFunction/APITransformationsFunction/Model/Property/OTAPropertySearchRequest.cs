#nullable enable
using APITransformationsFunction.Model.Shared;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static APITransformationsFunction.Common.ApiConstants;

namespace APITransformationsFunction.Model.Property
{
    public class OTAPropertySearchRequest
    {
        [Required(ErrorMessage = "CorrelationID is required.")]
        public required string CorrelationID { get; set; }

        [Required(ErrorMessage = "TradingAgreementCode is required.")]
        public required string TradingAgreementCode { get; set; }
        public GeoLocation? GeoLocation { get; set; }
        public List<PropertyItem>? Properties { get; set; }
        public bool MetadataOnly { get; set; }

        [EnumDataType(typeof(IncludeMedia), ErrorMessage = "Invalid value for IncludeMedia and Currently no media available for Units.")]
        public string? IncludeMedia { get; set; }

        [EnumDataType(typeof(MediaType), ErrorMessage = "Invalid value for MediaType and Currently no Video mediaType is available.")]
        public string? MediaType { get; set; }

        [EnumDataType(typeof(IncludeAmenities), ErrorMessage = "Invalid value for IncludeAmenities")]
        public string? IncludeAmenities { get; set; }
    }
}
