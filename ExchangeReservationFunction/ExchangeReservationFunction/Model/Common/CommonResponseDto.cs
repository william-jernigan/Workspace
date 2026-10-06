namespace ExchangeReservationFunction.Model.Common
{
    public class CommonResponseDto<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public OTAErrorResponse? Error { get; set; }
    }

    public class OTAErrorResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
    }

}
