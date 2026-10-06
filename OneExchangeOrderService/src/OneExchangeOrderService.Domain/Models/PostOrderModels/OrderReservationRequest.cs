using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.PostOrderModels
{
    [ExcludeFromCodeCoverage]
    public class OrderReservationRequest
    {
        public string? TradingAgreementCode { get; set; }
        public string? OrderType { get; set; }
        public string? Currency { get; set; }
        public decimal? Subtotal { get; set; }
        public string? TaxCurrency { get; set; }
        public decimal? TaxAmount { get; set; }
        public string? FeeCurrency { get; set; }
        public decimal OrderFee { get; set; }
        public List<CurrencyEquivalent>? CurrencyEquivalent { get; set; }
        public string? Memo { get; set; }
        public ExternalOrderReferenceRequest? ExternalOrderReference { get; set; }
        public ProfileRequest? CustomerProfile { get; set; }
        public ProfileRequest? SalesPersonProfile { get; set; }
        public List<ProfileRequest>? Guests { get; set; }
        public List<OrderItemRequest>? OrderItems { get; set; }

        public List<OrderPaymentRequest> Payments { get; set; } = [];
        public bool WaiveFee { get; set; }


    }
}
