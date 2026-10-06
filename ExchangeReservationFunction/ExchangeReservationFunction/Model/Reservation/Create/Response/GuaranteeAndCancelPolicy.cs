using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExchangeReservationFunction.Model.Reservation.Create.Response
{
    public class GuaranteeAndCancelPolicy
    {
        [JsonPropertyName("guaranteeDescription")] public string GuaranteeDescription { get; set; }
        [JsonPropertyName("guaranteeType")] public string GuaranteeType { get; set; }
        [JsonPropertyName("cancelPenalties")] public List<CancelPenalty> CancelPenalties { get; set; } = [];
    }
}
