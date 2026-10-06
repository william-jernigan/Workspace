

using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain
{
    [ExcludeFromCodeCoverage]
    public class TransactionHistoryRequest
    {
        public required string CustomerExternalReferenceId { get; set; }


    }

}
