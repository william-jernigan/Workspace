using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class OrderRequest
    {
        public required string CustomerExternalReferenceId { get; set; }

        public string? OrderStatus { get; set; }

        public string? OrderDateFrom { get; set; }

        public string? OrderDateTo { get; set; }

        public int? Limit { get; set; }

        public int? Offset { get; set; }

        public string? Authorization { get; set; }

        public string? TenantId { get; set; }

        public string? CorrelationId { get; set; }

        public string? Appname { get; set; }

        public string? Version { get; set; }
        public bool IsUserDiamondPremierMember { get; set; }
        public string? FreeTextSearch { get; set; }
    }
}
