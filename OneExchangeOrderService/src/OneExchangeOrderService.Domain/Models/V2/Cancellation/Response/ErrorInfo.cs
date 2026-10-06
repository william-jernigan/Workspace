namespace OneExchangeOrderService.Domain.Models.V2.Cancellation.Response
{
    public class ErrorInfo
    {
        public string? ErrorCode { get; set; }
        public string? ErrorType { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
