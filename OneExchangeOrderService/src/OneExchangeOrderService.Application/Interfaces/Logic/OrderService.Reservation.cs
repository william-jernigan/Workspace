using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Entities.ExchangeProfile;
using OneExchangeOrderService.Domain.Exceptions;
using OneExchangeOrderService.Domain.Helpers;
using OneExchangeOrderService.Domain.Models.V2.Reservation.Request;
using OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Request;
using OneExchangeOrderService.Domain.Models.V2.Reservation.ReservationConnector.Response;
using OneExchangeOrderService.Domain.Models.V2.Reservation.Response;
using OneExchangeOrderService.DTO.Models.InventoryUpdate.Request;
using System.Diagnostics;
using System.Text.Json;
using DigitalAddress = OneExchangeOrderService.Domain.Entities.ExchangeProfile.DigitalAddress;
using GuaranteeAndCancelPolicy = OneExchangeOrderService.Domain.Models.V2.Reservation.Response.GuaranteeAndCancelPolicy;
using SystemMeta = OneExchangeOrderService.Domain.Models.V2.Reservation.Request.SystemMeta;
using System.Globalization;

namespace OneExchangeOrderService.Application.Interfaces.Logic
{
    public partial class OrderService
    {
        private static readonly JsonSerializerOptions IndentedJsonOptions = new()
        {
            WriteIndented = true
        };
        public async Task<ReservationResponse> CreateReservationAsync(ReservationRequest request, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("{Prefix} [START] CreateReservationAsync received.", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix);

            var payloadJson = JsonSerializer.Serialize(request, IndentedJsonOptions);
            _logger.LogInformation("{Prefix} Incoming ReservationRequest payload:\n{Payload}", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, payloadJson);

            if (request.Data is null || request.Data.BookingItems is null || request.Data.BookingItems.Count == 0)
            {
                throw new ArgumentException($"{OneExchangeOrderServiceConstants.OrderServiceReservationPrefix} bookingItems is required");
            }

            var tenantCode = request.Data.SystemMeta.TenantID;
            var booking = request.Data.BookingItems[0];

            //1) Call external reservation connector

            _logger.LogInformation("{Prefix} [Connector][START] Calling ReservationConnector for orderNumber={OrderNumber}.",
                OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, "N/A");

            ReservationConnectorResponse extResp;
            string? bookingId = null;
            string status;
            string? failCode = null;
            string? failMsg = null;
            string? pms = null;

            var extReq = BuildReservationConnectorRequest(request, booking);
            var extReqJson = JsonSerializer.Serialize(extReq, IndentedJsonOptions);
            _logger.LogInformation("{Prefix} [Connector] Outgoing ReservationConnectorRequest payload:\n{Payload}",
                OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, extReqJson);

            try
            {
                extResp = await _boomiConnector.ReservationConnectorCreateAsync(extReq);
                var extRespJson = JsonSerializer.Serialize(extResp, IndentedJsonOptions);
                _logger.LogInformation("{Prefix} [Connector] Incoming ReservationConnectorResponse payload:\n{Payload}",
                    OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, extRespJson);

                bookingId = extResp?.Data?.BookingId;
                pms = extResp?.Data?.ExternalReference.TargetReferenceId ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(bookingId))
                {
                    status = "CONFIRMED";
                }
                else
                {
                    failCode = extResp?.Data?.Error?.ErrorCode;
                    failMsg = extResp?.Data?.Error?.ErrorMessage;
                    _logger.LogInformation("{Prefix} [Connector][END] ReservationConnector call completed for orderNumber={OrderNumber}. Success={Success}",
                        OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, "N/A", false);

                    return BuildFailureResponse(request, booking, failCode ?? "BOOKING_FAILED", failMsg ?? "Boomi Connector failed.");
                }

                _logger.LogInformation("{Prefix} [Connector][END] ReservationConnector call completed for orderNumber={OrderNumber}. Success={Success}",
                    OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, "N/A", true);
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException || tcEx.Message.Contains("HttpClient.Timeout"))
            {
                _logger.LogError(tcEx,
                    "{Prefix} [Connector][TIMEOUT] External reservation connector timed out after 100 seconds for order {OrderNumber}",
                    OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, "N/A");

                throw new BoomiTimeoutException("Boomi Timeout");
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx,
                    "{Prefix} [Connector][HTTP_ERROR] HTTP request failed for order {OrderNumber}",
                    OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, "N/A");

