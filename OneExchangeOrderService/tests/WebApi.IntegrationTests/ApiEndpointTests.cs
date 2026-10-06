using Microsoft.AspNetCore.Mvc.Testing;
using System.Data;

namespace WebApi.IntegrationTests
{
    [TestClass]
    public partial class ApiEndpointTests
    {
        private readonly HttpClient _httpClient;

        public ApiEndpointTests()
        {
            var factory = new WebApplicationFactory<Program>();
            _httpClient = factory.CreateClient();
        }

        [TestMethod]
        [DataRow("/health")]
        public async Task HealthReturnsSuccessStatusCode(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
    }
}
