namespace OneExchangeOrderService.Domain.Entities.Common
{
    public class BaseEntity
    {
        public Guid Id { get; set; }
        public long ClustRowId { get; set; }

        public DateTimeOffset InsertedDateTime { get; set; }

        public string InsertedBy { get; set; } = null!;

        public DateTimeOffset? UpdatedDateTime { get; set; }

        public string? UpdatedBy { get; set; }

        public bool Status { get; set; }

        public string? StatusReason { get; set; }
    }
}
