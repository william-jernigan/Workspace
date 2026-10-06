using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Property;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace APITransformationsFunction.Mappers
{
    public static class PropertyMapper
    {
        public static OTAPropertySearchResponse MapOnexToOtaResponse(ONEXPropertySearchResponse source)
        {
            if (source == null || source.Data?.ItemsList == null)
            {
                return null;
            }

            var otaResponse = new OTAPropertySearchResponse
            {
                CorrelationID = source.CorrelationId ?? Guid.NewGuid().ToString(),
                Properties = []
            };

            // Separate resort (property) and unit items
            var resortItems = source.Data.ItemsList
                .Where(i => i.ItemClass.Equals(ApiConstants.ItemClassResort, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var unitItems = source.Data.ItemsList
                .Where(i => i.ItemClass.Equals(ApiConstants.ItemClassUnit, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var resort in resortItems)
            {
                var relatedUnits = unitItems
                    .Where(u => u.ParentCode == resort.ItemCode)
                    .ToList();

                var property = new PropertyResponseItem
                {
                    BasicPropertyInfoSummary = MapBasicProperty(resort),
                    UnitDetails = [.. relatedUnits.Select(unit =>
                    {
                        var key = new { unit.ParentCode, Name = unit.Name?.Trim().ToLowerInvariant() };
                        var count = relatedUnits.Count;
                        return MapUnitDetail(unit, count);
                    })]
                };

                otaResponse.Properties.Add(property);
            }

            return otaResponse;
        }
        private static BasicPropertyInfoSummary MapBasicProperty(Item item)
        {
            var basicPropertyInfoSummary = new BasicPropertyInfoSummary
            {
                PropertyCode = item.ItemCode ?? string.Empty,
                PropertyCategory = item.ItemClass ?? string.Empty,
                ChainCode = ApiConstants.ChainCode,
                BrandCode = ApiConstants.BrandCode,
                CityCode = item.Profile.Addresses?.FirstOrDefault()?.City ?? string.Empty,
                CheckInTime = item.ItemAttributes?
                             .Where(c => c.Attribute == ApiConstants.CheckInTime)
                             .Select(c => c.Value)
                             .FirstOrDefault() ?? string.Empty,
                Name = item.Name ?? string.Empty,
                Status = item.ItemExternalStatus == ApiConstants.PublishedStatus ? ApiConstants.ActiveStatus : ApiConstants.InactiveStatus,
                ShortDescription = item.Description ?? string.Empty,
                LongDescription = item.Description ?? string.Empty,
                Website = item.Profile?.DigitalAddresses?
                                .FirstOrDefault(d => d.DigitalAddressType.Equals(ApiConstants.WebsiteType, StringComparison.OrdinalIgnoreCase))
                                ?.DigitalAddress ?? string.Empty,

                ContactInfo =
                    [
                        new PropertyContactInfo
                        {
                            LocationType = ApiConstants.LocationType,
                            Emails = item.Profile?.DigitalAddresses?
                            .Where(d => !string.Equals(d.DigitalAddressType, ApiConstants.WebsiteType, StringComparison.OrdinalIgnoreCase) && !d.DigitalAddress.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            .Select(d => new EmailInfo
                            {
                                Email = d.DigitalAddress,
                                EmailType = ApiConstants.EmailType
                            }).ToList() ?? [],

                            Phones = item.Profile?.PhoneNumbers?.Select(p => new PhoneInfo
                            {
                                PhoneNumber = p.Number,
                                PhoneTechType = ApiConstants.PhoneTechType
                            }).ToList() ?? [],

                            Addresses = item.Profile?.Addresses?.Select(a => new AddressInfo
                            {
                                AddressType = ApiConstants.AddressType,
                                AddressLine1 = a.Line1,
                                CityName = a.City,
                                PostalCode = a.PostalCode,
                                State = a.State,
                                StateProv = a.StateCode,
                                Country = a.Country,
                                CountryCode = a.CountryCode,
                                Latitude = item.GeoLocation?.Coordinates?.Count > 0 ? item.GeoLocation.Coordinates[0] : 0,
                                Longitude = item.GeoLocation?.Coordinates?.Count > 1 ? item.GeoLocation.Coordinates[1] : 0
                            }).ToList() ?? []
                        }
                    ],

                PropertyAmenities = item.ItemAttributes?
                                .Where(a => a.Type == ApiConstants.AmenityType)
                                .Select(a => new PropertyAmenity
                                {
                                    Description = a.Value,
                                    Code = string.IsNullOrEmpty(a.Attribute) ? a.Value : a.Attribute,
                                    CategoryCode = string.IsNullOrEmpty(a.Attribute) ? a.Value : a.Attribute,
                                }).ToList() ?? [],

                MultimediaDescriptions = item.Media?.Select(m => new MultimediaDescription
                {
                    MediaType = m.MediaType ?? string.Empty,
                    Url = m.MediaURI ?? string.Empty,
                    Description = m.MediaDescription ?? string.Empty,
                }).ToList() ?? []

            };
            return basicPropertyInfoSummary;
        }

        private static UnitDetail MapUnitDetail(Item unit, int unitCount)
        {
            var unitDetail = new UnitDetail
            {
                NumberOfUnits = unitCount,
                UnitTypeCode = unit.ItemCode ?? string.Empty,

                Description = new Description
                {
                    Name = unit.Name ?? string.Empty,
                },

                UnitAmenities = unit.ItemAttributes?
                    .Where(a => a.Type == ApiConstants.AmenityType)
                    .Select(a => new PropertyAmenity
                    {
                        Description = a.Value,
                        Code = string.IsNullOrEmpty(a.Attribute) ? a.Value : a.Attribute,
                        CategoryCode = string.IsNullOrEmpty(a.Attribute) ? a.Value : a.Attribute,
                    }).ToList() ?? [],

                MultimediaDescriptions = unit.Media?.Select(m => new MultimediaDescription
                {
                    MediaType = m.MediaType ?? string.Empty,
                    Url = m.MediaURI ?? string.Empty,
                    Description = m.MediaDescription ?? string.Empty,
                }).ToList() ?? []
            };

            return unitDetail;
        }


        public static OTAErrorResponse MapBackendError(HttpResponseMessage backendResponse)
        {
            var statusCode = backendResponse.StatusCode;
            var correlationId = Guid.NewGuid();

            return new OTAErrorResponse
            {
                Type = statusCode switch
                {
                    HttpStatusCode.BadRequest => "https://api.hgv.com/errors/bad-request",
                    HttpStatusCode.Unauthorized => "https://api.hgv.com/errors/unauthorized",
                    HttpStatusCode.Forbidden => "https://api.hgv.com/errors/forbidden",
                    HttpStatusCode.NotFound => "https://api.hgv.com/errors/not-found",
                    HttpStatusCode.InternalServerError => "https://api.hgv.com/errors/internal-server-error",
                    HttpStatusCode.PaymentRequired => "https://api.hgv.com/errors/payment-required",
                    HttpStatusCode.NotImplemented => "https://api.hgv.com/errors/not-implemented",
                    HttpStatusCode.BadGateway => "https://api.hgv.com/errors/bad-gateway",
                    HttpStatusCode.TooManyRequests => "https://api.hgv.com/errors/too-many-requests",
                    HttpStatusCode.Continue => throw new NotImplementedException(),
                    HttpStatusCode.SwitchingProtocols => throw new NotImplementedException(),
                    HttpStatusCode.Processing => throw new NotImplementedException(),
                    HttpStatusCode.EarlyHints => throw new NotImplementedException(),
                    HttpStatusCode.OK => throw new NotImplementedException(),
                    HttpStatusCode.Created => throw new NotImplementedException(),
                    HttpStatusCode.Accepted => throw new NotImplementedException(),
                    HttpStatusCode.NonAuthoritativeInformation => throw new NotImplementedException(),
                    HttpStatusCode.NoContent => throw new NotImplementedException(),
                    HttpStatusCode.ResetContent => throw new NotImplementedException(),
                    HttpStatusCode.PartialContent => throw new NotImplementedException(),
                    HttpStatusCode.MultiStatus => throw new NotImplementedException(),
                    HttpStatusCode.AlreadyReported => throw new NotImplementedException(),
                    HttpStatusCode.IMUsed => throw new NotImplementedException(),
                    HttpStatusCode.Ambiguous => throw new NotImplementedException(),
                    HttpStatusCode.Moved => throw new NotImplementedException(),
                    HttpStatusCode.Found => throw new NotImplementedException(),
                    HttpStatusCode.RedirectMethod => throw new NotImplementedException(),
                    HttpStatusCode.NotModified => throw new NotImplementedException(),
                    HttpStatusCode.UseProxy => throw new NotImplementedException(),
                    HttpStatusCode.Unused => throw new NotImplementedException(),
                    HttpStatusCode.RedirectKeepVerb => throw new NotImplementedException(),
                    HttpStatusCode.PermanentRedirect => throw new NotImplementedException(),
                    HttpStatusCode.MethodNotAllowed => throw new NotImplementedException(),
                    HttpStatusCode.NotAcceptable => throw new NotImplementedException(),
                    HttpStatusCode.ProxyAuthenticationRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestTimeout => throw new NotImplementedException(),
                    HttpStatusCode.Conflict => throw new NotImplementedException(),
                    HttpStatusCode.Gone => throw new NotImplementedException(),
                    HttpStatusCode.LengthRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionFailed => throw new NotImplementedException(),
                    HttpStatusCode.RequestEntityTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.RequestUriTooLong => throw new NotImplementedException(),
                    HttpStatusCode.UnsupportedMediaType => throw new NotImplementedException(),
                    HttpStatusCode.RequestedRangeNotSatisfiable => throw new NotImplementedException(),
                    HttpStatusCode.ExpectationFailed => throw new NotImplementedException(),
                    HttpStatusCode.MisdirectedRequest => throw new NotImplementedException(),
                    HttpStatusCode.UnprocessableEntity => throw new NotImplementedException(),
                    HttpStatusCode.Locked => throw new NotImplementedException(),
                    HttpStatusCode.FailedDependency => throw new NotImplementedException(),
                    HttpStatusCode.UpgradeRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestHeaderFieldsTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.UnavailableForLegalReasons => throw new NotImplementedException(),
                    HttpStatusCode.ServiceUnavailable => throw new NotImplementedException(),
                    HttpStatusCode.GatewayTimeout => throw new NotImplementedException(),
                    HttpStatusCode.HttpVersionNotSupported => throw new NotImplementedException(),
                    HttpStatusCode.VariantAlsoNegotiates => throw new NotImplementedException(),
                    HttpStatusCode.InsufficientStorage => throw new NotImplementedException(),
                    HttpStatusCode.LoopDetected => throw new NotImplementedException(),
                    HttpStatusCode.NotExtended => throw new NotImplementedException(),
                    HttpStatusCode.NetworkAuthenticationRequired => throw new NotImplementedException(),
                    _ => "https://api.hgv.com/errors/unexpected-error"
                },
                Title = statusCode switch
                {
                    HttpStatusCode.BadRequest => "Bad Request",
                    HttpStatusCode.Unauthorized => "Unauthorized",
                    HttpStatusCode.PaymentRequired => "Payment Required",
                    HttpStatusCode.Forbidden => "Forbidden",
                    HttpStatusCode.NotFound => "Not Found",
                    HttpStatusCode.NotAcceptable => "Not Acceptable",
                    HttpStatusCode.TooManyRequests => "Too Many Requests",
                    HttpStatusCode.InternalServerError => "Internal Server Error",
                    HttpStatusCode.NotImplemented => "Not Implemented",
                    HttpStatusCode.BadGateway => "Bad Gateway",
                    HttpStatusCode.Continue => throw new NotImplementedException(),
                    HttpStatusCode.SwitchingProtocols => throw new NotImplementedException(),
                    HttpStatusCode.Processing => throw new NotImplementedException(),
                    HttpStatusCode.EarlyHints => throw new NotImplementedException(),
                    HttpStatusCode.OK => throw new NotImplementedException(),
                    HttpStatusCode.Created => throw new NotImplementedException(),
                    HttpStatusCode.Accepted => throw new NotImplementedException(),
                    HttpStatusCode.NonAuthoritativeInformation => throw new NotImplementedException(),
                    HttpStatusCode.NoContent => throw new NotImplementedException(),
                    HttpStatusCode.ResetContent => throw new NotImplementedException(),
                    HttpStatusCode.PartialContent => throw new NotImplementedException(),
                    HttpStatusCode.MultiStatus => throw new NotImplementedException(),
                    HttpStatusCode.AlreadyReported => throw new NotImplementedException(),
                    HttpStatusCode.IMUsed => throw new NotImplementedException(),
                    HttpStatusCode.Ambiguous => throw new NotImplementedException(),
                    HttpStatusCode.Moved => throw new NotImplementedException(),
                    HttpStatusCode.Found => throw new NotImplementedException(),
                    HttpStatusCode.RedirectMethod => throw new NotImplementedException(),
                    HttpStatusCode.NotModified => throw new NotImplementedException(),
                    HttpStatusCode.UseProxy => throw new NotImplementedException(),
                    HttpStatusCode.Unused => throw new NotImplementedException(),
                    HttpStatusCode.RedirectKeepVerb => throw new NotImplementedException(),
                    HttpStatusCode.PermanentRedirect => throw new NotImplementedException(),
                    HttpStatusCode.MethodNotAllowed => throw new NotImplementedException(),
                    HttpStatusCode.ProxyAuthenticationRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestTimeout => throw new NotImplementedException(),
                    HttpStatusCode.Conflict => throw new NotImplementedException(),
                    HttpStatusCode.Gone => throw new NotImplementedException(),
                    HttpStatusCode.LengthRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionFailed => throw new NotImplementedException(),
                    HttpStatusCode.RequestEntityTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.RequestUriTooLong => throw new NotImplementedException(),
                    HttpStatusCode.UnsupportedMediaType => throw new NotImplementedException(),
                    HttpStatusCode.RequestedRangeNotSatisfiable => throw new NotImplementedException(),
                    HttpStatusCode.ExpectationFailed => throw new NotImplementedException(),
                    HttpStatusCode.MisdirectedRequest => throw new NotImplementedException(),
                    HttpStatusCode.UnprocessableEntity => throw new NotImplementedException(),
                    HttpStatusCode.Locked => throw new NotImplementedException(),
                    HttpStatusCode.FailedDependency => throw new NotImplementedException(),
                    HttpStatusCode.UpgradeRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestHeaderFieldsTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.UnavailableForLegalReasons => throw new NotImplementedException(),
                    HttpStatusCode.ServiceUnavailable => throw new NotImplementedException(),
                    HttpStatusCode.GatewayTimeout => throw new NotImplementedException(),
                    HttpStatusCode.HttpVersionNotSupported => throw new NotImplementedException(),
                    HttpStatusCode.VariantAlsoNegotiates => throw new NotImplementedException(),
                    HttpStatusCode.InsufficientStorage => throw new NotImplementedException(),
                    HttpStatusCode.LoopDetected => throw new NotImplementedException(),
                    HttpStatusCode.NotExtended => throw new NotImplementedException(),
                    HttpStatusCode.NetworkAuthenticationRequired => throw new NotImplementedException(),
                    _ => "Unexpected Error"
                },
                Status = (int)statusCode,
                Detail = statusCode switch
                {
                    HttpStatusCode.BadRequest => "Invalid or missing parameters.",
                    HttpStatusCode.Unauthorized => "Missing or invalid authentication credentials.",
                    HttpStatusCode.PaymentRequired => "A valid subscription is required to access this resource.",
                    HttpStatusCode.Forbidden => "You do not have permission to access this resource.",
                    HttpStatusCode.NotFound => "No property found for the specified propertyCode.",
                    HttpStatusCode.NotAcceptable => "The requested content type is not supported.",
                    HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again after the retry-after period.",
                    HttpStatusCode.InternalServerError => "An unexpected error occurred on the server.",
                    HttpStatusCode.NotImplemented => "The requested functionality is not supported.",
                    HttpStatusCode.BadGateway => "Invalid response from an upstream server.",
                    HttpStatusCode.Continue => throw new NotImplementedException(),
                    HttpStatusCode.SwitchingProtocols => throw new NotImplementedException(),
                    HttpStatusCode.Processing => throw new NotImplementedException(),
                    HttpStatusCode.EarlyHints => throw new NotImplementedException(),
                    HttpStatusCode.OK => throw new NotImplementedException(),
                    HttpStatusCode.Created => throw new NotImplementedException(),
                    HttpStatusCode.Accepted => throw new NotImplementedException(),
                    HttpStatusCode.NonAuthoritativeInformation => throw new NotImplementedException(),
                    HttpStatusCode.NoContent => throw new NotImplementedException(),
                    HttpStatusCode.ResetContent => throw new NotImplementedException(),
                    HttpStatusCode.PartialContent => throw new NotImplementedException(),
                    HttpStatusCode.MultiStatus => throw new NotImplementedException(),
                    HttpStatusCode.AlreadyReported => throw new NotImplementedException(),
                    HttpStatusCode.IMUsed => throw new NotImplementedException(),
                    HttpStatusCode.Ambiguous => throw new NotImplementedException(),
                    HttpStatusCode.Moved => throw new NotImplementedException(),
                    HttpStatusCode.Found => throw new NotImplementedException(),
                    HttpStatusCode.RedirectMethod => throw new NotImplementedException(),
                    HttpStatusCode.NotModified => throw new NotImplementedException(),
                    HttpStatusCode.UseProxy => throw new NotImplementedException(),
                    HttpStatusCode.Unused => throw new NotImplementedException(),
                    HttpStatusCode.RedirectKeepVerb => throw new NotImplementedException(),
                    HttpStatusCode.PermanentRedirect => throw new NotImplementedException(),
                    HttpStatusCode.MethodNotAllowed => throw new NotImplementedException(),
                    HttpStatusCode.ProxyAuthenticationRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestTimeout => throw new NotImplementedException(),
                    HttpStatusCode.Conflict => throw new NotImplementedException(),
                    HttpStatusCode.Gone => throw new NotImplementedException(),
                    HttpStatusCode.LengthRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionFailed => throw new NotImplementedException(),
                    HttpStatusCode.RequestEntityTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.RequestUriTooLong => throw new NotImplementedException(),
                    HttpStatusCode.UnsupportedMediaType => throw new NotImplementedException(),
                    HttpStatusCode.RequestedRangeNotSatisfiable => throw new NotImplementedException(),
                    HttpStatusCode.ExpectationFailed => throw new NotImplementedException(),
                    HttpStatusCode.MisdirectedRequest => throw new NotImplementedException(),
                    HttpStatusCode.UnprocessableEntity => throw new NotImplementedException(),
                    HttpStatusCode.Locked => throw new NotImplementedException(),
                    HttpStatusCode.FailedDependency => throw new NotImplementedException(),
                    HttpStatusCode.UpgradeRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestHeaderFieldsTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.UnavailableForLegalReasons => throw new NotImplementedException(),
                    HttpStatusCode.ServiceUnavailable => throw new NotImplementedException(),
                    HttpStatusCode.GatewayTimeout => throw new NotImplementedException(),
                    HttpStatusCode.HttpVersionNotSupported => throw new NotImplementedException(),
                    HttpStatusCode.VariantAlsoNegotiates => throw new NotImplementedException(),
                    HttpStatusCode.InsufficientStorage => throw new NotImplementedException(),
                    HttpStatusCode.LoopDetected => throw new NotImplementedException(),
                    HttpStatusCode.NotExtended => throw new NotImplementedException(),
                    HttpStatusCode.NetworkAuthenticationRequired => throw new NotImplementedException(),
                    _ => "An unexpected error occurred."
                },
                Instance = $"https://api.hgv.com/errors/{correlationId}"
            };
        }
    }
}
