using APITransformationsFunction.Model.Search;
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
    public class InventorySearchServiceTests
    {
        private Mock<ILogger<InventorySearchService>>? _mockLogger;
        private Mock<HttpMessageHandler>? _mockHttpHandler;
        private HttpClient? _httpClient;
        private InventorySearchService? _searchService;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<InventorySearchService>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            _httpClient = new HttpClient(_mockHttpHandler.Object)
            {
                BaseAddress = new Uri("https://api.dev.onegrandvacation.com/")
            };

            _searchService = new InventorySearchService(_httpClient, _mockLogger.Object);
        }

        #region Success Test

        [TestMethod]
        public async Task SearchAsync_ReturnsSuccess_WhenBackendRespondsOk()
        {
            // Arrange
            var otaRequest = new OTAInventorySearchRequest
            {
                CorrelationID = Guid.NewGuid().ToString(),
                TradingAgreementCode = "HGV-HGV-ARV",
                IncludeAvailablePropertiesOnly = false,
                IncludePropertyMetadata = true,
                PropertySearchCriteria = new OTAPropertySearchCriteria
                {
                    PropertiesOnly = false,
                    PropertyCodes = ["TEST123"],
                    StayDateRange = new OTAStayDateRange
                    {
                        Start = DateTime.UtcNow,
                        End = DateTime.UtcNow.AddDays(5),
                        FlexibleDays = 2
                    },
                    GuestCounts = [new OTAGuestCount() { Count = 1, AgeQualifyingCode = 10 }]
                }
            };

            // Mock backend response
            var backendResponse = new ONEXInventorySearchResponse
            {
                CorrelationId = otaRequest.CorrelationID,
                Data = new ONEXSearchData
                {
                    InventoryItems = new PagedResult<InventoryItem>
                    {
                        Items =
                [
                    new InventoryItem
                    {
                        ItemCode = "UNIT1",
                        ItemClass = "Unit",
                        ParentCode = "PROP001",
                        Name = "Sample Unit",
                        Description = "Unit description",
                        ItemAvailability = new ItemAvailability
                        {
                            StartDate = DateTime.UtcNow.ToString("o"),
                            EndDate = DateTime.UtcNow.AddDays(3).ToString("o"),
                            QuantityOnHand = 2,
                            UnitOfMeasure = "Room",
                            Status = "Available",
                            ItemSchedulePriceLevels =
                            [
                                new ItemSchedulePriceLevel
                                {
                                    Currency = "USD",
                                    Price = 100,
                                    Status = true
                                }
                            ]
                        },
                        ItemAttributes =
                        [
                            new ItemAttribute { Type = "Occupancy", Attribute = "MaxOccupancy", Value = "4" },
                            new ItemAttribute { Type = "Occupancy", Attribute = "StandardOccupancy", Value = "2" }
                        ]
                    }
                ],
                        PageSize = 10,
                        CurrentPage = 1
                    }
                }
            };

            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler!.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _searchService!.InventorySearchAsync(otaRequest, 1, 20);

            // Assert
            Assert.IsTrue(result.Success, "Expected Success = true, but got false. Check validation or mock setup.");
            Assert.IsNotNull(result.Data, "Expected result.Data to be not null.");
            Assert.AreEqual(otaRequest.TradingAgreementCode, result.Data.TradingAgreementCode);
            Assert.AreEqual(1, result.Data.Properties.Count, "Expected 1 property in result.");
            Assert.AreEqual("PROP001", result.Data.Properties[0].BasicPropertyInfoSummary.PropertyCode);
            Assert.AreEqual("Sample Unit", result.Data.Properties[0].UnitTypes[0].Description.Name);
        }

        #endregion

        #region Backend Error Test

        [TestMethod]
        public async Task SearchAsync_ReturnsError_WhenBackendReturnsFailure()
        {
            // Arrange
            var otaRequest = new OTAInventorySearchRequest
            {
                CorrelationID = Guid.NewGuid().ToString(),
                TradingAgreementCode = "HGV-HGV-ARV",
                PropertySearchCriteria = new OTAPropertySearchCriteria
                {
                    PropertiesOnly = false,
                    StayDateRange = new OTAStayDateRange
                    {
                        Start = DateTime.UtcNow,
                        End = DateTime.UtcNow.AddDays(1)
                    },
                    GuestCounts = [new OTAGuestCount() { Count = 1, AgeQualifyingCode = 10 }]
                }
            };

            _mockHttpHandler!.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Bad Request")
                });

            // Act
            var result = await _searchService!.InventorySearchAsync(otaRequest, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(400, result.Error.Status);
            Assert.AreEqual("Bad Request", result.Error.Title);
        }

        #endregion

        #region Null Backend Data Test

        [TestMethod]
        public async Task SearchAsync_ReturnsError_WhenBackendDataIsNull()
        {
            // Arrange
            var otaRequest = new OTAInventorySearchRequest
            {
                TradingAgreementCode = "HGV-HGV-ARV",
                CorrelationID = "1fb15db1-738b-4403-8666-042ecefa2472",
                PropertySearchCriteria = new OTAPropertySearchCriteria
                {
                    PropertiesOnly = true,
                    StayDateRange = new OTAStayDateRange
                    {
                        Start = DateTime.UtcNow,
                        End = DateTime.UtcNow.AddDays(1)
                    },
                    GuestCounts = [new OTAGuestCount() { Count = 1, AgeQualifyingCode = 10 }]
                }
            };

            var backendResponse = new ONEXInventorySearchResponse
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Data = null // simulate invalid backend
            };

            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler!.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson)
                });

            // Act
            var result = await _searchService!.InventorySearchAsync(otaRequest, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(400, result.Error.Status);
            Assert.AreEqual("Bad Request", result.Error.Title);
        }

        #endregion

        #region Exception Handling Test

        [TestMethod]
        public async Task SearchAsync_ReturnsError_WhenExceptionIsThrown()
        {
            var otaRequest = new OTAInventorySearchRequest
            {
                TradingAgreementCode = "HGV-HGV-ARV",
                CorrelationID = Guid.NewGuid().ToString(),
                IncludeAvailablePropertiesOnly = false, // ✅ Required for validation
                PropertySearchCriteria = new OTAPropertySearchCriteria
                {
                    PropertiesOnly = true,
                    PropertyCodes = ["PROP001"], // ✅ single code required
                    StayDateRange = new OTAStayDateRange
                    {
                        Start = DateTime.UtcNow,
                        End = DateTime.UtcNow.AddDays(1)
                    },
                    GuestCounts = [new OTAGuestCount() { Count = 1, AgeQualifyingCode = 10 }]
                }
            };

            _mockHttpHandler!.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Connection failed"));

            // Act
            var result = await _searchService!.InventorySearchAsync(otaRequest, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(500, result.Error.Status);
            Assert.AreEqual("Internal Server Error", result.Error.Title);
            StringAssert.Contains(result.Error.Detail, "An unexpected error occurred on the server.");
        }


        #endregion

        [TestMethod]
        public async Task SearchAsync_ReturnsError_WhenBackendReturnsFailure_AfterValidationPasses()
        {
            var otaRequest = new OTAInventorySearchRequest
            {
                CorrelationID = Guid.NewGuid().ToString(),
                TradingAgreementCode = "HGV-HGV-ARV",
                IncludeAvailablePropertiesOnly = false,
                IncludePropertyMetadata = true,
                PropertySearchCriteria = new OTAPropertySearchCriteria
                {
                    PropertiesOnly = false,
                    PropertyCodes = ["PROP001"],
                    StayDateRange = new OTAStayDateRange
                    {
                        Start = DateTime.UtcNow,
                        End = DateTime.UtcNow.AddDays(1)
                    },
                    GuestCounts = [new OTAGuestCount { Count = 1, AgeQualifyingCode = 10 }]
                }
            };

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Bad Request")
                });

            var result = await _searchService.InventorySearchAsync(otaRequest, 1, 10);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task SearchAsync_ReturnsError_WhenBackendReturnsNullData_AfterValidationPasses()
        {
            var otaRequest = new OTAInventorySearchRequest
            {
                CorrelationID = Guid.NewGuid().ToString(),
                TradingAgreementCode = "HGV-HGV-ARV",
                IncludeAvailablePropertiesOnly = false,
                IncludePropertyMetadata = true,
                PropertySearchCriteria = new OTAPropertySearchCriteria
                {
                    PropertiesOnly = false,
                    PropertyCodes = ["PROP001"],
                    StayDateRange = new OTAStayDateRange
                    {
                        Start = DateTime.UtcNow,
                        End = DateTime.UtcNow.AddDays(1)
                    },
                    GuestCounts = [new OTAGuestCount { Count = 1, AgeQualifyingCode = 10 }]
                }
            };

            var backendResponse = new ONEXInventorySearchResponse
            {
                CorrelationId = otaRequest.CorrelationID,
                Data = null // 👈 triggers branch
            };

            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            var result = await _searchService.InventorySearchAsync(otaRequest, 1, 10);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }
    }
}
