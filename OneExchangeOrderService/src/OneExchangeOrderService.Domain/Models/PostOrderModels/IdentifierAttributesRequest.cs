using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.PostOrderModels
{
    [ExcludeFromCodeCoverage]
    public class IdentifierAttributesRequest
    {
        public string? IdentifierType { get; set; }
        public string? IdentifierSource { get; set; }
        public string? Identifier { get; set; }
    }
}
