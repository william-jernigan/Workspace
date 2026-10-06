using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeOrder
{
    [ExcludeFromCodeCoverage]
    public partial class OrderReturn : BaseEntity
    {
        public Guid TenantId { get; set; }

        public Guid OrderId { get; set; }

        public string ReturnNumber { get; set; } = null!;

        public DateTime ReturnDate { get; set; }

        public string? SalesPersonEmail { get; set; }

        public string? SalesPersonFirstName { get; set; }

        public string? SalesPersonLastName { get; set; }

        public string? ExternalSourceSystem { get; set; }


        public string? ExternalReferenceId { get; set; }

        public string? ReturnFeeCurrency { get; set; }

        public decimal? ReturnFeeAmount { get; set; }

        public bool WaiveFee { get; set; }

        public string? Memo { get; set; }

        public virtual Order Order { get; set; } = null!;

        public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = [];
    }
}
