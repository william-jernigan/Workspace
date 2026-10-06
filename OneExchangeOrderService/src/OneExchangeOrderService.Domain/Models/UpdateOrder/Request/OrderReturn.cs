
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.UpdateOrder.Request
{
    [ExcludeFromCodeCoverage]
    public class OrderReturn
    {
        public string ReturnNumber { get; set; }
        public DateTime ReturnDate { get; set; }
        public string SalesPersonProfile { get; set; }
        public string ReturnFeeCurrency { get; set; }
        public decimal ReturnFeeAmount { get; set; }
        public string Memo { get; set; }
        public string ExternalSourceSystem { get; set; }
        public string ExternalReferenceId { get; set; }
        public bool WaiveFee { get; set; }
        public string SalesPersonFirstName { get; set; }
        public string SalesPersonLastName { get; set; }
        public string SalesPersonEmail { get; set; }
    }
}
