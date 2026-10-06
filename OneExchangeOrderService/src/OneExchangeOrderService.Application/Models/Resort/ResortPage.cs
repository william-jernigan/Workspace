
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.Application.Models.Resort
{
    public class ResortPage
    {

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("overviewSectionContent")]
        public Overview OverviewSectionContent { get; set; }


        [JsonPropertyName("overviewSectionAlerts")]
        public AlertsContentSection OverviewSectionAlerts { get; set; }

        [JsonPropertyName("unitTypes")]
        public List<UnitType>? UnitTypes { get; set; }

    }

}