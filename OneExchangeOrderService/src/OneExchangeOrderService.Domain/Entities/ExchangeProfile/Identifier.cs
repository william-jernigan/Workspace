using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeProfile
{
    [ExcludeFromCodeCoverage]
    public partial class Identifier : BaseEntity
    {

        public Guid ProfileId { get; set; }

        public int Sequence { get; set; }

        public string? IdentifierType { get; set; }

        public string? IdentifierSource { get; set; }

        public string? Identifier1 { get; set; }

        public string? IdentifierEncryption { get; set; }

        public byte[]? EncryptedIdentifier { get; set; }

        public virtual Profile Profile { get; set; } = null!;
    }
}