                throw new BoomiTimeoutException("Boomi Timeout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{Prefix} [Connector][EXCEPTION] External reservation failed for order {OrderNumber}",
                    OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, "N/A");

                return BuildFailureResponse(request, booking, "CONNECTOR_EXCEPTION", ex.Message);
            }

            // 2) TRANSACTION STARTS HERE

            Order order;
            try
            {

                order = await PersistPendingOrderAsync(request, tenantCode, booking);
                _logger.LogInformation("{Prefix} Order created in {ElapsedMs} ms.",
                    OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, sw.ElapsedMilliseconds);

                order.OrderStatus = status;
                order.ExternalReferenceId = bookingId;
                order.CustomerExternalReferenceId = pms;
                await _uow.SaveAsync(ct);
                _logger.LogInformation("{Prefix} Transaction committed successfully", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix);
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is SqlException sqlEx)
                {
                    _logger.LogError(dbEx, "{Prefix} SQL ERROR Number={Number}, State={State}, Message={Message}", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, sqlEx.Number, sqlEx.State, sqlEx.Message);
                }
                else
                {
                    _logger.LogError(dbEx, "{Prefix} DB UPDATE ERROR: {Message}", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, dbEx.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} [EXCEPTION] Failed to create reservation ce.id={Id}", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, request?.Id);
                throw;
            }

            try
            {
                if (string.Equals(status, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("{Prefix} [Inventory][START] Inventory update for booking confirmation, orderNumber={OrderNumber}.",
                        OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, order.OrderNumber);


                    // 3) Inventory update

                    var updateInventoryRequest = BuildInventoryReserveRequest(booking);
                    var updateInventoryRequestJson = JsonSerializer.Serialize(updateInventoryRequest, IndentedJsonOptions);
                    _logger.LogInformation("{Prefix} [Inventory] Outgoing InventoryRequest payload:\n{Payload}",
                        OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, updateInventoryRequestJson);

                    var updateInventoryResponse = await _inventoryService.UpdateInventoryAsync(updateInventoryRequest);
                    var updateInventoryResponseJson = JsonSerializer.Serialize(updateInventoryResponse, IndentedJsonOptions);
                    _logger.LogInformation("{Prefix} [Inventory] Incoming InventoryResponse payload:\n{Payload}",
                        OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, updateInventoryResponseJson);

                    if (!updateInventoryResponse.Success)
                    {
                        _logger.LogWarning("{Prefix} Inventory update failed after booking confirmation: {Message}",
                            OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, updateInventoryResponse.Message);

                        return BuildFailureResponse(request, booking, "INVENTORY_FAILED", "Inventory Update failed.");
                    }

                    _logger.LogInformation("{Prefix} Transaction committed successfully", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix);

                    var successResponse = BuildResponseEnvelope(request, order, booking);
                    return successResponse;
                }

                return BuildFailureResponse(request, booking, failCode ?? "BOOKING_FAILED", failMsg ?? "External Service failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} [EXCEPTION] Failed to create reservation ce.id={Id}", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, request?.Id);
                throw;
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("{Prefix} [END] CreateReservationAsync completed in {ElapsedMs} ms.",
                    OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, sw.ElapsedMilliseconds);
            }
        }

