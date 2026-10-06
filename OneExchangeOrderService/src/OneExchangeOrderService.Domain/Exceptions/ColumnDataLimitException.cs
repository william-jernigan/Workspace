
namespace OneExchangeOrderService.Domain.Exceptions
{
    public class ColumnDataLimitException(string businessMessage) : Exception(businessMessage)
    {
    }
}
