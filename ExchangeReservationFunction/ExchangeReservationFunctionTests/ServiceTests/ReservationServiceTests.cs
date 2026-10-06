using ExchangeReservationFunction.Model.Reservation.Create.Request;
using ExchangeReservationFunction.Model.Reservation.Create.Response;
using ExchangeReservationFunction.Model.Reservation.Search;
using ExchangeReservationFunction.Model.Reservation.Common;
using ExchangeReservationFunction.Model.Common;
using ExchangeReservationFunction.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Address = ExchangeReservationFunction.Model.Reservation.Common.Address;
using Base = ExchangeReservationFunction.Model.Reservation.Create.Request.Base;
using ContactInfo = ExchangeReservationFunction.Model.Reservation.Common.ContactInfo;
using ExternalReference = ExchangeReservationFunction.Model.Reservation.Create.Request.ExternalReference;
using Guarantee = ExchangeReservationFunction.Model.Reservation.Create.Request.Guarantee;
using GuestCount = ExchangeReservationFunction.Model.Reservation.Create.Request.GuestCount;
using MemberProfile = ExchangeReservationFunction.Model.Reservation.Common.MemberProfile;
using Phone = ExchangeReservationFunction.Model.Reservation.Common.Phone;
using RateDetail = ExchangeReservationFunction.Model.Reservation.Create.Request.RateDetail;
using RatePlan = ExchangeReservationFunction.Model.Reservation.Create.Request.RatePlan;
using RoomRate = ExchangeReservationFunction.Model.Reservation.Create.Request.RoomRate;
using SourceSystemId = ExchangeReservationFunction.Model.Reservation.Common.SourceSystemId;
using SpecialRequest = ExchangeReservationFunction.Model.Reservation.Create.Request.SpecialRequest;
using SystemMeta = ExchangeReservationFunction.Model.Reservation.Create.Request.SystemMeta;
using UnitStay = ExchangeReservationFunction.Model.Reservation.Create.Request.UnitStay;
using UnitStayDates = ExchangeReservationFunction.Model.Reservation.Create.Request.UnitStayDates;

namespace ExchangeReservationFunctionTests.ServiceTests
{
    [TestClass]
    public class ReservationServiceTests : IDisposable
    {
        private Mock<ILogger<ReservationService>>? _mockLogger;
        private ReservationService? _reservationService;
        private Mock<HttpMessageHandler>? _mockHttpHandler;
        private HttpClient? _httpClient;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<ReservationService>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _mockHttpHandler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
            _httpClient = new HttpClient(_mockHttpHandler.Object)
            {
                BaseAddress = new Uri("https://api.hgv.com")
            };

            _reservationService = new ReservationService(_httpClient, _mockLogger.Object);
        }

        #region Create Reservation Tests      

