using Microsoft.Extensions.Logging;
using OneExchangeOrderService.Application.Interfaces.Logic;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.DTO.Models.Availability.ChoiceInventory.Request;
using OneExchangeOrderService.DTO.Models.Availability.ChoiceInventory.Response;
using OneExchangeOrderService.DTO.Models.InventoryUpdate.Request;
using OneExchangeOrderService.DTO.Models.InventoryUpdate.Response;
using System.Net.Http.Json;
using System.Text.Json;

namespace OneExchangeOrderService.Application.Logic
{
    public class InventoryServiceClient(IHttpClientFactory httpClientFactory, ILogger<InventoryServiceClient> logger) : IInventoryServiceClient
    {
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient(OneExchangeOrderServiceConstants.InventoryServiceClient);
        private readonly ILogger<InventoryServiceClient> _logger = logger;
        private static readonly JsonSerializerOptions PascalCaseNoIndentOptions = new()
        {
            PropertyNamingPolicy = null,
            WriteIndented = false
        };

        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public async Task<HotelAvailabilityResponse?> CheckAvailabilityAsync(List<HotelAvailabilityRequest> requests)
        {
            var url = OneExchangeOrderServiceConstants.ChoicePropertyInventoryAvailabilityEndpointUri;
            _logger.LogInformation("Calling Inventory availability endpoint: {Url}", url);


            // Serialize request to JSON with PascalCase
            var jsonPayload = JsonSerializer.Serialize(requests, PascalCaseNoIndentOptions);
            if (!_httpClient.DefaultRequestHeaders.Contains("version"))
            {
                _httpClient.DefaultRequestHeaders.Add("version", "1");
            }

            // Log clean JSON request
            _logger.LogInformation("{Prefix} Calling Boomi Availability API with payload: {Payload}",
                OneExchangeOrderServiceConstants.InventoryServiceClientPrefix, jsonPayload);
            // var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120)); // Increase timeout to 60 seconds
            var response = await _httpClient.PostAsJsonAsync(url, requests);

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Inventory API response: {Response}", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("InventoryService returned status {StatusCode}", response.StatusCode);
                return null;
            }

            return JsonSerializer.Deserialize<HotelAvailabilityResponse>(content, CaseInsensitiveOptions);
        }

        public async Task<InventoryResponse> UpdateInventoryAsync(InventoryRequest request)
        {
            var response = new InventoryResponse
            {
                Success = false,
                Message = string.Empty
            };
            try
            {
                var url = OneExchangeOrderServiceConstants.InventoryEndpointUri;
                _logger.LogInformation("Calling Inventory Update Endpoint: {Url}", url);

                var jsonPayload = JsonSerializer.Serialize(request, PascalCaseNoIndentOptions);

                if (!_httpClient.DefaultRequestHeaders.Contains("version"))
                {
                    _httpClient.DefaultRequestHeaders.Add("version", "1");
                }

                _logger.LogInformation("{Prefix} Calling inventory Update with payload: {Payload}",
                    OneExchangeOrderServiceConstants.InventoryServiceClientPrefix, jsonPayload);

                using var httpResponse = await _httpClient.PostAsJsonAsync(url, request);
                var content = await httpResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("{Prefix} Inventory Update API response ({Status}): {Body}",
                    OneExchangeOrderServiceConstants.InventoryServiceClientPrefix, (int)httpResponse.StatusCode, content);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return new InventoryResponse
                    {
                        Success = false,
                        Message = $"Inventory Update failed with {(int)httpResponse.StatusCode} ({httpResponse.StatusCode}). Body: {content}"
                    };
                }

                var message = string.IsNullOrWhiteSpace(content)
                    ? "Inventory Updated successfully."
                    : content.Trim('\"');

                return new InventoryResponse
                {
                    Success = true,
                    Message = message
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("{Prefix} Inventory Update canceled.", OneExchangeOrderServiceConstants.InventoryServiceClientPrefix);
                return new InventoryResponse
                {
                    Success = false,
                    Message = "Inventory Update was cancelled."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Inventory Update failed: {Message}",
                    OneExchangeOrderServiceConstants.InventoryServiceClientPrefix, ex.Message);

                return new InventoryResponse
                {
                    Success = false,
                    Message = $"Inventory Update failed: {ex.Message}"
                };
            }
        }

    }
}
