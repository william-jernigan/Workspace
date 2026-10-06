using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Models.PostOrderModels
{
    [ExcludeFromCodeCoverage]
    public class PhoneRequest
    {
        public int? Sequence { get; set; }
        public string? PhoneType { get; set; }
        public string? PhonePurpose { get; set; }
        public string? Number { get; set; }
        public string? Extension { get; set; }
        public DateTime? VerifiedDate { get; set; }
    }
}
