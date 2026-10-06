using Microsoft.Extensions.Logging;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Exceptions;
using OneExchangeOrderService.Domain.Helpers;
using OneExchangeOrderService.Domain.Models.V2.Cancellation.Request;
using OneExchangeOrderService.Domain.Models.V2.Cancellation.Response;
using OneExchangeOrderService.Domain.Models.V2.Reservation.Request;
using OneExchangeOrderService.DTO.Models.InventoryUpdate.Request;
using System.Diagnostics;
using System.Text.Json;
using System.Globalization;

namespace OneExchangeOrderService.Application.Interfaces.Logic
{
    public partial class OrderService
    {
        public async Task<CancellationResponse> CancelReservationAsync(string bookingID, CancellationRequest request, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("{Prefix} [START] CancelReservationAsync for bookingID={BookingID}.", OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, bookingID);

            var payloadJson = JsonSerializer.Serialize(request, IndentedJsonOptions);
            _logger.LogInformation("{Prefix} Incoming CancellationRequest payload:\n{Payload}", OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, payloadJson);

            try
            {
                // 1. Fetch and mark order as PENDING_CANCELLATION
                var order = await _uow.Orders.GetOrderByExternalReferenceIdAsync(bookingID, ct);
                if (order == null)
                {
                    _logger.LogWarning("{Prefix} Order not found for bookingID={BookingID}.", OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, bookingID);
                    throw new DataNotFoundException($"Order not found for bookingID={bookingID}");
                }
                _logger.LogInformation("{Prefix} Fetched order: orderId={OrderId}, orderNumber={OrderNumber}, currentStatus={OrderStatus}.",
                    OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, order.Id, order.OrderNumber, order.OrderStatus);

                // 2. Call Boomi Connector for cancellation
                _logger.LogInformation("{Prefix} [Boomi][START] Calling Boomi connector for cancellation, orderNumber={OrderNumber}.",
                    OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, order.OrderNumber);

                var boomiSuccess = false;
                string? boomiError = null;
                string? boomiErrorCode = null;
                try
                {
                    var boomiRequest = BuildBoomiCancellationRequest(order, request);
                    var boomiPayloadJson = JsonSerializer.Serialize(boomiRequest, IndentedJsonOptions);
                    _logger.LogInformation("{Prefix} [Boomi] Outgoing CancellationRequest payload:\n{Payload}", OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, boomiPayloadJson);

                    var boomiResponse = await _boomiConnector.CancellationConnectorCancelAsync(boomiRequest);

                    var boomiResponseJson = JsonSerializer.Serialize(boomiResponse, IndentedJsonOptions);
                    _logger.LogInformation("{Prefix} [Boomi] Incoming CancellationResponse payload:\n{Payload}", OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, boomiResponseJson);

                    boomiSuccess = boomiResponse?.Data?.CancellationStatus?.Equals("Cancelled", StringComparison.Ordinal) ?? false;
                    if (!boomiSuccess)
                    {
                        boomiError = boomiResponse?.Data?.Error?.ErrorMessage ?? "Unknown error from Boomi connector";
                        boomiErrorCode = boomiResponse?.Data?.Error?.ErrorCode ?? "Unknown error code from Boomi connector";
                    }
                    _logger.LogInformation("{Prefix} [Boomi][END] Boomi cancellation call completed for orderNumber={OrderNumber}. Success={Success}",
                        OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, order.OrderNumber, boomiSuccess);
                }
                catch (Exception ex)
                {
                    boomiError = ex.Message;
                    boomiErrorCode = "Unknown error code from Boomi connector";
                    _logger.LogError(ex, "{Prefix} [Boomi][EXCEPTION] Boomi cancellation failed for order {OrderNumber}.",
                        OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, order.OrderNumber);
                }
                if (!boomiSuccess)
                {
                    _logger.LogWarning("{Prefix} Boomi cancellation failed: {Error}. Rolling back transaction.",
                        OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, boomiError);
                    var failureResponse = BuildCancellationFailureResponse(request, order, boomiErrorCode, boomiError ?? "Boomi cancellation failed.");
                    var failureRespJson = JsonSerializer.Serialize(failureResponse, IndentedJsonOptions);
                    _logger.LogInformation("{Prefix} Failure CancellationResponse payload:\n{Response}", OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, failureRespJson);
                    return failureResponse;
                }

                // 3. Update inventory (release unit)
                _logger.LogInformation("{Prefix} [Inventory][START] Releasing inventory for orderNumber={OrderNumber}.",
                    OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, order.OrderNumber);

                var inventorySuccess = false;
                string? inventoryError = null;
                try
                {
                    var inventoryRequest = BuildInventoryReleaseRequest(order);
                    var inventoryPayloadJson = JsonSerializer.Serialize(inventoryRequest, IndentedJsonOptions);
                    _logger.LogInformation("{Prefix} [Inventory] Outgoing InventoryRequest payload:\n{Payload}", OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, inventoryPayloadJson);

                    var inventoryResponse = await _inventoryService.UpdateInventoryAsync(inventoryRequest);

                    var inventoryResponseJson = JsonSerializer.Serialize(inventoryResponse, IndentedJsonOptions);
                    _logger.LogInformation("{Prefix} [Inventory] Incoming InventoryResponse payload:\n{Payload}", OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, inventoryResponseJson);

                    inventorySuccess = inventoryResponse?.Success ?? false;
                    if (!inventorySuccess)
                    {
                        inventoryError = inventoryResponse?.Message ?? "Unknown error from inventory service";
                    }

                    _logger.LogInformation("{Prefix} [Inventory][END] Inventory release call completed for orderNumber={OrderNumber}. Success={Success}",
                        OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, order.OrderNumber, inventorySuccess);
                }
                catch (Exception ex)
                {
                    inventoryError = ex.Message;
                    _logger.LogError(ex, "{Prefix} [Inventory][EXCEPTION] Inventory release failed for order {OrderNumber}.",
                        OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, order.OrderNumber);
                }
                if (!inventorySuccess)
                {
                    _logger.LogWarning("{Prefix} Inventory release failed: {Error}. Rolling back transaction.",
                        OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, inventoryError);
                    var failureResponse = BuildCancellationFailureResponse(request, order, "INVENTORY_RELEASE_FAILED", inventoryError ?? "Inventory release failed.");
                    var failureRespJson = JsonSerializer.Serialize(failureResponse, IndentedJsonOptions);
                    _logger.LogInformation("{Prefix} Failure CancellationResponse payload:\n{Response}", OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, failureRespJson);
                    return failureResponse;
                }

                // 4. Create OrderReturn record
                _logger.LogInformation("{Prefix} Creating OrderReturn record for orderId={OrderId}.",
                    OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, order.Id);

                var objOrderReturn = new OrderReturn
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ReturnNumber = "RET" + (await CommonHelper.GenerateNanoId()),
                    ReturnDate = DateTime.UtcNow,
                    TenantId = order.TenantId,
                    ReturnFeeCurrency = order.Currency,
                    ReturnFeeAmount = order.OrderFee,
                    Memo = request.Data?.CancellationReason,
                    ExternalSourceSystem = request.Data?.ExternalReference?.TargetSystem,
                    ExternalReferenceId = request.Data?.ExternalReference?.TargetReferenceId,
                    InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy,
                    UpdatedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy,
                    InsertedDateTime = DateTime.UtcNow,
                    UpdatedDateTime = DateTime.UtcNow,
                    WaiveFee = false
                };
                await _uow.Orders.CreateOrderReturnsAsync(objOrderReturn);
                _logger.LogInformation("{Prefix} OrderReturn record created: returnNumber={ReturnNumber}, orderId={OrderId}.",
                    OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, objOrderReturn.ReturnNumber, order.Id);

                // 5. Mark order as CANCELLED
                order.OrderStatus = "CANCELLED";
                await _uow.SaveAsync(ct);
                _logger.LogInformation("{Prefix} Order status updated to CANCELLED for orderId={OrderId}.",
                    OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, order.Id);

                _logger.LogInformation("{Prefix} Transaction committed in {ElapsedMs} ms.",
                    OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, sw.ElapsedMilliseconds);

                // 6. Build and return response
                var resp = BuildCancellationResponse(request, order);
                var respJson = JsonSerializer.Serialize(resp, IndentedJsonOptions);
                _logger.LogInformation("{Prefix} Success CancellationResponse payload:\n{Response}",
                    OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, respJson);

                sw.Stop();
                _logger.LogInformation("{Prefix} [END] CancelReservationAsync completed in {ElapsedMs} ms.",
                    OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, sw.ElapsedMilliseconds);

                return resp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} [EXCEPTION] Failed to cancel reservation ce.id={Id}", OneExchangeOrderServiceConstants.OrderServiceCancellationPrefix, request?.Id);
                throw;
            }
        }

        private static CancellationResponse BuildCancellationResponse(CancellationRequest req, Order order)
        {
            var now = DateTime.UtcNow;

            var resp = new CancellationResponse
            {
                Id = Guid.NewGuid().ToString(),
                Source = req.Source,
                Specversion = "1.0",
                Type = "Cancellation-Response",
                Datacontenttype = "application/json",
                Dataschema = "https://schemas.onetruth.example/reservations/response/2020-12",
                Subject = $"cancellation/{req.Data.CancellationStatus ?? "NOT_CANCELLED"}",
                Time = now,
                Ispartial = false,
                Data = new CancelItemResponse
                {
                    BookingId = order.ExternalReferenceId,
                    SystemMeta = new SystemMeta
                    {
                        TenantID = req.Data.SystemMeta.TenantID,
                        InsertedDateTime = now,
                        InsertedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy,
                        UpdatedDateTime = now,
                        UpdatedBy = OneExchangeOrderServiceConstants.PostOrderInsertedBy,
                        Status = true,
                        StatusReason = "CANCELLED"
                    },
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = req.Data.ExternalReference.SourceSystem,
                        TargetSystem = req.Data.ExternalReference.TargetSystem,
                        SourceReferenceId = req.Data.ExternalReference.SourceReferenceId,
                        TargetReferenceId = order.ExternalReferenceId
                    },
                    CancellationReason = req.Data.CancellationReason,
                    CancellationDate = now,
                    CancellationStatus = req.Data.CancellationStatus,
                    RefundedAmount = req.Data.RefundedAmount,
                    BookingItems = req.Data.BookingItems,
                }
            };
            return resp;
        }

        private static CancellationRequest BuildBoomiCancellationRequest(Order order, CancellationRequest req) => new()
        {
            Id = req.Id,
            Source = req.Source,
            Specversion = req.Specversion,
            Type = "Cancel-Booking",
            Datacontenttype = req.Datacontenttype,
            Dataschema = req.Dataschema,
            Subject = req.Subject,
            Time = req.Time,
            Data = new CancelItemRequest
            {
                SystemMeta = new SystemMeta
                {
                    TenantID = req.Data.SystemMeta.TenantID,
                    InsertedDateTime = DateTime.UtcNow,
                    InsertedBy = "order-service",
                    UpdatedDateTime = DateTime.UtcNow,
                    UpdatedBy = "order-service",
                    Status = true,
                    StatusReason = "Completed"
                },
                ExternalReference = new ExternalReference
                {
                    SourceSystem = req.Data.ExternalReference.SourceSystem,
                    TargetSystem = req.Data.ExternalReference.TargetSystem,
                    SourceReferenceId = req.Data.ExternalReference.SourceReferenceId,
                    TargetReferenceId = req.Data.ExternalReference.TargetReferenceId
                },
                BookingId = order.ExternalReferenceId,
                CancellationReason = req.Data.CancellationReason,
                CancellationDate = DateTime.UtcNow,
                CancellationStatus = req.Data.CancellationStatus,
                BookingItems =
                [
                    new BookingItem
                    {
                        GuestInfo = new GuestInfo
                        {
                            FirstName = order.CustomerFirstName,
                            LastName = order.CustomerLastName
                        },
                        BookingAttributes =
                        [
                            new AttributePair
                            {
                                Attribute = "PmsConfirmNumber",
                                Value = order.CustomerExternalReferenceId
                            }
                        ]
                    }
                    ]
            }
        };

        private static CancellationResponse BuildCancellationFailureResponse(CancellationRequest req, Order order, string code, string message)
        {
            var now = DateTime.UtcNow;
            return new CancellationResponse
            {
                Id = Guid.NewGuid().ToString(),
                Source = req.Source,
                Specversion = "1.0",
                Type = "Cancellation-Response",
                Datacontenttype = "application/json",
                Dataschema = "https://schemas.onetruth.example/reservations/response/2020-12",
                Subject = $"cancellation/{req.Data.CancellationStatus ?? "NOT_CANCELLED"}",
                Time = now,
                Ispartial = false,
                Data = new CancelItemResponse
                {
                    BookingId = order.ExternalReferenceId,
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
                    ExternalReference = new ExternalReference
                    {
                        SourceSystem = req.Data.ExternalReference.SourceSystem,
                        TargetSystem = req.Data.ExternalReference.TargetSystem,
                        SourceReferenceId = req.Data.ExternalReference.SourceReferenceId,
                        TargetReferenceId = order?.ExternalReferenceId
                    },
                    CancellationReason = req.Data.CancellationReason,
                    CancellationDate = now,
                    CancellationStatus = "FAILED",
                    RefundedAmount = 0,
                    BookingItems = req.Data.BookingItems,
                    Error = new ErrorInfo
                    {
                        ErrorCode = code,
                        ErrorMessage = message,
                    }
                },
            };

        }

        private static InventoryRequest BuildInventoryReleaseRequest(Order order) => new()
        {
            UnitCode = order.OrderItems.First().ItemCode,
            CheckInDate = DateTime.Parse(order.OrderItems.First().OrderItemAttributes.First(a => a.AttributeType == "CHECK_IN_DATE").AttributeValue, CultureInfo.InvariantCulture),
            CheckOutDate = DateTime.Parse(order.OrderItems.First().OrderItemAttributes.First(a => a.AttributeType == "CHECK_OUT_DATE").AttributeValue, CultureInfo.InvariantCulture),
            Quantity = 1,
            Reason = "Cancellation"
        };
    }
}