        private async Task<Order> PersistPendingOrderAsync(ReservationRequest request, string tenantCode, BookingItemRequest booking)
        {
            //TODO, check the mapping as per Actual Reservation payload
            var orderId = Guid.NewGuid();
            var nanoId = CommonHelper.GenerateNanoId();
            var orderNumber = "ORD" + nanoId.Result;
            var billToProfileId = Guid.NewGuid();
            var guestProfileId = Guid.NewGuid();
            var itemCode = request.Data.BookingItems?.FirstOrDefault()?.ItemCode;
            var partnerCode = CommonHelper.GetCodeFromItemCode(itemCode!, OneExchangeOrderServiceConstants.PartnerCodeString);
            var tenantId = Guid.Parse("9A7B6C5D-1E2F-4A3B-8C9D-0E1F2A3B4C5D"); // TODO get logic to fetch from Database
            var tradingAgreementId = Guid.Parse("BEEFCAFE-8BAD-4F00-ABCD-00FF11EE22DD"); // TODO get logic to fetch from Database

            _logger.LogInformation("{Prefix} Order Details: tenantCode={TenantCode}, partnerCode={PartnerCode}, tenantId={TenantId}, tradingAgreementId={TradingAgreementId}, orderNumber={OrderNumber}",
            OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, tenantCode, partnerCode, tenantId, tradingAgreementId, orderNumber);

            // --- CUSTOMER (billTo) PROFILE
            var billToProfile = new Profile
            {
                Id = billToProfileId,
                ProfileType = "PERSON",
                FirstName = booking.GuestInfo.FirstName,
                LastName = booking.GuestInfo.LastName,
                ExternalSourceSystem = "HGV",
                ExternalReferenceId = booking.MemberID,
                InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy,
                Addresses = (booking.GuestInfo.PostalAddresses ?? [])
                .Select((a, idx) => new Address
                {
                    Id = Guid.NewGuid(),
                    ProfileId = billToProfileId,
                    Sequence = a.Sequence ?? (idx + 1),
                    AddressType = ToAddressType(a.AddressType),
                    AddressPurpose = "PRIMARY",
                    Line1 = a.Line1,
                    Line2 = a.Line2,
                    City = a.City,
                    State = a.State,
                    Postalcode = a.PostalCode,
                    Country = a.Country,
                    InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy
                })
                .ToList(),

                Phones = (booking.GuestInfo.PhoneNumbers ?? [])
                .Select((p, idx) => new Phone
                {
                    Id = Guid.NewGuid(),
                    ProfileId = billToProfileId,
                    Sequence = p.Sequence ?? (idx + 1),
                    PhoneType = ToPhoneType(p.PhoneType),
                    PhonePurpose = "PRIMARY",
                    Number = p.Number,
                    InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy
                })
                .ToList(),

                DigitalAddresses = (booking.GuestInfo.DigitalAddresses ?? [])
                .Select((d, idx) => new DigitalAddress
                {
                    Id = Guid.NewGuid(),
                    ProfileId = billToProfileId,
                    Sequence = d.Sequence ?? (idx + 1),
                    DigitalAddressType = d.DigitalAddressType.ToUpperInvariant(), // EMAIL
                    DigitalAddress1 = d.DigitalAddressValue,
                    InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy
                })
                .ToList()
            };

            _logger.LogInformation("{Prefix} Creating bill-to profile: profileId={ProfileId}, name={First} {Last}, phones={PhoneCount}, emails={EmailCount}, addresses={AddrCount}",
                OneExchangeOrderServiceConstants.OrderServiceReservationPrefix,
              billToProfileId, billToProfile.FirstName, billToProfile.LastName,
              billToProfile.Phones?.Count ?? 0,
              billToProfile.DigitalAddresses?.Count ?? 0,
              billToProfile.Addresses?.Count ?? 0);

            await _uow.Profiles.CreateProfileAsync(billToProfile);

            // --- ORDER
            var order = new Order
            {
                Id = orderId,
                //TenantId = OrganizationHelper.GetTenantGuid(tenantCode), // TODO check this logic
                //TradingAgreementId = OrganizationHelper.GetTradingAgreementGuid(request.Data.ExternalReference.TargetSystem), // TODO check this logic
                TenantId = tenantId,
                TradingAgreementId = tradingAgreementId,
                OrderType = "BOOKING",
                OrderNumber = orderNumber,
                OrderStatus = "PENDING",
                ExternalSourceSystem = request.Data.ExternalReference.TargetSystem,
                ExternalReferenceId = request.Data.ExternalReference.TargetReferenceId,
                OrderDate = DateTime.UtcNow,
                BillToProfileId = billToProfileId,

                CustomerExternalSourceSystem = "HGV",
                CustomerExternalReferenceId = booking.MemberID,
                CustomerPhoneNumber = billToProfile.Phones?.FirstOrDefault()?.Number,
                CustomerEmailAddress = billToProfile.DigitalAddresses?.FirstOrDefault()?.DigitalAddress1,
                CustomerFirstName = booking.GuestInfo.FirstName,
                CustomerLastName = booking.GuestInfo.LastName,

                Currency = booking.PriceLevel?.Currency ?? "USD",
                Subtotal = booking.PriceLevel?.TotalPrice ?? 0,
                TaxCurrency = booking.PriceLevel?.Currency ?? "USD",
                TaxAmount = 0,
                FeeCurrency = booking.PriceLevel?.Currency ?? "USD",
                OrderFee = GetOrderFee(booking),
                WaiveFee = false,
                Memo = BuildMemo(request),
                InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy
            };
            _logger.LogInformation("{Prefix} Order init: orderId={OrderId}, orderNumber={OrderNumber}, status={Status}, currency={Currency}, subtotal={Subtotal}, fee={Fee}",
            OneExchangeOrderServiceConstants.OrderServiceReservationPrefix,
            order.Id, order.OrderNumber, order.OrderStatus, order.Currency, order.Subtotal, order.OrderFee);

            // --- ORDER ITEM
            var orderItemId = Guid.NewGuid();
            var propertyName = booking.Name ?? booking.ParentCode ?? booking.ItemCode;

            var orderItem = new OrderItem
            {
                Id = orderItemId,
                TenantId = order.TenantId,
                OrderId = order.Id,
                ItemId = Guid.NewGuid(),
                ItemCode = booking.ItemCode,
                Sequence = 1,
                ItemDescription = propertyName,
                Quantity = 1,
                UnitPrice = booking.PriceLevel?.TotalPrice ?? 0,
                UnitPriceCurrency = booking.PriceLevel?.Currency ?? "USD",
                TaxCurrency = booking.PriceLevel?.Currency ?? "USD",
                TaxAmount = 0,
                Memo = booking.BookingComments?.FirstOrDefault()?.Value,
                InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy,
                OrderItemAttributes = BuildOrderItemAttributes(orderItemId, booking, order.TenantId)
            };

            order.OrderItems = [orderItem];
            _logger.LogInformation("{Prefix} OrderItem: orderItemId={OrderItemId}, itemCode={ItemCode}, desc={Desc}, unitPrice={UnitPrice}, attributes={AttrCount}",
            OneExchangeOrderServiceConstants.OrderServiceReservationPrefix,
            orderItemId, orderItem.ItemCode, orderItem.ItemDescription, orderItem.UnitPrice, orderItem.OrderItemAttributes?.Count ?? 0);

            // --- ORDER GUEST
            order.OrderGuests =
                [
                    new()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        TenantId = order.TenantId,
                        ProfileId = billToProfileId, // same as primary guest
                        IsPrimary = true,
                        InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy
                    }
                ];

