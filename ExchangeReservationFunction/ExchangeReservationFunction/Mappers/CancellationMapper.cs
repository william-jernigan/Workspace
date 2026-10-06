using ExchangeReservationFunction.Common;
using ExchangeReservationFunction.Model.Cancellation;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace ExchangeReservationFunction.Mappers
{
    public static class CancellationMapper
    {
        public static ONEXCancelPostRequest MapOtaToOnexRequest(OTACancelPostRequest otaRequest, string reservationId)
        {
            if (otaRequest == null)
            {
                return null;
            }

            var sourceRes = otaRequest.Reservations?.SourceReservationIds?.FirstOrDefault();
            return new ONEXCancelPostRequest
            {
                Id = otaRequest.CorrelationID ?? Guid.NewGuid().ToString(),
                Source = "booking-system",
                Specversion = ApiConstants.SpecVersion,
                Type = "booking.cancelled",
                Datacontenttype = ApiConstants.DataContentType,
                Dataschema = "https://example.com/schemas/booking-cancellation",
                Subject = "booking/evt-12345",
                Time = DateTimeOffset.UtcNow,
                Ispartial = false,
                Data = new CancelItemRequest
                {
                    SystemMeta = new SystemMeta
                    {
                        TenantID = "HGV",
                        InsertedDateTime = DateTimeOffset.UtcNow,
                        InsertedBy = ApiConstants.Api,
                        UpdatedDateTime = DateTimeOffset.UtcNow,
                        UpdatedBy = ApiConstants.Api,
                        Status = true,
                        StatusReason = "Cancellation initiated"
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = sourceRes.SourceSystem ?? string.Empty,
                        TargetSystem = "CLA",
                        SourceReferenceId = sourceRes?.Id ?? string.Empty,
                        TargetReferenceId = "CLACancel"
                    },
                    BookingId = reservationId,
                    CancellationReason = otaRequest.CancellationInfo?.CancelReason ?? string.Empty,
                    CancellationDate = otaRequest.CancellationInfo != null
                        ? new DateTimeOffset(otaRequest.CancellationInfo.CancelDate)
                        : DateTimeOffset.UtcNow,
                    RefundedAmount = 0,
                    CancellationStatus = string.Empty,
                    BookingItems =
                    [
                        new BookingItem
                        {
                            ItemCode = string.Empty,
                            ItemClass = string.Empty,
                            ParentCode = string.Empty,
                            Name = string.Empty,
                            Description = string.Empty,
                            ItemAttributes = [],
                            MemberID = string.Empty,
                            StartDate = DateTimeOffset.UtcNow,
                            EndDate = DateTimeOffset.UtcNow,
                            WeekNumber = 0,
                            Action = string.Empty,
                            GuestCounts = new GuestCounts
                            {
                                Adults = 0,
                                Children = 0
                            },
                            IsOwnerBooking = false,
                            GuestInfo = new GuestInfo
                            {
                                FirstName = string.Empty,
                                LastName = string.Empty,
                                PostalAddresses =
                                [
                                    new PostalAddress
                                    {
                                        Sequence = 0,
                                        AddressType = string.Empty,
                                        Line1 = string.Empty,
                                        Line2 = string.Empty,
                                        City = string.Empty,
                                        State = string.Empty,
                                        PostalCode = string.Empty,
                                        Country = string.Empty
                                    }
                                    ],
                                PhoneNumbers =
                                [
                                    new PhoneNumber
                                    {
                                        Sequence = 0,
                                        PhoneType = string.Empty,
                                        Number = string.Empty
                                    }
                                    ],
                                DigitalAddresses =
                                [
                                    new DigitalAddress
                                    {
                                        Sequence = 0,
                                        DigitalAddressType = string.Empty,
                                        DigitalAddressValue = string.Empty
                                    }
                                    ]
                            },
                            PriceLevel = new PriceLevel
                            {
                                Status = false,
                                PriceLevelCode = string.Empty,
                                Currency = string.Empty,
                                TotalPrice = 0,
                                PriceAttributes = []
                            },
                            BookingAttributes = [],
                            BookingComments = []
                        }
                    ]
                }
            };
        }

        public static OTACancelPostResponse MapOnexToOtaResponse(ONEXCancelPostResponse onexResponse, OTACancelPostRequest otaRequest, string reservationId)
        {
            if (onexResponse == null)
            {
                return null;
            }

            var isSuccess = onexResponse.Data?.SystemMeta?.Status ?? false;
            var correlationId = onexResponse.Id ?? Guid.NewGuid().ToString();
            var tradingAgreementCode = otaRequest.TradingAgreementCode ?? ApiConstants.TradingAgreementCode;
            var cancelReason = onexResponse.Data?.CancellationReason
                                  ?? otaRequest.CancellationInfo?.CancelReason
                                  ?? string.Empty;
            var cancelDate = onexResponse.Data?.CancellationDate.UtcDateTime ?? DateTime.UtcNow;

            var otaResponse = new OTACancelPostResponse
            {
                CorrelationID = correlationId,
                TradingAgreementCode = tradingAgreementCode,
                CancellationInfo = new ResponseCommonCancellationInfo
                {
                    CancelNumber = onexResponse?.Data?.Error == null ? reservationId : null,
                    CancelDate = cancelDate,
                    CancelReason = cancelReason,
                    Status = isSuccess ? ApiConstants.CancelledStatus : ApiConstants.NotCancelledStatus
                },
                Message = isSuccess
                        ? ApiConstants.SuccessMessage
                        : ApiConstants.FailureMessage
            };

            if (isSuccess)
            {
                otaResponse.Reservations = new ResponseReservationDetails
                {
                    SourceReservationIds = []
                };

                if (otaRequest?.Reservations?.SourceReservationIds != null)
                {
                    foreach (var src in otaRequest.Reservations.SourceReservationIds)
                    {
                        otaResponse.Reservations.SourceReservationIds.Add(new ResponseSourceReservationId
                        {
                            SourceSystem = src.SourceSystem ?? string.Empty,
                            Id = src.Id ?? string.Empty
                        });
                    }
                }
            }
            else
            {
                var errorCode = onexResponse.Data?.Error?.ErrorCode ?? ApiConstants.NotCancelledStatus;
                var errorType = !string.IsNullOrWhiteSpace(onexResponse.Data?.Error?.ErrorType)
                    ? onexResponse.Data.Error.ErrorType
                    : "https://api.hgv.com/errors/warning";
                otaResponse.Errors =
                [
                    new FailureError
                    {
                        Type = errorType,
                        Code = errorCode,
                        Status = 200,
                        ShortText = onexResponse.Data?.Error?.ErrorMessage
                            ?? ApiConstants.ErrorMessage
                    }
                ];
            }

            return otaResponse;
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
                    HttpStatusCode.NotFound => "No reservations found matching the specified criteria.",
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
