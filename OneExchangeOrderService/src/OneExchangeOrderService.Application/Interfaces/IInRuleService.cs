using OneExchangeOrderService.DTO.Response;

namespace OneExchangeOrderService.Application.Interfaces
{
    public interface IInRuleService
    {
        public Task<CancelFeeResponse> GetCancellationFee(DateTime? orderDate, DateTime? checkInDate, string membershipLevel, string? absoluteDeadline);
    }
}
