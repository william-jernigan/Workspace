using APITransformationsFunction.Common;
using APITransformationsFunction.Model.Property;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace APITransformationsFunctionTests.ServiceTests
{
    [TestClass]
    public class PropertyServiceTests
    {
        private Mock<ILogger<PropertyService>>? _mockLogger;
        private PropertyService? _propertyService;
        private Mock<HttpMessageHandler>? _mockHttpHandler;
        private HttpClient? _httpClient;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<PropertyService>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_mockHttpHandler.Object)
            {
                BaseAddress = new Uri("https://api.dev.onegrandvacation.com")
            };
            _propertyService = new PropertyService(_httpClient, _mockLogger.Object);
        }

        #region POST Tests

        [TestMethod]
        public async Task GetPropertiesAsync_ReturnsSuccess_WhenBackendReturnsValidData()
        {
            // Arrange
            var otaRequest = new OTAPropertySearchRequest
            {
                TradingAgreementCode = "HGV-ARV-HGV",
                CorrelationID = Guid.NewGuid().ToString()
            };

            var resort = new Item
            {
                ItemId = "1",
                ItemCode = "PROP001",
                ItemClass = ApiConstants.ItemClassResort,
                Name = "Hilton Resort",
                Description = "Luxury resort property",
                ParentCode = "",
                ItemExternalStatus = "Published",

                Profile = new Profile
                {
                    Addresses =
            [
                new Profile.Address
                {
                    AddressType = "PRIMARY",
                    AddressPurpose = "BUSINESS",
                    Line1 = "123 Beach Road",
                    City = "Goa",
                    State = "GA",
                    StateCode = "GA",
                    PostalCode = "403001",
                    Country = "India",
                    CountryCode = "IN"
                }
            ],
                    PhoneNumbers = [],
                    DigitalAddresses = []
                },

                ItemAttributes = [],
                SupplementalAttributes = new SupplementalAttributes(),

                Media = [],
                GeoLocation = new GeoLocationItem
                {
                    Type = "Point",
                    Coordinates = [15.2993, 74.1240]
                }
            };

            var unit = new Item
            {
                ItemId = "2",
                ItemCode = "UNIT001",
                ItemClass = ApiConstants.ItemClassUnit,
                Name = "Ocean View",
                Description = "Unit with ocean view",
                ParentCode = "PROP001",
                ItemExternalStatus = "Published",

                Profile = new Profile
                {
                    Addresses = [],
                    PhoneNumbers = [],
                    DigitalAddresses = []
                },

                ItemAttributes = [],
                SupplementalAttributes = new SupplementalAttributes(),

                Media = [],
                GeoLocation = new GeoLocationItem
                {
                    Type = "Point",
                    Coordinates = []
                }
            };

            var backendResponse = new
            {
                items = new List<Item> { resort, unit },
                pageSize = 10,
                currentPage = 1,
                totalItems = 2,
                totalPages = 1
            };

            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _propertyService!.GetPropertiesAsync(otaRequest, 1, 10);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data);
            Assert.IsNotNull(result.Data.Properties);

            Assert.AreEqual(1, result.Data.Properties.Count, "One resort expected");

            var mappedResort = result.Data.Properties.First();
            Assert.AreEqual("Hilton Resort", mappedResort.BasicPropertyInfoSummary.Name);
            Assert.AreEqual("PROP001", mappedResort.BasicPropertyInfoSummary.PropertyCode);

            Assert.IsNotNull(mappedResort.UnitDetails);
            Assert.AreEqual(1, mappedResort.UnitDetails.Count);
            Assert.AreEqual("Ocean View", mappedResort.UnitDetails.First().Description.Name);

            Assert.AreEqual("HGV-ARV-HGV", result.Data.TradingAgreementCode);
            Assert.AreEqual(10, result.Data.Pagination.PageSize);
            Assert.AreEqual(1, result.Data.Pagination.Page);
            Assert.AreEqual(2, result.Data.Pagination.TotalItems);
        }


        [TestMethod]
        public async Task GetPropertiesAsync_ReturnsError_WhenValidationFails()
        {
            // Arrange
            var otaRequest = new OTAPropertySearchRequest
            {
                TradingAgreementCode = "",
                CorrelationID = ""         // missing
            };

            // Act
            var result = await _propertyService!.GetPropertiesAsync(otaRequest, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task GetPropertiesAsync_ReturnsError_WhenBackendFails()
        {
            // Arrange
            var otaRequest = new OTAPropertySearchRequest
            {
                TradingAgreementCode = "HGV-HGV-ARV",
                CorrelationID = Guid.NewGuid().ToString()
            };

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Server Error")
                });

            // Act
            var result = await _propertyService!.GetPropertiesAsync(otaRequest, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(500, result.Error.Status);
        }

        [TestMethod]
        public async Task GetPropertiesAsync_ReturnsError_WhenBackendReturnsEmptyList()
        {
            // Arrange
            var otaRequest = new OTAPropertySearchRequest
            {
                TradingAgreementCode = "HGV-HGV-ARV",
                CorrelationID = Guid.NewGuid().ToString()
            };

            var backendResponse = new PagedResult<Item>
            {
                Items = [],
                PageSize = 10,
                CurrentPage = 1,
                TotalItems = 0
            };

            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson)
                });

            // Act
            var result = await _propertyService!.GetPropertiesAsync(otaRequest, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task GetPropertiesAsync_ReturnsError_WhenExceptionThrown()
        {
            // Arrange
            var otaRequest = new OTAPropertySearchRequest
            {
                TradingAgreementCode = "HGV-ARV-HGV",
                CorrelationID = Guid.NewGuid().ToString()
            };

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network failure"));

            // Act
            var result = await _propertyService!.GetPropertiesAsync(otaRequest, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(500, result.Error.Status);
            StringAssert.Contains(result.Error.Detail, "An unexpected error occurred on the server.");
        }

        #endregion

        #region GET Tests

        [TestMethod]
        public async Task GetPropertyByCodeAsync_ReturnsSuccess_WhenBackendReturnsValidData()
        {
            // Arrange
            var propertyCode = "CHO-NY537";
            var includeMedia = "PROPERTIES_ONLY";

            // Full valid resort item for the mapper
            var resort = new Item
            {
                ItemId = "1",
                ItemCode = propertyCode,
                ItemClass = ApiConstants.ItemClassResort,
                Name = "Hilton Resort",
                Description = "Luxury Resort",
                ParentCode = "",
                ItemExternalStatus = "Published",

                Profile = new Profile
                {
                    Addresses =
            [
                new Profile.Address
                {
                    AddressType = "PRIMARY",
                    AddressPurpose = "BUSINESS",
                    Line1 = "Beach Road",
                    City = "New York",
                    State = "NY",
                    StateCode = "NY",
                    PostalCode = "10001",
                    Country = "USA",
                    CountryCode = "US"
                }
            ],
                    PhoneNumbers = [],
                    DigitalAddresses = []
                },

                ItemAttributes = [],
                SupplementalAttributes = new SupplementalAttributes(),

                Media = [],
                GeoLocation = new GeoLocationItem
                {
                    Type = "Point",
                    Coordinates = [40.7128, -74.0060]
                }
            };

            var backendItems = new List<Item> { resort };

            var backendJson = JsonSerializer.Serialize(backendItems, JsonOptionsProvider.Options);

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _propertyService!.GetPropertyByCodeAsync(propertyCode, includeMedia);

            // Assert
            Assert.IsTrue(result.Success, "Expected Success=true");
            Assert.IsNotNull(result.Data);
            Assert.IsTrue(result.Data.Properties.Count > 0);

            var mappedResort = result.Data.Properties.First();
            Assert.AreEqual(propertyCode, mappedResort.BasicPropertyInfoSummary.PropertyCode);
            Assert.AreEqual("Hilton Resort", mappedResort.BasicPropertyInfoSummary.Name);
        }

        [TestMethod]
        public async Task GetPropertyByCodeAsync_ReturnsError_WhenPropertyCodeMissing()
        {
            // Act
            var result = await _propertyService!.GetPropertyByCodeAsync("", "PROPERTIES_ONLY");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task GetPropertyByCodeAsync_ReturnsError_WhenIncludeMediaInvalid()
        {
            // Act
            var result = await _propertyService!.GetPropertyByCodeAsync("PROP001", "INVALID_MEDIA");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task GetPropertyByCodeAsync_ReturnsError_WhenBackendFails()
        {
            // Arrange
            var propertyCode = "PROP001";

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("Not Found")
                });

            // Act
            var result = await _propertyService!.GetPropertyByCodeAsync(propertyCode, "PROPERTIES_ONLY");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(404, result.Error.Status);
        }

        [TestMethod]
        public async Task GetPropertyByCodeAsync_ReturnsError_WhenBackendReturnsEmptyList()
        {
            // Arrange
            var propertyCode = "PROP001";
            var backendItems = new List<Item>(); // empty list
            var backendJson = JsonSerializer.Serialize(backendItems, JsonOptionsProvider.Options);

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _propertyService!.GetPropertyByCodeAsync(propertyCode, "PROPERTIES_ONLY");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task GetPropertyByCodeAsync_ReturnsError_WhenExceptionThrown()
        {
            // Arrange
            var propertyCode = "PROP001";

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Timeout occurred"));

            // Act
            var result = await _propertyService!.GetPropertyByCodeAsync(propertyCode, "PROPERTIES_ONLY");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(500, result.Error.Status);
            Assert.AreEqual("Internal Server Error", result.Error.Title);
            StringAssert.Contains(result.Error.Detail, "An unexpected error occurred on the server.");
        }

        #endregion
    }
}
