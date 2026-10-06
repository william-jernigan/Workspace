namespace OneExchangeOrderService.Domain.Exceptions
{
    public class BoomiTimeoutException(string businessMessage) : Exception(businessMessage)
    {
    }
}
