using System.Text.Json.Serialization;

namespace APITransformationsFunction.Model.Reservation.Create.Response
{
    public class ErrorInfo
    {
        [JsonPropertyName("errorCode")] public string ErrorCode { get; set; }
        [JsonPropertyName("errorType")] public string ErrorType { get; set; }
        [JsonPropertyName("errorMessage")] public string ErrorMessage { get; set; }
    }
}
