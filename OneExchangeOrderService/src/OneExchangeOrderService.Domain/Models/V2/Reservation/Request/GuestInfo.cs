using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Domain.Models.V2.Reservation.Request
{
    [ExcludeFromCodeCoverage]
    public class GuestInfo
    {
        [JsonPropertyName("firstName")] public string FirstName { get; set; } = default!;
        [JsonPropertyName("lastName")] public string LastName { get; set; } = default!;

        [JsonPropertyName("postalAddresses")] public List<PostalAddress>? PostalAddresses { get; set; }
        [JsonPropertyName("phoneNumbers")] public List<PhoneNumber>? PhoneNumbers { get; set; }
        [JsonPropertyName("digitalAddresses")] public List<DigitalAddress>? DigitalAddresses { get; set; }
    }
}