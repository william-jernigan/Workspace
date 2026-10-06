using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneExchangeOrderService.Application.Interfaces;
using OneExchangeOrderService.Domain.Models.Eventhub.OrderReservation;
using System.Text;
using System.Text.Json;

namespace OneExchangeOrderService.Application.Eventhub
{
    public class OrderReservationEventPublisher : IOrderReservationEventPublisher, IDisposable
    {
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ILogger<OrderReservationEventPublisher> _logger;
        private bool _disposed;
        private static readonly JsonSerializerOptions IndentedJsonOptions = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public OrderReservationEventPublisher(IConfiguration config, ILogger<OrderReservationEventPublisher> logger)
        {
            _logger = logger;
            var fullyQualifiedNamespace = config["OrderReservationEventHub:EventHubNamespace"];
            var eventHubName = config["OrderReservationEventHub:ReservationRequestEventhubName"];
            // Log exactly what namespace & hub we’re using (no secrets here)
            _logger.LogInformation(
                "EventHubs producer initializing. Namespace={Namespace} EventHub={EventHub}",
                fullyQualifiedNamespace, eventHubName);

            _eventHubProducerClient =
                new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, new DefaultAzureCredential());

            _logger.LogInformation(
                "EventHubs producer ready. Namespace={Namespace} EventHub={EventHub}",
                fullyQualifiedNamespace, eventHubName);
        }

        public async Task PublishOrderAsync(OrderRequestedEvent evt)
        {
            try
            {
                var json = JsonSerializer.Serialize(evt, IndentedJsonOptions);

                _logger.LogInformation("EventHub Payload:\n{Json}", json);

                // Log the target Event Hubs details up front
                _logger.LogInformation(
                    "EventHub target details from Publisher: Namespace={Namespace} EventHub={EventHub}",
                    _eventHubProducerClient.FullyQualifiedNamespace,
                    _eventHubProducerClient.EventHubName);

                using var eventBatch = await _eventHubProducerClient.CreateBatchAsync();
                if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json))))
                {
                    _logger.LogWarning("Failed to add event to batch. Event size may exceed the maximum allowed.");
                    return;
                }

                await _eventHubProducerClient.SendAsync(eventBatch);
                _logger.LogInformation("Published Order-Requested event to Event Hub.");
            }
            catch (Exception ex)
            {
                // This will log exception message and full stack trace
                _logger.LogError(ex,
                    "Failed to publish Order-Requested event to Event Hub. Exception: {ErrorMessage}",
                    ex.Message);

                // If you want to explicitly log stack trace string too
                _logger.LogError("StackTrace: {StackTrace}", ex.StackTrace);

                // Rethrow to let upstream handle it (optional)
                throw;
            }
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