        private static OTACreateReservationRequest GetValidRequest() => new()
        {
            CorrelationID = "1a2b3c4d-5e6f-7890-abcd-1234567890ef",
            TradingAgreementCode = "HGV-HGV-RES",
            ReservationCandidates = new ReservationCandidates
            {

                SourceReservationIds =
                [
                    new SourceReservationId { SourceSystem = "PARTNER", Id = "RES323322" }
                ],
                ReservationComments =
                [
                     new ReservationComment { Comment = "Need wheel chair", Category = "General" }
                ],
                LoyaltyRedemption = new ExchangeReservationFunction.Model.Reservation.Create.Request.LoyaltyRedemption
                {

                    CurrencyCode = "USD",
                    RedemptionQuantity = 25
                },

                Properties = new Properties
                {
                    BasicPropertyInfoSummary = new BasicPropertyInfoSummaryPostRequest
                    {
                        PropertyCode = "MIA456",
                        PropertyCategory = "5-STAR",
                        PropertySegment = "LEISURE",
                        UnitStays =
                        [
                            new UnitStay
                            {
                                UnitTypeCode = "DLX",
                                UnitDescription = "Deluxe oceanfront suite with balcony",
                                RatePlan =
                                [
                                    new RatePlan
                                    {
                                        RatePlanCode = "VIPRATE",
                                        EffectiveDate = "2025-11-01",
                                        GuaranteeRequired = true
                                    }
                                ],
                                SpecialRequests =
                                [
                                    new SpecialRequest { RequestCode = "NS", Text = "Non-smoking room" },
                                    new SpecialRequest { Text = "Late check-out at 2 PM" }
                                ],
                                RoomRates =
                                [
                                    new RoomRate
                                    {
                                        NumberOfUnits = 1,
                                        Rates =
                                        [
                                            new RateDetail
                                            {
                                                Base = new Base { AmountBeforeTax = 200, CurrencyCode = "USD", DecimalPlaces = 2 },
                                                Taxes =
                                                [
                                                    new ExchangeReservationFunction.Model.Reservation.Create.Request.Tax { Amount = 20, CurrencyCode = "USD", DecimalPlaces = 2, Type = "Exclusive", Description = "City tax (10%)" }
                                                ],
                                                Fees =
                                                [
                                                    new ExchangeReservationFunction.Model.Reservation.Create.Request.Fee { Amount = 15, CurrencyCode = "USD", DecimalPlaces = 2, Description = "Resort fee" }
                                                ],
                                                Total = new ExchangeReservationFunction.Model.Reservation.Create.Request.Total { AmountAfterTax = 235, CurrencyCode = "USD", DecimalPlaces = 2 },

                                            }
                                        ],

                                    }
                                ],

                                GuestCounts =
                                [
                                    new GuestCount { Count = 2, AgeQualifyingCode = 10 },
                                    new GuestCount { Count = 1, AgeQualifyingCode = 8 }
                                ],
                                UnitStayDates = new UnitStayDates
                                {
                                    Start = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                    End = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                                },
                                Guarantee = new Guarantee
                                {
                                    GuaranteeType = "CREDIT_CARD",
                                    Amount = 500,
                                    CurrencyCode = "USD",
                                    Deadline = "2025-10-31T23:59:59Z",
                                    GuaranteedAccepted = true
                                }
                            }
                        ]
                    }
                },
                Profiles =
                [
                        new Profile
                        {
                            MemberProfile = new MemberProfile
                            {
                                FirstName = "John",
                                LastName = "Doe",
                                DateOfBirth = "1985-05-21",
                                Sex = "M",
                                IsOccupied = true,
                                ProfileType = "Individual",
                                SourceSystemIds =
                                [
                                    new SourceSystemId { SourceSystem = "EXCHANGE", Id = "MEM322332" }
                                ],

                                ContactInfo = new ContactInfo
                                {
                                    Emails =
                                    [
                                        new EmailDetails { Email = "john.doe@example.com", EmailType = "CUS", IsPrimary = true }
                                    ],
                                    Phones =
                                    [
                                        new Phone { PhoneNumber = "9876543210", PhoneTechType = "Mobile", CountryCode = "91", IsPrimary = true, AreaCode = "67" }
                                    ],
                                    Addresses =
                                    [
                                        new Address
                                        {
                                            AddressType = "Home",
                                            AddressLine1 = "123 Beach Road",
                                            CityName = "Miami",
                                            PostalCode = "10001",
                                            CountryCode = "USA",
                                            Country = "United States",
                                            IsPrimary = true,
                                            AddressLine2 = "test",
                                            Latitude = 12.2,
                                            Longitude = 32.6,
                                            State = "NY",
                                            StateProv = "NY"

                                        }
                                    ]
                                }
                            }
                        }
                ]
            }
        };


