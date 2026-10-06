using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.UpdateOrder.Request
{
    [ExcludeFromCodeCoverage]
    public class ExternalOrderReference
    {
        public string ExternalSourceSystem { get; set; }
        public string ExternalReferenceId { get; set; }
    }
}
