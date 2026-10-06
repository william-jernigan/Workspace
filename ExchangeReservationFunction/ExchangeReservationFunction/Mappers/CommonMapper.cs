using ExchangeReservationFunction.Model.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace ExchangeReservationFunction.Mappers
{
    public static class CommonMapper
    {
        public static OTAErrorResponse MapBackendError(ReservationError? error = null, HttpResponseMessage? backendResponse = null, HttpStatusCode defaultStatusCode = HttpStatusCode.BadRequest, string? context = null, List<string>? validationMessages = null)
        {
            // 1️. Determine instance ID
            var instanceId = !string.IsNullOrWhiteSpace(error?.ErrorInstance)
                ? error.ErrorInstance
                : Guid.NewGuid().ToString();

            // 2️. Determine the status code
            var effectiveStatusCode = defaultStatusCode;

            if (error != null)
            {
                // Case: error.ErrorCode is numeric (e.g., "404")
                if (int.TryParse(error.ErrorCode, out var numericCode) &&
                    Enum.IsDefined(typeof(HttpStatusCode), numericCode))
                {
                    effectiveStatusCode = (HttpStatusCode)numericCode;
                }
                // Case: error.ErrorCode is textual (e.g., "NotFound")
                else if (Enum.TryParse<HttpStatusCode>(error.ErrorCode, true, out var parsedStatus))
                {
                    effectiveStatusCode = parsedStatus;
                }
            }
            else if (backendResponse != null)
            {
                effectiveStatusCode = backendResponse.StatusCode;
            }

            // 3️. Build the error response
            return new OTAErrorResponse
            {
                Type = effectiveStatusCode switch
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
                Title = effectiveStatusCode switch
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
                Status = (int)effectiveStatusCode,
                Detail = effectiveStatusCode switch
                {
                    HttpStatusCode.BadRequest => validationMessages != null && validationMessages.Count != 0
                        ? $"Invalid or missing parameters - {string.Join("; ", validationMessages)}"
                        : "Invalid or missing parameters.",
                    HttpStatusCode.Unauthorized => "Missing or invalid authentication credentials.",
                    HttpStatusCode.PaymentRequired => "A valid subscription is required to access this resource.",
                    HttpStatusCode.Forbidden => "You do not have permission to access this resource.",
                    // HttpStatusCode.NotFound => "No member profile found for the specified memberId.",
                    HttpStatusCode.NotFound => error?.ErrorMessage
                        ?? context?.ToLower(System.Globalization.CultureInfo.CurrentCulture) switch
                        {
                            "member" => "No member profile found for the specified memberId.",
                            "reservation" => "No reservations found matching the specified criteria.",
                            "property" => "No property found for the specified propertyCode.",
                            _ => "The requested resource was not found."
                        },
                    HttpStatusCode.NotAcceptable => "The requested content type is not supported.",
                    HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again after the retry-after period.",
                    HttpStatusCode.InternalServerError => validationMessages != null && validationMessages.Count != 0
                        ? $"An unexpected error occurred on the server - {string.Join("; ", validationMessages)}"
                        : "An unexpected error occurred on the server.",
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
                Instance = $"https://api.hgv.com/errors/{instanceId}"
            };
        }
    }
}
