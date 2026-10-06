using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeOrder
{
    [ExcludeFromCodeCoverage]
    public partial class Order : BaseEntity
    {

        public Guid TenantId { get; set; }

        public Guid TradingAgreementId { get; set; }

        public string OrderType { get; set; } = null!;

        public string OrderNumber { get; set; } = null!;

        public string OrderStatus { get; set; } = null!;

        public string? ExternalSourceSystem { get; set; }

        public string? ExternalReferenceId { get; set; }

        public DateTime? OrderDate { get; set; }

        public string? SalesPersonEmail { get; set; }

        public string? SalesPersonFirstName { get; set; }

        public string? SalesPersonLastName { get; set; }

        public Guid? BillToProfileId { get; set; }

        public string? CustomerExternalSourceSystem { get; set; }

        public string? CustomerExternalReferenceId { get; set; }

        public string? CustomerFirstName { get; set; }

        public string? CustomerLastName { get; set; }

        public string? CustomerPhoneNumber { get; set; }

        public string? CustomerEmailAddress { get; set; }

        public string? CustomerMemberLevel { get; set; }

        public DateTime? CancelDate { get; set; }

        public string Currency { get; set; } = null!;

        public decimal? Subtotal { get; set; }

        public string TaxCurrency { get; set; } = null!;

        public decimal? TaxAmount { get; set; }

        public string FeeCurrency { get; set; } = null!;

        public decimal OrderFee { get; set; }

        public bool WaiveFee { get; set; }

        public string? Memo { get; set; }
        public string? ExtendedReference { get; set; }
        public virtual ICollection<ExchangeTransactionLog> ExchangeTransactionLogs { get; set; } = [];

        public virtual ICollection<OrderGuest> OrderGuests { get; set; } = [];

        public virtual ICollection<OrderItem> OrderItems { get; set; } = [];

        public virtual ICollection<OrderPayment> OrderPayments { get; set; } = [];

        public virtual OrderReturn? OrderReturn { get; set; }

        public virtual ICollection<OrderRefund> OrderRefunds { get; set; } = [];
    }
}