            await _uow.Orders.CreateOrderAsync(order);
            _logger.LogInformation("{Prefix} Order persisted (orderId={OrderId}, orderNumber={OrderNumber}).", OneExchangeOrderServiceConstants.OrderServiceReservationPrefix, order.Id, order.OrderNumber);
            return order;
        }

        private static string ToAddressType(string? from) => string.Equals(from, "business", StringComparison.OrdinalIgnoreCase) ? "BUSINESS" :
            string.Equals(from, "residence", StringComparison.OrdinalIgnoreCase) ? "HOME" :
            "HOME";

        private static string ToPhoneType(string from) =>
            string.Equals(from, "mobile", StringComparison.OrdinalIgnoreCase) ? "MOBILE" :
            string.Equals(from, "work", StringComparison.OrdinalIgnoreCase) ? "BUSINESS" :
            "HOME";

        private static decimal GetOrderFee(BookingItemRequest bi)
        {
            var feeAttr = bi.PriceLevel?.PriceAttributes?.FirstOrDefault(a => a.Attribute.Equals("feesTotal", StringComparison.OrdinalIgnoreCase));
            return feeAttr != null && decimal.TryParse(feeAttr.Value, out var v) ? v : 0m;
        }

        private static string BuildMemo(ReservationRequest req)
        {
            var who = req.Data?.SystemMeta?.InsertedBy ?? OneExchangeOrderServiceConstants.PostOrderInsertedBy;
            var when = (req.Time ?? DateTime.UtcNow).ToString("u");
            var firstComment = req.Data?.BookingItems?.FirstOrDefault()?.BookingComments?.FirstOrDefault()?.Value;
            return $"{when}|{firstComment}|{who}";
        }

        private static List<OrderItemAttribute> BuildOrderItemAttributes(Guid orderItemId, BookingItemRequest bi, Guid tenantId)
        {
            var list = new List<OrderItemAttribute>();
            var seq = 0;
            list.Add(new OrderItemAttribute { Id = Guid.NewGuid(), TenantId = tenantId, OrderItemId = orderItemId, AttributeType = "CHECK_IN_DATE", AttributeValue = bi.StartDate.ToString(OneExchangeOrderServiceConstants.IsoDateFormat, CultureInfo.InvariantCulture), Sequence = seq++, InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy });
            list.Add(new OrderItemAttribute { Id = Guid.NewGuid(), TenantId = tenantId, OrderItemId = orderItemId, AttributeType = "CHECK_OUT_DATE", AttributeValue = bi.EndDate.ToString(OneExchangeOrderServiceConstants.IsoDateFormat, CultureInfo.InvariantCulture), Sequence = seq++, InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy });
            list.Add(new OrderItemAttribute { Id = Guid.NewGuid(), TenantId = tenantId, OrderItemId = orderItemId, AttributeType = "NUM_ADULTS", AttributeValue = (bi.GuestCounts.Adults ?? 0).ToString(CultureInfo.InvariantCulture), Sequence = seq++, InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy });
            list.Add(new OrderItemAttribute { Id = Guid.NewGuid(), TenantId = tenantId, OrderItemId = orderItemId, AttributeType = "NUM_CHILDREN", AttributeValue = (bi.GuestCounts.Children ?? 0).ToString(CultureInfo.InvariantCulture), Sequence = seq++, InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy });
            list.Add(new OrderItemAttribute { Id = Guid.NewGuid(), TenantId = tenantId, OrderItemId = orderItemId, AttributeType = "UNIT_TYPE", AttributeValue = bi.ItemClass ?? "UnitType", Sequence = seq++, InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy });

            foreach (var a in bi.BookingAttributes ?? Enumerable.Empty<Domain.Models.V2.Reservation.Request.AttributePair>())
            {
                list.Add(new OrderItemAttribute { Id = Guid.NewGuid(), TenantId = tenantId, OrderItemId = orderItemId, AttributeType = a.Attribute.ToUpperInvariant(), AttributeValue = a.Value, Sequence = seq++, InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy });
            }

            return list;
        }

        private static ReservationResponse BuildResponseEnvelope(ReservationRequest req, Order order, BookingItemRequest bi)
        {
            var now = DateTime.UtcNow;

            var resp = new ReservationResponse
            {
                Id = order.ExternalReferenceId,
                Source = req.Source,
                Specversion = "1.0",
                Type = "Reservation-Response",
                Datacontenttype = "application/json",
                Dataschema = "https://schemas.onetruth.example/reservations/response/2020-12",
                Subject = $"reservation/{bi.ParentCode ?? bi.ItemCode}",
                Time = now,
                Ispartial = false,
                Data = new ReservationResponseData
                {
                    SystemMeta = new SystemMeta
                    {
                        TenantID = req.Data.SystemMeta.TenantID,
                        InsertedDateTime = now,
                        InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy,
                        UpdatedDateTime = now,
                        UpdatedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy,
                        Status = true,
                        StatusReason = "Booked"
                    },
                    ExternalReference = new Domain.Models.V2.Reservation.Request.ExternalReference
                    {
                        SourceSystem = req.Data.ExternalReference.SourceSystem,
                        TargetSystem = req.Data.ExternalReference.TargetSystem,
                        SourceReferenceId = req.Data.ExternalReference.SourceReferenceId,
                        TargetReferenceId = order.ExternalReferenceId
                    },
                    BookingStatus = "CONFIRMED",
                    BookingId = order.ExternalReferenceId,
                    PaymentStatus = "PAID", // adjust per your payment creation
                    CreatedAt = now,
                    BookingItems =
                    [
                        new()
                        {
                            ItemCode = bi.ItemCode,
                            ItemClass = bi.ItemClass,
                            ParentCode = bi.ParentCode,
                            Name = bi.Name,
                            Description = bi.Description,
                            ItemAttributes = bi.ItemAttributes,
                            MemberID = bi.MemberID,
                            StartDate = bi.StartDate,
                            EndDate = bi.EndDate,
                            WeekNumber = bi.WeekNumber,
                            Action = bi.Action,
                            GuestCounts = bi.GuestCounts,
                            IsOwnerBooking = bi.IsOwnerBooking,
                            GuestInfo = bi.GuestInfo,
                            PriceLevel = bi.PriceLevel,
                            BookingAttributes = bi.BookingAttributes,
                            BookingComments = bi.BookingComments
                        }
                    ],
                    Error = new ErrorInfo(), // empty per success
                    GuaranteeAndCancelPolicy = new GuaranteeAndCancelPolicy
                    {
                        GuaranteeType = bi.BookingAttributes?.FirstOrDefault(a => a.Attribute.Equals("guaranteeType", StringComparison.OrdinalIgnoreCase))?.Value,
                        GuaranteeDescription = bi.BookingAttributes?.FirstOrDefault(a => a.Attribute.Equals("guaranteeDeadline", StringComparison.OrdinalIgnoreCase))?.Value is string d
                            ? $"Credit card guarantee required by {d}"
                            : null,
                        CancelPenalties = [] // populate if you stored policy in attributes
                    }
                }
            };
            return resp;
        }
        private static ReservationResponse BuildFailureResponse(ReservationRequest req, BookingItemRequest bi, string code, string message)
        {
            var now = DateTime.UtcNow;
            return new ReservationResponse
            {
                Id = Guid.NewGuid().ToString(),
                Source = req.Source,
                Specversion = "1.0",
                Type = "Reservation-Response",
                Datacontenttype = "application/json",
                Dataschema = "https://schemas.onetruth.example/reservations/response/2020-12",
                Subject = $"reservation/{bi.ParentCode ?? bi.ItemCode}",
                Time = now,
                Ispartial = false,
                Data = new ReservationResponseData
                {
                    SystemMeta = new SystemMeta
                    {
                        TenantID = req.Data.SystemMeta.TenantID,
                        InsertedDateTime = now,
                        InsertedBy = "order-service",
                        UpdatedDateTime = now,
                        UpdatedBy = "order-service",
                        Status = false,
                        StatusReason = "Failed"
                    },
                    ExternalReference = new Domain.Models.V2.Reservation.Request.ExternalReference
                    {
                        SourceSystem = req.Data.ExternalReference.SourceSystem,
                        TargetSystem = req.Data.ExternalReference.TargetSystem,
                        SourceReferenceId = req.Data.ExternalReference.SourceReferenceId,
                        TargetReferenceId = null
                    },
                    BookingStatus = "FAILED",
                    BookingId = null,
                    PaymentStatus = "NOT_PAID",
                    CreatedAt = now,
                    BookingItems =
                    [
                        new()
                        {
                            ItemCode = bi.ItemCode,
                            ItemClass = bi.ItemClass,
                            ParentCode = bi.ParentCode,
                            Name = bi.Name,
                            Description = bi.Description,
                            ItemAttributes = bi.ItemAttributes,
                            MemberID = bi.MemberID,
                            StartDate = bi.StartDate,
                            EndDate = bi.EndDate,
                            WeekNumber = bi.WeekNumber,
                            Action = "Fail",
                            GuestCounts = bi.GuestCounts,
                            IsOwnerBooking = bi.IsOwnerBooking,
                            GuestInfo = bi.GuestInfo,
                            PriceLevel = bi.PriceLevel,
                            BookingAttributes = bi.BookingAttributes,
                            BookingComments = bi.BookingComments
                        }
                    ],
                    Error = new ErrorInfo
                    {
                        ErrorCode = code,
                        ErrorType = "VALIDATION",
                        ErrorMessage = message
                    },
                    GuaranteeAndCancelPolicy = new GuaranteeAndCancelPolicy
                    {
                        CancelPenalties = []
                    }
                }
            };
        }

        private static ReservationConnectorRequest BuildReservationConnectorRequest(ReservationRequest req, BookingItemRequest bi)
        {
            //TODO, check the mapping as per Reservation Connector payload
            var itemCode = req.Data.BookingItems?.FirstOrDefault()?.ItemCode;
            var unitCode = CommonHelper.GetCodeFromItemCode(itemCode!, OneExchangeOrderServiceConstants.UnitTypeString);
            var resortCode = CommonHelper.GetCodeFromItemCode(itemCode!, OneExchangeOrderServiceConstants.ResortCodeString);
            var memberTypeValue = bi.BookingAttributes?.FirstOrDefault(x => x.Attribute.Equals("memberType", StringComparison.OrdinalIgnoreCase))?.Value;
            var policyCodeValue = bi.BookingAttributes?.FirstOrDefault(x => x.Attribute.Equals("policyCode", StringComparison.OrdinalIgnoreCase))?.Value;
            var absoluteDeadLineValue = bi.BookingAttributes?.FirstOrDefault(x => x.Attribute.Equals("absoluteDeadline", StringComparison.OrdinalIgnoreCase))?.Value;
            var penalityDescriptionValue = bi.BookingAttributes?.FirstOrDefault(x => x.Attribute.Equals("penaltyDescription", StringComparison.OrdinalIgnoreCase))?.Value;
            return new ReservationConnectorRequest
            {
                Id = req.Id,
                Source = req.Source,
                Specversion = req.Specversion,
                Type = "Booking-ResortUnitType",
                Data = new ReservationConnectorRequestData
                {
                    SystemMeta = new Domain.Models.V2.Reservation.ReservationConnector.Request.SystemMeta
                    {
                        TenantID = req.Data.SystemMeta.TenantID,
                        InsertedDateTime = DateTime.UtcNow.ToString("o"),
                        InsertedBy = "admin",
                        UpdatedDateTime = DateTime.UtcNow.ToString("o"),
                        UpdatedBy = "admin",
                        Status = true,
                        StatusReason = "Active"
                    },
                    ExternalReference = new Domain.Models.V2.Reservation.ReservationConnector.Request.ExternalReference
                    {
                        SourceSystem = "OX",
                        SourceReferenceId = req.Data.ExternalReference.SourceReferenceId ?? "",
                        TargetSystem = "CLA",
                        TargetReferenceId = "CLABooking"
                    },
                    BookingItems =
            [
                new BookingItem
                {
                    ItemCode = unitCode,
                    ItemClass = "ResortUnitType",
                    ParentCode = resortCode,
                    MemberID = bi.MemberID,
                    GuestInfo = new Domain.Models.V2.Reservation.ReservationConnector.Request.GuestInfo
                    {
                        FirstName = bi.GuestInfo.FirstName,
                        LastName = bi.GuestInfo.LastName,
                    },
                    GuestCounts = new Domain.Models.V2.Reservation.ReservationConnector.Request.GuestCounts
                    {
                        Children = bi.GuestCounts.Children,
                        Adults = bi.GuestCounts.Adults,
                    },
                    StartDate = bi.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    EndDate = bi.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Action = "Confirm",
                    PriceLevel = new Domain.Models.V2.Reservation.ReservationConnector.Request.PriceLevel
                    {
                        Status = true,
                        PriceLevelName = bi.PriceLevel?.PriceLevelCode ?? "SRD",
                        Currency = bi.PriceLevel?.Currency ?? "USD",
                        TotalPrice = bi.PriceLevel?.TotalPrice,
                        PriceAttributes = bi.PriceLevel?.PriceAttributes?
                        .Where(x => !string.IsNullOrEmpty(x.Value))
                        .Select(x => new Domain.Models.V2.Reservation.ReservationConnector.Request.AttributePair
                        {
                            Attribute = x.Attribute,
                            Value = x.Value
                        })
                        .ToList() ?? []
                    },
                    BookingAttributes =
                    [
                        new() { Attribute = "GuaranteeTypeCode", Value = "22" },
                        new() { Attribute = "ProgramName", Value = "GP" },
                        new() { Attribute = "ProgramID", Value = "GP" },
                        new() { Attribute = "MemberType", Value = memberTypeValue ?? "" }
                    ],
                    CancellationAttributes =
                    [
                        new() { Attribute = "PolicyCode", Value = policyCodeValue ?? "" },
                        new() { Attribute = "AbsoluteDeadline", Value = absoluteDeadLineValue ?? "" },
                        new() { Attribute = "PenaltyDescription", Value = penalityDescriptionValue ?? "" }
                    ]
                }
            ]
                }
            };
        }

        private static InventoryRequest BuildInventoryReserveRequest(BookingItemRequest bi) => new()
        {
            UnitCode = bi.ItemCode,
            CheckInDate = bi.StartDate,
            CheckOutDate = bi.EndDate,
            Quantity = -1,
        };
    }
}
