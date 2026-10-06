using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Create.Request
{
    public class GuestInfo
    {
        [JsonPropertyName("firstName")] public string FirstName { get; set; } = default!;
        [JsonPropertyName("lastName")] public string LastName { get; set; } = default!;

        [JsonPropertyName("postalAddresses")] public List<PostalAddress> PostalAddresses { get; set; }
        [JsonPropertyName("phoneNumbers")] public List<PhoneNumber> PhoneNumbers { get; set; }
        [JsonPropertyName("digitalAddresses")] public List<DigitalAddress> DigitalAddresses { get; set; }
    }
}