        [TestMethod]
        public async Task CreateReservationAsyncReturnsSuccessWhenBackendReturnsValidResponse()
        {
            // Arrange
            var otaRequest = GetValidRequest();

            var backendResponse = new ReservationResponse
            {
                Id = Guid.NewGuid().ToString(),
                Source = "ReservationSystem",
                Specversion = "1.0",
                Type = "Reservation-Response",
                Datacontenttype = "application/json",
                Dataschema = "https://schemas.onetruth.example/reservations/response/2020-12",
                Subject = "Reservation Confirmation",
                Time = DateTimeOffset.UtcNow,
                Ispartial = false,

                Data = new ReservationResponseData
                {
                    SystemMeta = new SystemMeta
                    {
                        TenantID = "HGV",
                        InsertedDateTime = DateTimeOffset.UtcNow,
                        InsertedBy = "SystemUser",
                        UpdatedDateTime = DateTimeOffset.UtcNow,
                        UpdatedBy = "SystemUser",
                        Status = true,
                        StatusReason = "Processed successfully"
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = "PARTNER",
                        TargetSystem = "INTERNAL",
                        SourceReferenceId = "RES323322",
                        TargetReferenceId = "BK123456"
                    },
                    BookingStatus = "CONFIRMED",
                    BookingId = "BK123456",
                    PaymentStatus = "PAID",
                    CreatedAt = DateTimeOffset.UtcNow,

                    BookingItems =
                    [
                        new BookingItemResponse
                        {
                            ItemCode = "UNIT001",
                            ItemClass = "ROOM",
                            ParentCode = "PROP01",
                            Name = "Deluxe Oceanfront Suite",
                            Description = "Spacious suite with balcony and ocean view",
                            ItemAttributes =
                            [
                                new AttributePair { Attribute = "View", Value = "Ocean" },
                                new AttributePair { Attribute = "BedType", Value = "King" }
                            ],
                            MemberID = "MEM322332",
                            StartDate = new DateTimeOffset(2025, 11, 15, 14, 0, 0, TimeSpan.Zero),
                            EndDate = new DateTimeOffset(2025, 11, 22, 10, 0, 0, TimeSpan.Zero),
                            WeekNumber = 46,
                            Action = "BOOKED",
                            GuestCounts = new GuestCounts { Adults = 2, Children = 1 },
                            IsOwnerBooking = false,
                            GuestInfo = new GuestInfo
                            {
                                FirstName = "John",
                                LastName = "Doe",
                                PostalAddresses =
                                [
                                    new PostalAddress
                                    {
                                        Sequence = 1,
                                        AddressType = "Home",
                                        Line1 = "123 Main St",
                                        City = "New York",
                                        State = "NY",
                                        PostalCode = "10001",
                                        Country = "USA"
                                    }
                                ],
                                PhoneNumbers =
                                [
                                    new PhoneNumber
                                    {
                                        Sequence = 1,
                                        PhoneType = "Mobile",
                                        Number = "+1-202-555-1234"
                                    }
                                ],
                                DigitalAddresses =
                                [
                                    new DigitalAddress
                                    {
                                        Sequence = 1,
                                        DigitalAddressType = "Email",
                                        DigitalAddressValue = "john.doe@example.com"
                                    }
                                ]
                            },
                            PriceLevel = new PriceLevel
                            {
                                Status = true,
                                PriceLevelCode = "Elite",
                                Currency = "USD",
                                TotalPrice = (decimal?)235.00,
                                PriceAttributes =
                                [
                                    new AttributePair { Attribute = "Base", Value = "200" },
                                    new AttributePair { Attribute = "Tax", Value = "20" },
                                    new AttributePair { Attribute = "Fee", Value = "15" }
                                ]
                            },
                            BookingAttributes =
                            [
                                new AttributePair { Attribute = "RoomPreference", Value = "Non-Smoking" }
                            ],
                            BookingComments =
                            [
                                new AttributePair { Attribute = "Comment", Value = "Need wheelchair access" }
                            ]
                        }
                    ],

                    GuaranteeAndCancelPolicy = new GuaranteeAndCancelPolicy
                    {
                        GuaranteeDescription = "Credit card required at time of booking",
                        GuaranteeType = "CREDIT_CARD",
                        CancelPenalties =
                        [
                            new CancelPenalty
                            {
                                NonRefundable = false,
                                Deadline = new CancelDeadline
                                {
                                    AbsoluteDeadline = new DateTimeOffset(2025, 11, 10, 23, 59, 59, TimeSpan.Zero)
                                },
                                AmountPercent = new CancelAmount
                                {
                                    Amount = 50,
                                    FeesInclusive = true,
                                    TaxInclusive = false
                                },
                                PenaltyDescription = "50% charge if cancelled after deadline"
                            }
                        ]
                    }
                }
            };


            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _reservationService!.CreateReservationAsync(otaRequest);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data);
        }

        [TestMethod]
        public async Task CreateReservationAsyncReturnsSuccessWhenBackendReturnsValidData()
        {
            // Arrange
            var otaRequest = GetValidRequest();
            var backendResponse = new ReservationResponse
            {
                Id = "123",
                Source = "OneXChange",
                Specversion = "1.0",
                Type = "Reservation-Response",
                Datacontenttype = "application/json",
                Dataschema = "https://schemas.onetruth.example/reservations/response/2020-12",
                Subject = "TestReservation",
                Time = DateTimeOffset.UtcNow,
                Ispartial = false,
                Data = new ReservationResponseData
                {
                    BookingId = "BKG1234",
                    BookingStatus = "CONFIRMED",
                    PaymentStatus = "PAID",
                    CreatedAt = DateTimeOffset.UtcNow,
                    BookingItems =
                    [
                        new BookingItemResponse
                        {
                            ItemCode = "ROOM1",
                            ItemClass = "Deluxe",
                            ParentCode = "RESV001",
                            Name = "Deluxe Room",
                            Description = "Sea View Room",
                            StartDate = DateTimeOffset.Parse("2025-12-01T00:00:00Z", CultureInfo.InvariantCulture),
                            EndDate = DateTimeOffset.Parse("2025-12-07T00:00:00Z", CultureInfo.InvariantCulture),
                            WeekNumber = 49,
                            Action = "BOOK",
                            MemberID = "MEM001",
                            IsOwnerBooking = true
                        }
                    ]
                }
            };

            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _reservationService!.CreateReservationAsync(otaRequest);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data);
            Assert.IsNull(result.Error);
        }

        [TestMethod]
        public async Task CreateReservationAsyncReturnsFailureWhenBackendServiceFails()
        {
            // Arrange
            var otaRequest = GetValidRequest();

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Backend error")
                });

            // Act
            var result = await _reservationService!.CreateReservationAsync(otaRequest);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(500, result.Error.Status);
            StringAssert.Contains(result.Error.Title, "Internal Server Error");
        }

        [TestMethod]
        public async Task CreateReservationAsyncReturnsBadRequestWhenValidationFails()
        {
            // Arrange
            var invalidRequest = new OTACreateReservationRequest
            {
                TradingAgreementCode = null,
                CorrelationID = null
            };

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(/*lang=json,strict*/ "{\"message\": \"Bad Request - Validation failed\"}", Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _reservationService!.CreateReservationAsync(invalidRequest);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(400, result.Error.Status);
            Assert.IsTrue(result.Error.Title.Contains("Bad Request", StringComparison.OrdinalIgnoreCase)
                          || result.Error.Detail.Contains("Invalid or missing parameters.", StringComparison.OrdinalIgnoreCase),
                          "Expected error message to mention validation failure.");
        }

        [TestMethod]
        public async Task CreateReservationAsyncReturnsBadRequestWhenRequestIsInvalid()
        {
            // Arrange
            var invalidRequest = new OTACreateReservationRequest
            {
                CorrelationID = null,   // required field missing
                ReservationCandidates = null
            };

            // Act
            var result = await _reservationService!.CreateReservationAsync(invalidRequest);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual("Bad Request", result.Error.Title);
            _mockHttpHandler!.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            ); // ensures backend wasn’t called
        }

        [TestMethod]
        public async Task CreateReservationAsyncReturnsErrorWhenBackendReturnsNullData()
        {
            // Arrange
            var otaRequest = GetValidRequest();

            // explicitly data null
            var backendJson = /*lang=json,strict*/ """
            {
                "id": "123",
                "source": "ReservationSystem",
                "type": "Reservation-Response",
                "data": null
            }
            """;

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

            // Act
            var result = await _reservationService.CreateReservationAsync(otaRequest);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual("Bad Request", result.Error.Title);
        }

        [TestMethod]
        public async Task CreateReservationAsyncReturnsInternalServerErrorWhenExceptionThrown()
        {
            // Arrange
            var otaRequest = GetValidRequest();

            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network failure"));

            // Act
            var result = await _reservationService.CreateReservationAsync(otaRequest);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(500, result.Error.Status);
            Assert.AreEqual("Internal Server Error", result.Error.Title);
        }

        #endregion

        #region Get Reservation Tests

        [TestMethod]
        public async Task GetReservationByIdAsyncReturnsSuccessWhenBackendReturnsValidData()
        {
            // Arrange
            var reservationId = "RES12345";
            var backendResponse = GetValidBackendResponse(reservationId);
            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler!.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _reservationService!.GetReservationByIdAsync(reservationId);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data);
            Assert.IsTrue(result.Data.Reservations.Count >= 1, "Expected at least one reservation in the result.");
        }

        [TestMethod]
        public async Task GetReservationByIdAsyncReturnsBadRequestWhenReservationIdIsInvalid()
        {
            // Arrange
            var invalidId = "RES@123"; // invalid characters

            // Act
            var result = await _reservationService!.GetReservationByIdAsync(invalidId);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(400, result.Error.Status);
            StringAssert.Contains(result.Error.Detail, "Invalid or missing parameters.");
            _mockHttpHandler!.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        public async Task GetReservationByIdAsyncReturnsNotFoundWhenBackendReturnsNullData()
        {
            // Arrange
            var reservationId = "RES12345";
            var backendResponse = new OnexReservationRequest { Data = null };
            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler!.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _reservationService!.GetReservationByIdAsync(reservationId);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(404, result.Error.Status);
        }

        [TestMethod]
        public async Task GetReservationByIdAsyncReturnsFailureWhenBackendServiceFails()
        {
            // Arrange
            var reservationId = "RES12345";

            _mockHttpHandler!.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Server error")
                });

            // Act
            var result = await _reservationService!.GetReservationByIdAsync(reservationId);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(500, result.Error.Status);
        }

        [TestMethod]
        public async Task GetReservationByIdAsyncReturnsFailureWhenExceptionOccurs()
        {
            // Arrange
            var reservationId = "RES12345";

            _mockHttpHandler!.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection failed"));

            // Act
            var result = await _reservationService!.GetReservationByIdAsync(reservationId);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(500, result.Error.Status);
            StringAssert.Contains(result.Error.Title, "Internal Server Error");
        }

        private static OnexReservationRequest GetValidBackendResponse(string reservationId) => new()
        {
            Id = reservationId,
            Source = "OneXChange",
            Type = "Reservation-Response",
            Data = new ReservationRequestData
            {
                SystemMeta = new SystemMeta(),
                ExternalReference = new ExternalReference(),
                BookingItems =
        [
            new BookingItemRequest
            {
                ItemCode = "ROOM1",
                Name = "Deluxe Room",
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(2),
                GuestCounts = new GuestCounts(),
                GuestInfo = new GuestInfo
                {
                    FirstName = "John",
                    LastName = "Doe",
                    PostalAddresses = [],
                    PhoneNumbers = [],
                    DigitalAddresses = []
                },
                PriceLevel = new PriceLevel
                {
                    Status = true,
                    PriceLevelCode = "Elite",
                    Currency = "USD",
                    TotalPrice = 200,
                    PriceAttributes = []
                },
                ItemAttributes = [],
                BookingAttributes = [],
                BookingComments = []
            }
        ]
            }
        };

        #endregion

        #region Search Reservation Tests
        [TestMethod]
        public async Task SearchReservationAsyncReturnsSuccessWhenBackendRespondsValid()
        {
            // Arrange
            var request = GetValidSearchRequest();
            var backendResponse = GetValidBackendResponse();
            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            _mockHttpHandler!
                .Protected()
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
            var result = await _reservationService!.SearchReservationAsync(request, 1, 10);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data);

        }

        [TestMethod]
        public async Task SearchReservationAsyncReturnsErrorWhenRequestIsInvalid()
        {
            // Arrange
            var invalidRequest = new OTASearchReservationRequest
            {
                CorrelationID = "invalid-guid", // invalid UUID format
                TradingAgreementCode = "HGV-HGV-RES",
                // Missing required search criteria (SourceReservationIds / Status etc.)
            };

            // Act
            var result = await _reservationService!.SearchReservationAsync(invalidRequest, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            StringAssert.Contains(result.Error.Detail, "Invalid or missing parameters.");
        }

        [TestMethod]
        public async Task SearchReservationAsyncReturnsErrorWhenBackendReturnsBadRequest()
        {
            // Arrange
            var request = GetValidSearchRequest();

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Invalid or missing parameters.")
                });

            // Act
            var result = await _reservationService!.SearchReservationAsync(request, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            StringAssert.Contains(result.Error.Title, "Bad Request");
        }

        [TestMethod]
        public async Task SearchReservationAsyncReturnsErrorWhenBackendReturnsInternalServerError()
        {
            // Arrange
            var request = GetValidSearchRequest();

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            // Act
            var result = await _reservationService!.SearchReservationAsync(request, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual("https://api.hgv.com/errors/internal-server-error", result.Error.Type);
        }

        [TestMethod]
        public async Task SearchReservationAsyncReturnsInternalServerErrorWhenExceptionThrown()
        {
            // Arrange
            var request = GetValidSearchRequest();

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new InvalidOperationException("Something went wrong"));

            // Act
            var result = await _reservationService!.SearchReservationAsync(request, 1, 10);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual("Internal Server Error", result.Error.Title);
            Assert.AreEqual(500, result.Error.Status);
        }



        private static OTASearchReservationRequest GetValidSearchRequest() => new()
        {
            CorrelationID = Guid.NewGuid().ToString(),
            TradingAgreementCode = "HGV-HGV-RES",
            SourceReservationIds =
            [
                new SourceReservationId
                {
                    SourceSystem = "OTA",
                    Id = "RES123"
                }
            ],
            ArrivalDate = "2025-11-03",
            ArrivalDateInterval = 1,
            ReservationDate = "2025-11-01",
            ReservationDateInterval = 1,
            SourceMemberIds =
            [
                new OTASearchReservationRequest.SourceMemberId
                {
                    Id = "PARTNER",
                    SourceSystem = "1234444"
                }
            ],
            Status = "Confirmed"

        };

        private static OnexSearchReservationResponse GetValidBackendResponse() => new()
        {
            Id = "123424343",
            Source = "test",
            Data = new ReservationSearchResponseData
            {
                SystemMeta = new ExchangeReservationFunction.Model.Reservation.Search.SystemMeta
                {
                    TenantID = "HGV",
                    InsertedDateTime = DateTime.UtcNow,
                    InsertedBy = "Admin",
                    UpdatedBy = "Admin",
                    UpdatedDateTime = DateTime.UtcNow,
                },
                ExternalReference = new ExternalReference
                {
                    SourceReferenceId = "1223333",
                    SourceSystem = "PARTNER",
                    TargetReferenceId = "4465656546",
                    TargetSystem = "CLA"
                },
                Results =
                [
                    new SearchBookingItemResponse
                    {
                        ItemCode = "ROOM1",
                        Name = "Deluxe Room",
                        StartDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        EndDate = DateTimeOffset.UtcNow.AddDays(2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Action = "Confirmed",
                        BookingAttributes =
                        [
                            new AttributePair
                            {
                                Attribute = "guaranteeType",
                                Value = "Need some value"
                            }
                        ],
                        ItemAttributes =
                        [
                            new AttributePair
                            {
                                Attribute = "guaranteeDeadline",
                                Value = "2025-11-05"
                            }
                        ],
                        BookingComments =
                        [
                            new AttributePair
                            {
                                Attribute = "Test3",
                                Value = "Need some value3"
                            }
                        ],
                        Description = "This is a test booking",
                        GuestCounts = new GuestCounts
                        {
                            Adults = 1,
                            Children = 1
                        },
                        IsOwnerBooking = true,
                        ItemClass = "test class",
                        MemberID = "567888",
                        ParentCode = "CVGG",
                        PriceLevel = new PriceLevel
                        {
                            PriceAttributes =
                            [
                                new AttributePair
                                {
                                    Attribute = "totalAfterTax",
                                    Value = "300"
                                },
                                new AttributePair
                                {
                                    Attribute = "tax",
                                    Value = "12"
                                },
                                new AttributePair
                                {
                                    Attribute = "fees",
                                    Value = "23"
                                }
                            ],
                        },
                        WeekNumber = 1,
                        GuestInfo = new GuestInfo
                        {
                            FirstName = "John",
                            LastName = "Doe",
                            PostalAddresses = [],
                            PhoneNumbers = [],
                            DigitalAddresses = []
                        }
                    }
                ]
            }
        };

        public void Dispose()
        {
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
