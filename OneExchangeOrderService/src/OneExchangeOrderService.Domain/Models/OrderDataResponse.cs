using OneExchangeOrderService.Domain.Models.OrderDetails;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class OrderDataResponse : BaseResponse
    {

        public Guid TenantId { get; set; }
        public Guid TradingAgreementId { get; set; }
        public string OrderType { get; set; } = null!;
        public string OrderNumber { get; set; } = null!;
        public string OrderStatus { get; set; } = null!;
        public DateTime? OrderDate { get; set; }
        public DateTime? CancelDate { get; set; }
        public string? Memo { get; set; }
        public string? ExtendedReference { get; set; }
        public string? ExternalSourceSystem { get; set; }
        public string? ExternalReferenceId { get; set; }
        public string? CustomerExternalSourceSystem { get; set; }
        public string? CustomerExternalReferenceId { get; set; }
        public string? SalesPersonEmail { get; set; }
        public string? SalesPersonFirstName { get; set; }
        public string? SalesPersonLastName { get; set; }
        public Guid? BillToProfileId { get; set; }
        public string? CustomerPhoneNumber { get; set; }
        public string? CustomerEmailAddress { get; set; }
        public string? CustomerMemberLevel { get; set; }
        public string Currency { get; set; } = null!;
        public decimal? Subtotal { get; set; }
        public string TaxCurrency { get; set; } = null!;
        public decimal? TaxAmount { get; set; }
        public string FeeCurrency { get; set; } = null!;
        public decimal OrderFee { get; set; }

        public bool WaiveFee { get; set; }

        public List<OrderItemResponse> OrderItems { get; set; } = [];
        public List<ExchangeTransactionLogResponse> ExchangeTransactions { get; set; } = [];
        public List<OrderGuestResponse> OrderGuests { get; set; } = [];
        public List<OrderPaymentResponse> OrderPayments { get; set; } = [];

        public OrderReturnResponse? OrderReturn { get; set; }
        public List<OrderRefundResponse> OrderRefunds { get; set; } = [];
    }
}
