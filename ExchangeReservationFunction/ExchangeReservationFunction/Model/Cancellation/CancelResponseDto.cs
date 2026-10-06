using System.Diagnostics.CodeAnalysis;

namespace ExchangeReservationFunction.Model.Cancellation
{
    [ExcludeFromCodeCoverage]
    public class CancelResponseDto<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public OTAErrorResponse? Error { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class OTAErrorResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
    }
}

