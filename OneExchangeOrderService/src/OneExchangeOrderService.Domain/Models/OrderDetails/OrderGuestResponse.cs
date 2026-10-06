using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.OrderDetails
{
    [ExcludeFromCodeCoverage]
    public class OrderGuestResponse : BaseResponse
    {
        public Guid TenantId { get; set; }

        public Guid OrderId { get; set; }

        public Guid ProfileId { get; set; }

        public bool IsPrimary { get; set; }

        public virtual OrderDataResponse Order { get; set; } = null!;
    }
}
