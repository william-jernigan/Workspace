using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.PostOrderModels
{
    [ExcludeFromCodeCoverage]
    public class ExternalOrderReferenceRequest
    {
        public string? ExternalSourceSystem { get; set; }
        public string? ExternalReferenceId { get; set; }
    }
}
