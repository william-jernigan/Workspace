using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation;
using System.Text;
using System.Text.Json;

namespace OneExchangeOrderService.Application.Eventhub
{
    public class OrderCancelEventPublisher : IDisposable
    {
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ILogger<OrderReservationEventPublisher> _logger;
        private bool _disposed;
        private static readonly JsonSerializerOptions IndentedJsonOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public OrderCancelEventPublisher(IConfiguration config, ILogger<OrderReservationEventPublisher> logger)
        {
            var fullyQualifiedNamespace = config["OrderReservationEventHub:EventHubNamespace"];
            var eventHubName = config["OrderReservationEventHub:CancellationRequestEventhubName"];
            _eventHubProducerClient = new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, new DefaultAzureCredential());

            _logger = logger;
        }

        /// <summary>
        /// To public order details, (need to cancle).
        /// </summary>
        /// <param name="orderEvent"></param>
        /// <returns></returns>
        public async Task PublishOrderAsync(CancelOrderEvent orderEvent)
        {
            var json = JsonSerializer.Serialize(orderEvent, IndentedJsonOptions);
            _logger.LogInformation("EventHub Payload:\n{Json}", json);

            using var eventBatch = await _eventHubProducerClient.CreateBatchAsync();
            eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json)));

            await _eventHubProducerClient.SendAsync(eventBatch);
            _logger.LogInformation("Published Cancel Order-Requested event to Event Hub.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                _eventHubProducerClient?.DisposeAsync().AsTask().GetAwaiter().GetResult(); // Since DisposeAsync returns ValueTask
            }
            _disposed = true;
        }
    }
}
