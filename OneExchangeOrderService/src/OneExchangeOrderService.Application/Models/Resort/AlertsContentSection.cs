using Newtonsoft.Json;
namespace OneExchangeOrderService.Application.Models.Resort
{
    public class AlertsContentSection
    {
        [JsonProperty("alerts")]
        public List<AlertComponent>? Alerts { get; set; }
    }
}
