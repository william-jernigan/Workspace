using Contentful.Core.Models;
using Newtonsoft.Json;

namespace OneExchangeOrderService.Application.Models.Resort
{
    public class AlertComponent
    {
        [JsonProperty("text")]
        public Document Text { get; set; }
    }
}
