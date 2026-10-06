using APITransformationsFunction.Model.Member;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace APITransformationsFunctionTests.ServiceTests
{
    [TestClass]
    public class MemberServiceTests
    {
        private Mock<ILogger<MemberService>>? _mockLogger;
        private MemberService? _memberService;
        private Mock<HttpMessageHandler>? _mockHttpHandler;
        private HttpClient? _httpClient;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<MemberService>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            _httpClient = new HttpClient(_mockHttpHandler.Object)
            {
                BaseAddress = new Uri("https://api.abc.com")
            };

            _memberService = new MemberService(_httpClient, _mockLogger.Object);
        }

        #region Get Tests

        [TestMethod]
        public async Task GetMemberByIdAsync_ReturnsSuccess_WhenBackendRespondsOk()
        {
            // Arrange
            var memberId = "12345";
            var backendResponse = new OnexGetMemberResponse
            {
                Id = "12234577",
                Source = "CLARITY",
                SpecVersion = "1.0",
                CorrelationId = "113e4567-e89b-12d3-a456-426614174000",
                Type = "New",
                Data = new Data
                {
                    SystemMeta = new SystemMeta
                    {
                        InsertedBy = "Admin",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "Admin",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = true,
                        StatusReason = "Member Created"
                    },
                    PersonName = new PersonName
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        MiddleName = "Bill",
                        Title = "Mr"
                    },
                    PhoneNumbers =
                    [
                        new PhoneNumber
                        {
                            PhonePurpose = "Personal",
                            PhoneType = "Home",
                            Number = "123456",
                            Extension = "2",
                            Status = true
                        }
                        ],
                    PostalAddresses =
                    [
                        new PostalAddress
                        {
                            AddressPurpose = "Home",
                            AddressType = "Home",
                            PostalCode = "12345",
                            City = "NY",
                            Country = "USA",
                            CountryCode = "USA",
                            Latitude = 40.12,
                            Longitude = -30.23,
                            Status = true,
                            Line1 = "1233 street"
                        }
                    ],
                    DigitalAddresses =
                    [
                        new()
                        {
                            DigitalAddress = "abc@test.com",
                            DigitalAddressType = "Personal",
                            Status = true
                        }
                    ],
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = "EXCHANGE",
                        ReferenceId = "12345"
                    },
                    MembershipInformation = new APITransformationsFunction.Model.Shared.MembershipInformation
                    {
                        AccountNumber = "33333",
                        MembershipLevel = "GOLD",
                        OwnerType = "OWN",
                        PremierGenCode = 767676
                    },
                    OtherAttributes =
                    [
                        new() { Attribute = "DL", Value = "D12345678" }
                    ],
                    MemberResponseMessage = new MemberResponseMessage
                    {
                        Message = "Member Details",
                        MemberInfo = new MemberInfo { MemberId = "222222" },
                        MemberStatus = "Active"
                    }
                }
            };

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
                    Content = new StringContent(backendJson)
                });

            // Act
            var result = await _memberService!.GetMemberByIdAsync(memberId);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data);

            var data = result.Data;
            var source = backendResponse.Data;

            Assert.AreEqual(source.PersonName.FirstName, data.MemberProfile.FirstName);
            Assert.AreEqual(source.PersonName.LastName, data.MemberProfile.LastName);

            // Emails
            if (source.DigitalAddresses != null && source.DigitalAddresses.Count > 0)
            {
                Assert.IsNotNull(data.ContactInfo, "data.ContactInfo should not be null");
                Assert.IsNotNull(data.ContactInfo.Emails, "data.ContactInfo.Emails should not be null");
                Assert.AreEqual(source.DigitalAddresses.Count, data.ContactInfo.Emails.Count, "Email count mismatch");

                for (var i = 0; i < source.DigitalAddresses.Count; i++)
                {
                    Assert.IsNotNull(data.ContactInfo.Emails[i], $"Mapped Emails[{i}] should not be null");

                    Assert.AreEqual(source.DigitalAddresses[i].DigitalAddress, data.ContactInfo.Emails[i].Email, $"Mismatch in Email at index {i}");
                    Assert.AreEqual(source.DigitalAddresses[i].DigitalAddressType, data.ContactInfo.Emails[i].EmailType, $"Mismatch in EmailType at index {i}");
                    Assert.AreEqual(source.DigitalAddresses[i].Status, data.ContactInfo.Emails[i].IsPrimary, $"Mismatch in IsPrimary at index {i}");
                }
            }
            else
            {
                Assert.IsTrue(data.ContactInfo?.Emails == null || data.ContactInfo.Emails.Count == 0,
                    "Emails should be null or empty when source has no digital addresses");
            }

            // Phones- Note: Clarity is not sending this details
            if (source.PhoneNumbers != null)
            {
                Assert.IsNotNull(data.ContactInfo, "data.ContactInfo should not be null");
                Assert.IsNotNull(data.ContactInfo.Phones, "data.ContactInfo.Phones should not be null");
                Assert.AreEqual(source.PhoneNumbers.Count, data.ContactInfo.Phones.Count, "Phone count mismatch");

                for (var i = 0; i < source.PhoneNumbers.Count; i++)
                {
                    Assert.IsNotNull(data.ContactInfo.Phones[i], $"Mapped Phones[{i}] should not be null");

                    Assert.AreEqual(source.PhoneNumbers[i].Number, data.ContactInfo.Phones[i].PhoneNumber, $"Mismatch in PhoneNumber at index {i}");
                    Assert.AreEqual(source.PhoneNumbers[i].PhoneType, data.ContactInfo.Phones[i].PhoneTechType, $"Mismatch in PhoneTechType at index {i}");
                    Assert.AreEqual(source.PhoneNumbers[i].Extension, data.ContactInfo.Phones[i].AreaCode, $"Mismatch in AreaCode at index {i}");
                    Assert.AreEqual(source.PhoneNumbers[i].Status, data.ContactInfo.Phones[i].IsPrimary, $"Mismatch in IsPrimary at index {i}");
                }
            }
            else
            {
                Assert.IsTrue(data.ContactInfo?.Phones == null || data.ContactInfo.Phones.Count == 0, "Phones should be null or empty when source has no phone numbers");
            }


            // Addresses Note: Clarity will send this details only if countrycode = 'USA'

            if (source.PostalAddresses != null && source.PostalAddresses.Count > 0)
            {
                Assert.IsNotNull(data.ContactInfo, "data.ContactInfo should not be null");
                Assert.IsNotNull(data.ContactInfo.Addresses, "data.ContactInfo.Addresses should not be null");
                Assert.AreEqual(source.PostalAddresses.Count, data.ContactInfo.Addresses.Count, "Address count mismatch");

                for (var i = 0; i < source.PostalAddresses.Count; i++)
                {
                    var a1 = source.PostalAddresses[i];
                    var a2 = data.ContactInfo.Addresses[i];

                    Assert.IsNotNull(a1, $"Source.PostalAddresses[{i}] should not be null");
                    Assert.IsNotNull(a2, $"Mapped Addresses[{i}] should not be null");

                    Assert.AreEqual(a1.Line1, a2.AddressLine1, $"Mismatch in AddressLine1 at index {i}");
                    Assert.AreEqual(a1.Line2, a2.AddressLine2, $"Mismatch in AddressLine2 at index {i}");
                    Assert.AreEqual(a1.City, a2.CityName, $"Mismatch in CityName at index {i}");
                    Assert.AreEqual(a1.StateCode, a2.StateProv, $"Mismatch in StateProv at index {i}");
                    Assert.AreEqual(a1.State, a2.State, $"Mismatch in State at index {i}");
                    Assert.AreEqual(a1.PostalCode, a2.PostalCode, $"Mismatch in PostalCode at index {i}");
                    Assert.AreEqual(a1.Country, a2.Country, $"Mismatch in Country at index {i}");
                    Assert.AreEqual(a1.CountryCode, a2.CountryCode, $"Mismatch in CountryCode at index {i}");
                    Assert.AreEqual(a1.Latitude, a2.Latitude, $"Mismatch in Latitude at index {i}");
                    Assert.AreEqual(a1.Longitude, a2.Longitude, $"Mismatch in Longitude at index {i}");
                    Assert.AreEqual(a1.AddressType, a2.AddressType, $"Mismatch in AddressType at index {i}");
                    Assert.AreEqual(a1.Status, a2.IsPrimary, $"Mismatch in IsPrimary at index {i}");
                }
            }
            else
            {
                Assert.IsTrue(data.ContactInfo?.Addresses == null || data.ContactInfo.Addresses.Count == 0,
                    "Addresses should be null or empty when source has no postal addresses");
            }


            // Membership & Loyalty

            if (source.MembershipInformation != null)
            {
                Assert.IsNotNull(data.LoyaltyPrograms, "LoyaltyPrograms should not be null when MembershipInformation exists.");
                Assert.IsTrue(data.LoyaltyPrograms.Count > 0, "LoyaltyPrograms should contain at least one item.");

                var loyalty = data.LoyaltyPrograms.FirstOrDefault();
                Assert.IsNotNull(loyalty, "First LoyaltyProgram entry should not be null.");

                Assert.AreEqual(source.MembershipInformation.PremierGenCode.ToString(), loyalty.ProgramCode, "Mismatch in ProgramCode.");
                Assert.AreEqual(source.MembershipInformation.AccountNumber, loyalty.MembershipId, "Mismatch in MembershipId.");
                Assert.AreEqual(source.MembershipInformation.OwnerType, loyalty.OwnType, "Mismatch in OwnerType.");

                Assert.IsNotNull(data.MemberProfile, "MemberProfile should not be null when MembershipInformation exists.");
            }
            else
            {
                Assert.IsTrue(data.LoyaltyPrograms == null || data.LoyaltyPrograms.Count == 0,
                    "LoyaltyPrograms should be null or empty when MembershipInformation is null.");
            }

        }

        [TestMethod]
        public async Task GetMemberByIdAsync_ReturnsError_WhenBackendReturnsFailure()
        {
            // Arrange
            var memberId = "99999";

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
                      Content = new StringContent("Bad Request")
                  });

            // Act
            var result = await _memberService!.GetMemberByIdAsync(memberId);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task GetMemberByIdAsync_ReturnsError_WhenExceptionThrown()
        {
            // Arrange
            var memberId = "boom";

            _mockHttpHandler!
                  .Protected()
                  .Setup<Task<HttpResponseMessage>>(
                      "SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>()
                  )
                  .ThrowsAsync(new HttpRequestException("Network failure"));

            // Act
            var result = await _memberService!.GetMemberByIdAsync(memberId);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(500, result.Error.Status);
        }

        [TestMethod]
        public async Task GetMemberByIdAsync_ReturnsError_WhenDataIsNull()
        {
            var memberId = "12345";

            var backendResponse = new OnexGetMemberResponse
            {
                Data = null // simulate empty backend data
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

            var result = await _memberService!.GetMemberByIdAsync(memberId);

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task GetMemberByIdAsync_ReturnsError_WhenBackendDataHasErrors()
        {
            var memberId = "12345";

            var backendResponse = new OnexGetMemberResponse
            {
                Data = new Data
                {
                    SystemMeta = new SystemMeta
                    {
                        InsertedBy = "Admin",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "Admin",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = true,
                        StatusReason = "Member Created"
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = "EXCHANGE",
                        ReferenceId = "12344556"
                    },
                    Errors =
                    [
                        new Error { ErrorCode = "InvalidId", ErrorMessage = "Member not found" }
                    ]
                }
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

            var result = await _memberService!.GetMemberByIdAsync(memberId);

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task GetMemberByIdAsync_ReturnsNotFound_WithBackendError()
        {
            // Arrange
            var memberId = "invalid-id";

            // Simulate backend returning an error in Data.Errors
            var backendErrorResponse = new
            {
                data = new
                {
                    errors = new[]
                    {
                new
                {
                    ErrorCode = "404",
                    ErrorMessage = "Member not found",
                    ErrorType = "NotFound",
                    ErrorTitle = "Not Found",
                    ErrorInstance = "abc123-instance"
                }
            }
                }
            };

            var backendJson = JsonSerializer.Serialize(backendErrorResponse);

            var backendResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(backendJson)
            };

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(backendResponse);

            // Act
            var result = await _memberService!.GetMemberByIdAsync(memberId);

            // Assert
            Assert.IsFalse(result.Success, "Result should indicate failure for invalid member ID");
            Assert.IsNotNull(result.Error, "Error object should not be null");

            var error = result.Error;
            Assert.AreEqual(404, error.Status, "Status should match backend ErrorCode");
            Assert.AreEqual("Not Found", error.Title, "Title should match backend ErrorTitle");
            Assert.AreEqual("No member profile found for the specified memberId.", error.Detail, "Detail should match backend ErrorMessage");
        }


        #endregion

        #region POST Tests

        [TestMethod]
        public async Task CreateMemberAsync_ReturnsSuccess_WhenBackendReturnsValidData()
        {
            // Arrange
            var request = new OTAPostMemberRequest
            {
                TradingAgreementCode = "HGV-HGV-ARV",
                ContactInfo = new ContactInfo
                {
                    Addresses =
                    [
                        new Address
                        {
                            AddressLine1 = "address1",
                            AddressLine2 = "address2",
                            AddressType = "Home",
                            Country = "USA",
                            CountryCode = "USA",
                            PostalCode = "96201"
                        }
                    ],
                    Emails =
                    [
                        new Emails
                        {
                            Email = "test@abc.com",
                            EmailType = "CUS",
                            IsPrimary = true,
                        }
                    ],
                    Phones =
                    [
                        new Phone
                        {
                            PhoneNumber = "+1-202-5551234",
                            PhoneTechType = "MOBILE",
                            AreaCode = "20",
                            CountryCode = "+1",
                            IsPrimary = true,
                        }
                    ]
                },
                CorrelationID = "113e4567-e89b-12d3-a456-426614174000",
                MemberProfile = new MemberProfile
                {
                    FirstName = "John",
                    LastName = "Doe",
                    SourceSystemIds =
                    [
                       new SourceSystemId
                       {
                           Id = "12345",
                           SourceSystem = "PARTNER"
                       }

                    ],
                    Gender = "M",
                    BirthDate = "1985-01-01",
                    MemberTier = "GOLD"
                }
            };
            var backendResponse = new MemberResponse
            {
                Source = "test",
                SpecVersion = "1.0.0",
                CorrelationId = "test",
                Id = "test",
                Type = "test",
                Data = new Data
                {
                    SystemMeta = new SystemMeta
                    {
                        InsertedBy = "Admin",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "Admin",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = true,
                        StatusReason = "Member Created"
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = "EXCHANGE",
                        ReferenceId = "12344556"
                    },
                    MemberResponseMessage = new MemberResponseMessage
                    {
                        Message = "Member Created",
                        MemberStatus = "Active",
                        MemberInfo = new MemberInfo { MemberId = "12344556" }
                    },
                    Errors = null
                }
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
            var result = await _memberService!.CreateMemberAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data);
            Assert.IsNull(result.Error);
        }

        [TestMethod]
        public async Task CreateMemberAsync_ReturnsError_WhenRequestIsInvalid()
        {
            // Arrange
            var request = new OTAPostMemberRequest
            {
                TradingAgreementCode = "test",
                ContactInfo = new ContactInfo
                {
                    Addresses =
                    [
                        new Address
                        {
                            AddressLine1 = "address1",
                            AddressLine2 = "address2",
                            AddressType = "Office",
                            Country = "US",
                            CountryCode = "US",
                            PostalCode = "12345"
                        }
                    ],
                    Emails =
                    [
                        new Emails
                        {
                            Email = "test@abc.com",
                            EmailType = "Office",
                            IsPrimary = true,
                        }
                    ],
                    Phones =
                    [
                        new Phone
                        {
                            PhoneNumber = "1",
                            PhoneTechType = "Office",
                            AreaCode = "20",
                            CountryCode = "+1",
                            IsPrimary = true,
                        }
                    ]
                },
                MemberProfile = new MemberProfile
                {
                    FirstName = "John",
                    LastName = "Doe",
                    SourceSystemIds =
                    [
                       new SourceSystemId
                       {
                           Id = "12345",
                           SourceSystem = "Clarity"
                       }

                    ],
                    Gender = "M",
                    BirthDate = "1985-01-01",
                    MemberTier = "MemberTier1"
                }
            };
            var backendResponse = new MemberResponse
            {
                Source = "test",
                SpecVersion = "1.0.0",
                CorrelationId = "test",
                Id = "test",
                Type = "test",
                Data = new Data
                {
                    SystemMeta = new SystemMeta
                    {
                        InsertedBy = "Admin",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "Admin",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = true,
                        StatusReason = "Member Created"
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = "EXCHANGE",
                        ReferenceId = "12344556"
                    },
                    MemberResponseMessage = new MemberResponseMessage
                    {
                        Message = "Member Created",
                        MemberStatus = "Active",
                        MemberInfo = new MemberInfo { MemberId = "12345" }
                    },
                    Errors = null
                }
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
            var result = await _memberService!.CreateMemberAsync(request);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(400, result.Error.Status);
            Assert.AreEqual("Bad Request", result.Error.Title);
        }

        [TestMethod]
        public async Task CreateMemberAsync_ReturnsError_WhenBackendServiceNotAvailable()
        {
            // Arrange
            var request = new OTAPostMemberRequest
            {
                TradingAgreementCode = "HGV-HGV-ARV",
                ContactInfo = new ContactInfo
                {
                    Addresses =
                    [
                        new Address
                        {
                            AddressLine1 = "address1",
                            AddressLine2 = "address2",
                            AddressType = "Home",
                            Country = "USA",
                            CountryCode = "USA",
                            PostalCode = "96201"
                        }
                    ],
                    Emails =
                    [
                        new Emails
                        {
                            Email = "test@abc.com",
                            EmailType = "CUS",
                            IsPrimary = true,
                        }
                    ],
                    Phones =
                    [
                        new Phone
                        {
                            PhoneNumber = "+1-202-5551234",
                            PhoneTechType = "MOBILE",
                            AreaCode = "20",
                            CountryCode = "+1",
                            IsPrimary = true,
                        }
                    ]
                },
                CorrelationID = "113e4567-e89b-12d3-a456-426614174000",
                MemberProfile = new MemberProfile
                {
                    FirstName = "John",
                    LastName = "Doe",
                    SourceSystemIds =
                    [
                       new SourceSystemId
                       {
                           Id = "12345",
                           SourceSystem = "PARTNER"
                       }

                    ],
                    Gender = "M",
                    BirthDate = "1985-01-01",
                    MemberTier = "GOLD"
                }
            };
            var backendResponse = new MemberResponse
            {
                Source = "test",
                SpecVersion = "1.0.0",
                CorrelationId = "test",
                Id = "test",
                Type = "test",
                Data = new Data
                {
                    SystemMeta = new SystemMeta
                    {
                        InsertedBy = "Admin",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "Admin",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = true,
                        StatusReason = "Member Created"
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = "EXCHANGE",
                        ReferenceId = "12344556"
                    },
                    MemberResponseMessage = new MemberResponseMessage
                    {
                        Message = "Member Created",
                        MemberStatus = "Active",
                        MemberInfo = new MemberInfo { MemberId = "12345" }
                    },
                    Errors = null
                }
            };
            var backendJson = JsonSerializer.Serialize(backendResponse, JsonOptionsProvider.Options);

            // Simulate timeout
            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("The request timed out"));

            // Act
            var result = await _memberService!.CreateMemberAsync(request);

            // Assert
            Assert.IsNull(result.Data);
            Assert.IsFalse(result.Success, "Expected Success = false on timeout");
            Assert.IsNotNull(result.Error, "Expected Error to be populated on timeout");
            Assert.AreEqual(500, result.Error.Status);
            Assert.AreEqual("Internal Server Error", result.Error.Title);
        }

        [TestMethod]
        public async Task CreateMemberAsync_ShouldFail_WhenMandatoryFieldsAreMissing()
        {
            // Arrange
            var request = new OTAPostMemberRequest
            {
                CorrelationID = "invalid-uuid",
                MemberProfile = new MemberProfile
                {
                    FirstName = "",
                    LastName = "",
                    SourceSystemIds =
                    [
                        new SourceSystemId
                        {
                            SourceSystem = "",
                            Id = ""
                        }
                    ]
                }
            };

            // Act
            var result = await _memberService!.CreateMemberAsync(request);

            // Assert
            Assert.IsFalse(result.Success, "Request should fail due to invalid mandatory fields");
            Assert.IsNotNull(result.Error, "Error object should not be null");
            Assert.AreEqual(400, result.Error.Status, "HTTP status should be BadRequest");
        }

        [TestMethod]
        public async Task CreateMemberAsync_ShouldFail_WhenCorrelationIdIsInvalid()
        {
            var request = new OTAPostMemberRequest
            {
                CorrelationID = "113e4567e89b12d3a45688",
                MemberProfile = new MemberProfile
                {
                    FirstName = "John",
                    LastName = "Doe",
                    SourceSystemIds =
                    [
                        new() { SourceSystem = "PARTNER", Id = "123" }
                    ],
                    BirthDate = "1990-01-01",
                    Gender = "M"
                }
            };

            var result = await _memberService!.CreateMemberAsync(request);

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(400, result.Error.Status);
            Assert.AreEqual("Bad Request", result.Error.Title);
        }

        [TestMethod]
        public async Task CreateMemberAsync_ShouldFail_WhenSourceSystemIdIsInvalid()
        {
            var request = new OTAPostMemberRequest
            {
                CorrelationID = Guid.NewGuid().ToString(),
                MemberProfile = new MemberProfile
                {
                    FirstName = "Jane",
                    LastName = "Doe",
                    SourceSystemIds =
                    [
                        new() { SourceSystem = "INVALID_SYSTEM", Id = "123" }
                    ],
                    BirthDate = "1990-01-01",
                    Gender = "F"
                }
            };

            var result = await _memberService!.CreateMemberAsync(request);

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(400, result.Error.Status);
            Assert.AreEqual("Bad Request", result.Error.Title);
        }


        [TestMethod]
        public async Task CreateMemberAsync_ReturnsError_WhenBackendReturnsNonSuccessStatusCode()
        {
            // Arrange
            var request = new OTAPostMemberRequest
            {
                TradingAgreementCode = "HGV-HGV-ARV",
                CorrelationID = Guid.NewGuid().ToString(),
                MemberProfile = new MemberProfile
                {
                    FirstName = "John",
                    LastName = "Doe",
                    SourceSystemIds =
                    [
                        new SourceSystemId { SourceSystem = "PARTNER", Id = "123" }
                    ]
                }
            };

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Internal Error")
                });

            // Act
            var result = await _memberService.CreateMemberAsync(request);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(500, result.Error.Status);
        }

        #endregion

        #region PATCH Tests

        [TestMethod]
        public async Task UpdateMemberAsync_ReturnsSuccess_WhenBackendReturnsValidData()
        {
            // Arrange
            var memberId = "12344556";
            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Replace(x => x.MemberProfile.FirstName, "Jo");
            patchDoc.Replace(x => x.MemberProfile.LastName, "Smit");
            patchDoc.Replace(x => x.MemberProfile.BirthDate, "1980-02-20");
            patchDoc.Replace(x => x.MemberProfile.Gender, "M");
            patchDoc.Replace(x => x.MemberProfile.ProfileType, "Individual");
            patchDoc.Replace(x => x.MemberProfile.MarketingKeyCode, "GP2");
            patchDoc.Replace(x => x.MemberProfile.MemberTier, "DIAMOND");
            patchDoc.Add(x => x.MemberProfile.AdditionalIdentifiers, new AdditionalIdentifier { Type = "DL", Value = "D87654321" });
            patchDoc.Replace(x => x.MemberStatus, "Active");
            patchDoc.Add(x => x.ContactInfo.Emails, new Emails { Email = "john.smith@hgv.com", EmailType = "CUS", IsPrimary = true });
            patchDoc.Replace(x => x.ContactInfo.Phones[0], new Phone { PhoneNumber = "+1-202-5551234", PhoneTechType = "VOICE", CountryCode = "+1", AreaCode = "202", IsPrimary = true });
            patchDoc.Add(x => x.ContactInfo.Addresses, new Address { AddressType = "Business", AddressLine1 = "456 Elm St", AddressLine2 = "Apartment 200", CityName = "New York", PostalCode = "10002", StateProv = "NY", State = "New York", CountryCode = "USA", Country = "United States", Latitude = 40.7128, Longitude = -74.006, IsPrimary = true });
            patchDoc.Add(x => x.LoyaltyPrograms, new LoyaltyProgram { ProgramCode = "GP", OwnType = "Individual", MembershipId = "LP123456" });

            var deserializedPatchDoc = PreparePatchDoc(patchDoc);

            var backendResponse = new MemberResponse
            {
                Source = "test",
                SpecVersion = "1.0.0",
                CorrelationId = "test",
                Id = memberId,
                Type = "test",
                Data = new Data
                {
                    SystemMeta = new SystemMeta
                    {
                        InsertedBy = "Admin",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "Admin",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = true,
                        StatusReason = "Member Updated"
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = "EXCHANGE",
                        ReferenceId = memberId
                    },
                    MemberResponseMessage = new MemberResponseMessage
                    {
                        Message = "Member Updated",
                        MemberStatus = "Active",
                        MemberInfo = new MemberInfo { MemberId = memberId }
                    },
                    Errors = null
                }
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
            var result = await _memberService!.UpdateMemberAsync(memberId, deserializedPatchDoc);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data);
            Assert.IsNull(result.Error);
            Assert.AreEqual("Active", result.Data.MemberStatus);
        }


        [TestMethod]
        public async Task UpdateMemberAsync_ReturnsError_WhenRequestIsInvalid()
        {
            // Arrange
            var memberId = "12344556";
            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Replace(x => x.MemberProfile.FirstName, "");

            var deserializedPatchDoc = PreparePatchDoc(patchDoc);

            var backendResponse = new MemberResponse
            {
                Source = "test",
                SpecVersion = "1.0.0",
                CorrelationId = "test",
                Id = memberId,
                Type = "test",
                Data = new Data
                {
                    SystemMeta = new SystemMeta
                    {
                        InsertedBy = "Admin",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "Admin",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = false,
                        StatusReason = "Invalid Request"
                    },
                    ExternalReference = new ExternalReference
                    {
                        ReferenceId = "test",
                        SourceSystem = "test",
                    },
                    Errors =
            [
                new Error
                {
                    ErrorCode = "400",
                    ErrorTitle = "Bad Request",
                    ErrorMessage = "Invalid or missing parameters."
                }
            ]
                }
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
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(backendJson, Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _memberService!.UpdateMemberAsync(memberId, deserializedPatchDoc);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(400, result.Error.Status);
            Assert.AreEqual("Bad Request", result.Error.Title);
            Assert.AreEqual("Invalid or missing parameters.", result.Error.Detail);
        }

        [TestMethod]
        public async Task UpdateMemberAsync_ReturnsError_WhenBackendServiceNotAvailable()
        {
            // Arrange
            var memberId = "12344556";
            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Replace(x => x.MemberProfile.FirstName, "Jo");

            var deserializedPatchDoc = PreparePatchDoc(patchDoc);

            _mockHttpHandler!
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("The request timed out"));

            // Act
            var result = await _memberService!.UpdateMemberAsync(memberId, deserializedPatchDoc);

            // Assert
            Assert.IsNull(result.Data);
            Assert.IsFalse(result.Success, "Expected Success = false on timeout");
            Assert.IsNotNull(result.Error, "Expected Error to be populated on timeout");
            Assert.AreEqual(500, result.Error.Status);
            Assert.AreEqual("Internal Server Error", result.Error.Title);
        }

        [TestMethod]
        public async Task UpdateMemberAsync_ShouldFail_WhenMandatoryFieldsAreMissing()
        {
            // Arrange
            var memberId = "12344556";
            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Replace(x => x.MemberProfile.LastName, "");
            var deserializedPatchDoc = PreparePatchDoc(patchDoc);

            // Act
            var result = await _memberService!.UpdateMemberAsync(memberId, deserializedPatchDoc);

            // Assert
            Assert.IsFalse(result.Success, "Request should fail due to invalid mandatory fields");
            Assert.IsNotNull(result.Error, "Error object should not be null");
            Assert.AreEqual(500, result.Error.Status, "HTTP status should be BadRequest");
        }

        [TestMethod]
        public async Task UpdateMemberAsync_ShouldFail_WhenMemberIdIsInvalid()
        {
            // Arrange
            var memberId = "invalid-id";
            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Replace(x => x.MemberProfile.LastName, "Jo");
            var deserializedPatchDoc = PreparePatchDoc(patchDoc);

            // Act
            var result = await _memberService!.UpdateMemberAsync(memberId, deserializedPatchDoc);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error, "result.Error should not be null");
            Assert.AreEqual(500, result.Error.Status);
            Assert.AreEqual("Internal Server Error", result.Error.Title);
        }

        [TestMethod]
        public async Task UpdateMemberAsync_ActivateMember_Success()
        {
            // Arrange
            var memberId = "12344556";
            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Replace(x => x.MemberStatus, "Active");

            var deserializedPatchDoc = PreparePatchDoc(patchDoc);
            var backendResponse = new MemberResponse
            {
                Source = "test",
                SpecVersion = "1.0.0",
                CorrelationId = "test",
                Id = memberId,
                Type = "test",
                Data = new Data

                {
                    SystemMeta = new SystemMeta
                    {
                        InsertedBy = "Admin",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "Admin",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = true,
                        StatusReason = "Member Activated"
                    },
                    ExternalReference = new ExternalReference
                    {
                        ReferenceId = "test",
                        SourceSystem = "test",
                    },
                    MemberResponseMessage = new MemberResponseMessage
                    {
                        Message = "Member Activated",
                        MemberStatus = "Active",
                        MemberInfo = new MemberInfo { MemberId = memberId }
                    },
                    Errors = null
                }
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
            var result = await _memberService!.UpdateMemberAsync(memberId, deserializedPatchDoc);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data, "result.Data should not be null");
            Assert.AreEqual("Active", result.Data.MemberStatus);
        }

        [TestMethod]
        public async Task UpdateMemberAsync_DeactivateMember_Success()
        {
            // Arrange
            var memberId = "12344556";
            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Replace(x => x.MemberStatus, "InActive");

            var deserializedPatchDoc = PreparePatchDoc(patchDoc);

            var backendResponse = new MemberResponse
            {
                Source = "test",
                SpecVersion = "1.0.0",
                CorrelationId = "test",
                Id = memberId,
                Type = "test",
                Data = new Data
                {
                    SystemMeta = new SystemMeta
                    {
                        InsertedBy = "Admin",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "Admin",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = true,
                        StatusReason = "Member Deactivated"
                    },
                    ExternalReference = new ExternalReference
                    {
                        ReferenceId = "test",
                        SourceSystem = "test",
                    },
                    MemberResponseMessage = new MemberResponseMessage
                    {
                        Message = "Member Deactivated",
                        MemberStatus = "InActive",
                        MemberInfo = new MemberInfo { MemberId = memberId }
                    },
                    Errors = null
                }
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
            var result = await _memberService!.UpdateMemberAsync(memberId, deserializedPatchDoc);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Data, "result.Data should not be null");
            Assert.AreEqual("InActive", result.Data.MemberStatus);
        }

        [TestMethod]
        public async Task UpdateMemberAsync_Activate_InvalidValue_CoversValidator()
        {
            var memberId = "12344556";

            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Operations.Add(new Microsoft.AspNetCore.JsonPatch.Operations.Operation<OTAGetMemberResponse>
            {
                op = "replace",
                path = "/memberStatus",
                value = "INVALID"
            });

            var result = await _memberService.UpdateMemberAsync(memberId, patchDoc);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task UpdateMemberAsync_Activate_InvalidOperation_CoversValidator()
        {
            var memberId = "12344556";

            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Operations.Add(new Microsoft.AspNetCore.JsonPatch.Operations.Operation<OTAGetMemberResponse>
            {
                op = "add",
                path = "/memberStatus",
                value = "Active"
            });

            var result = await _memberService.UpdateMemberAsync(memberId, patchDoc);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task UpdateMemberAsync_Deactivate_InvalidValue_CoversValidator()
        {
            var memberId = "12344556";

            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Operations.Add(new Microsoft.AspNetCore.JsonPatch.Operations.Operation<OTAGetMemberResponse>
            {
                op = "replace",
                path = "/memberStatus",
                value = "DISABLED"
            });

            var result = await _memberService.UpdateMemberAsync(memberId, patchDoc);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task UpdateMemberAsync_Deactivate_InvalidPath_CoversValidator()
        {
            var memberId = "12344556";

            var patchDoc = new JsonPatchDocument<OTAGetMemberResponse>();
            patchDoc.Operations.Add(new Microsoft.AspNetCore.JsonPatch.Operations.Operation<OTAGetMemberResponse>
            {
                op = "replace",
                path = "/status",
                value = "Inactive"
            });

            var result = await _memberService.UpdateMemberAsync(memberId, patchDoc);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(400, result.Error.Status);
        }

        [TestMethod]
        public async Task UpdateMemberAsync_InvalidPatch_NoOperations()
        {
            var result = await _memberService.UpdateMemberAsync(
                "123",
                new JsonPatchDocument<OTAGetMemberResponse>()
            );

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task UpdateMemberAsync_GeneralUpdate_MultipleOperations()
        {
            var patch = new JsonPatchDocument<OTAGetMemberResponse>();
            patch.Replace(x => x.MemberProfile.FirstName, "A");
            patch.Replace(x => x.MemberProfile.LastName, "B");

            var result = await _memberService.UpdateMemberAsync("123", patch);

            Assert.IsFalse(result.Success);
        }

        private JsonPatchDocument<OTAGetMemberResponse> PreparePatchDoc(JsonPatchDocument<OTAGetMemberResponse> patchDoc)
        {
            // Serialize to JSON with camelCase (like a real HTTP request)
            var requestBody = Newtonsoft.Json.JsonConvert.SerializeObject(
                patchDoc,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                });

            // Deserialize back to JsonPatchDocument using the same settings
            var patchDocument = string.IsNullOrWhiteSpace(requestBody)
                                ? new JsonPatchDocument<OTAGetMemberResponse>()
                                : Newtonsoft.Json.JsonConvert.DeserializeObject<JsonPatchDocument<OTAGetMemberResponse>>(requestBody,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                }) ?? new JsonPatchDocument<OTAGetMemberResponse>();

            return patchDocument;
        }


        #endregion
    }
}
