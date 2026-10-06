using OneExchangeOrderService.Domain.Entities.Common;
using System.Diagnostics.CodeAnalysis;

namespace OneExchangeOrderService.Domain.Entities.ExchangeProfile
{
    [ExcludeFromCodeCoverage]
    public partial class Profile : BaseEntity
    {

        public string ProfileType { get; set; } = null!;

        public string? ExternalSourceSystem { get; set; }

        public string? ExternalReferenceId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public virtual ICollection<Address> Addresses { get; set; } = [];

        public virtual ICollection<DigitalAddress> DigitalAddresses { get; set; } = [];

        public virtual ICollection<Identifier> Identifiers { get; set; } = [];

        public virtual ICollection<Phone> Phones { get; set; } = [];
    }
}
