
namespace OneExchangeOrderService.Domain.Exceptions
{
    public class LogicException(string businessMessage) : Exception(businessMessage)
    {
    }
}
