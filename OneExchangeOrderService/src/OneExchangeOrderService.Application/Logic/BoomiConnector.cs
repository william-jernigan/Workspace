using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneExchangeOrderService.Application.Interfaces.Logic;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Models.V2.Cancellation.Request;
using OneExchangeOrderService.Domain.Models.V2.Cancellation.Response;
using OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request;
using OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Response;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OneExchangeOrderService.Application.Logic
{
    public class BoomiConnector(IHttpClientFactory httpClientFactory, ILogger<BoomiConnector> logger, IConfiguration config) : IBoomiConnector
    {
        //TODO, Please change the URL and URI(in code and appsetting) along with Credentials for actual Reservation Connector
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient(OneExchangeOrderServiceConstants.BoomiConnectorClient);
        private readonly ILogger<BoomiConnector> _logger = logger;
        private readonly string _baseUrl = config["BoomiConnectorApi:BaseUrl"]!;
        private readonly Dictionary<string, string> _apiKeyMap = config.GetSection("BoomiConnectorApi:ApiKeys").Get<Dictionary<string, string>>() ?? [];

        public async Task<ReservationConnectorResponse> ReservationConnectorCreateAsync(ReservationConnectorRequest request) => await PostAsync<ReservationConnectorRequest, ReservationConnectorResponse, ReservationConnectorResponse>(
                OneExchangeOrderServiceConstants.ReservationConnectorCreateEndpointUri, OneExchangeOrderServiceConstants.ReservationApiKeyName,
                request,
                res => res,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });

        public async Task<CancellationResponse> CancellationConnectorCancelAsync(CancellationRequest request)
           => await PostAsync<CancellationRequest, CancellationResponse, CancellationResponse>(
               OneExchangeOrderServiceConstants.CancellationConnectorCancelEndpointUri,
               OneExchangeOrderServiceConstants.CancellationCancelApiKeyName,
               request,
               res => res,
               new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });

        private async Task<TOut> PostAsync<TIn, TWrapped, TOut>(
            string relativePath, string apiKeyName,
            TIn request,
            Func<TWrapped, TOut> map,
            JsonSerializerOptions? serializerOptions = null)
        {
            serializerOptions ??= new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var fullUrl = new Uri(new Uri(_baseUrl), relativePath);
            var json = JsonSerializer.Serialize(request, serializerOptions);

            _logger.LogInformation("Sending POST to {Url}:\n{Payload}", fullUrl, json);

            using var message = new HttpRequestMessage(HttpMethod.Post, fullUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Add dynamic x-api-key if mapped
            if (_apiKeyMap.TryGetValue(apiKeyName, out var apiKey))
            {
                message.Headers.Remove("x-api-key");
                message.Headers.Add("x-api-key", apiKey);
            }
            else
            {
                _logger.LogWarning("No API key configured for path: {Path}", apiKeyName);
            }

            var response = await _httpClient.SendAsync(message);
            var rawContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Response from {Url}:\n{Payload}", fullUrl, rawContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Boomi API call to {Url} failed: {StatusCode}", fullUrl, response.StatusCode);
                throw new HttpRequestException($"Boomi call to {relativePath} failed: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<TWrapped>();
            return map(result!);
        }
    }

}
