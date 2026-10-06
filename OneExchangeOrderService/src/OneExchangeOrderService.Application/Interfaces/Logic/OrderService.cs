using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OneExchangeOrderService.Application.Eventhub;
using OneExchangeOrderService.Application.Helpers;
using OneExchangeOrderService.Application.Mapping;
using OneExchangeOrderService.Domain;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Constants.Enums;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Entities.ExchangeProfile;
using OneExchangeOrderService.Domain.Exceptions;
using OneExchangeOrderService.Domain.Extensions;
using OneExchangeOrderService.Domain.Helpers;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Models.Eventhub.BookingAlternateEmail;
using OneExchangeOrderService.Domain.Models.Eventhub.OrderCancellation;
using OneExchangeOrderService.Domain.Models.PostOrderModels;
using OneExchangeOrderService.Domain.Models.UpdateOrder.Request;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeProfile;
using OneExchangeOrderService.DTO.Request;
using OneExchangeOrderService.DTO.Response;
using OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Address = OneExchangeOrderService.Domain.Entities.ExchangeProfile.Address;
using DigitalAddress = OneExchangeOrderService.Domain.Entities.ExchangeProfile.DigitalAddress;


namespace OneExchangeOrderService.Application.Interfaces.Logic
{
    public partial class OrderService(IOrderRepository orderRepository, IOrderReturnRepository orderReturnRepository, IExchangeTransactionLogRepository transactionRepository,
        IProfileRepository profileRepository,
        ILogger<OrderService> logger, IOrderUnitOfWork unitOfWork, IOrderReservationEventPublisher eventPublisher,
        IOrderPaymentRepository orderPaymentRepository, OrderCancelEventPublisher eventCancelOrderPublisher,
        IContentfulService contentfulService, IMapper mapper, IInRuleService inRuleService,
        IInventoryServiceClient inventoryService, IBoomiConnector boomiConnector, IOneExchangeUnitOfWork uow) : IOrderService
    {
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly IOrderReturnRepository _orderReturnRepository = orderReturnRepository;
        private readonly IProfileRepository _profileRepository = profileRepository;
        private readonly IExchangeTransactionLogRepository _transactionRepository = transactionRepository;
        private readonly ILogger<OrderService> _logger = logger;
        private readonly IOrderUnitOfWork _unitOfWork = unitOfWork;
        private readonly IOrderReservationEventPublisher _eventPublisher = eventPublisher;
        private readonly IContentfulService _contentfulService = contentfulService;
        private readonly OrderCancelEventPublisher _eventCancelOrderPublisher = eventCancelOrderPublisher;
        private readonly IMapper _mapper = mapper;
        private readonly IInRuleService _inRuleService = inRuleService;
        private readonly IInventoryServiceClient _inventoryService = inventoryService;
        private readonly IBoomiConnector _boomiConnector = boomiConnector;
        private readonly IOneExchangeUnitOfWork _uow = uow;
        private static readonly CompositeFormat StrictPenaltyDaysFormat = CompositeFormat.Parse(OneExchangeOrderServiceConstants.StrictPenaltyDays);

        private static readonly CompositeFormat ChoiceCancellationPolicyFormat = CompositeFormat.Parse(OneExchangeOrderServiceConstants.ChoiceCancellationPolicyMessage);
        private static readonly Action<ILogger, string, string, string?, Exception?> LogInRuleCancellationFeeFailed =
        LoggerMessage.Define<string, string, string?>(
        LogLevel.Error,
        new EventId(1001, nameof(LogInRuleCancellationFeeFailed)),
        "{Prefix} InRule cancellation fee calculation failed for OrderId : {OrderId} and returned cancellationFee {CancellationFee} ");

        private static readonly Action<ILogger, string, string, Exception?> LogOrderUpdateFailed =
        LoggerMessage.Define<string, string>(
        LogLevel.Error,
        new EventId(1001, nameof(ApplyPatchToOrderAsync)),
        "Error updating order {OrderId}: {Error}");

        /// <summary>
        /// To get order (booking) details.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IEnumerable<OrderResponse>> GetOrdersAsync(OrderRequest request)
        {
            _logger.LogInformation("{Prefix} To get Order details.", OneExchangeOrderServiceConstants.OrderServicePrefix);
            List<Guid> confirmOrderProfileIds;
            IEnumerable<OrderResponse> orders;

            if (!string.IsNullOrEmpty(request.FreeTextSearch))
            {
                orders = await _orderRepository.GetOrdersFreeTextSearchAsync(request);
                confirmOrderProfileIds = orders.SelectMany(profile => profile?.ProfileId).ToList();
            }
            else
            {
                List<string> orderStatus = [.. Enum.GetNames(typeof(OrderStatus)).Where(status => status != OrderStatus.PENDING.ToString())];
                orders = await _orderRepository.GetOrdersAsync(request, orderStatus);

                // Get confirm orders and populate guest details
                confirmOrderProfileIds = orders.Where(order => order.OrderStatus ==
                OrderStatus.CONFIRMED.ToString()).SelectMany(profile => profile?.ProfileId).ToList();
            }
            if (confirmOrderProfileIds.Count > 0)
            {
                var guestDetails = await _profileRepository.GetGuestsAsync(confirmOrderProfileIds);
                foreach (var order in orders)
                {
                    var orderProfileIds = order.ProfileId;
                    var orderGuestDetails = guestDetails.Where(guest => (bool)(orderProfileIds?.Contains((Guid)guest.ProfileId))).ToList();

                    if (orderGuestDetails.Count > 0)
                    {
                        order.Guests ??= [];
                        order.Guests.AddRange(orderGuestDetails);
                    }
                }
            }
            var isUserEligibleForBookingFeeWaiver = false;

            if (request.IsUserDiamondPremierMember)
            {
                _logger.LogInformation("{Prefix} Processing booking fee eligibility check for member {MemberId}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix, request.CustomerExternalReferenceId);

                isUserEligibleForBookingFeeWaiver = await CheckBookingFeeEligibilityAsync(request.CustomerExternalReferenceId, request.OrderDateFrom);
                foreach (var order in orders)
                {
                    order.IsUserEligibleForBookingFeeWaiver = isUserEligibleForBookingFeeWaiver;
                }
            }
            _logger.LogInformation("{Prefix} Returning the results of Order with results count, {Count}",
                OneExchangeOrderServiceConstants.OrderServicePrefix, orders.Count());
            return orders;
        }

        public async Task<string> CreateOrderAsync(OrderReservationRequest request)
        {
            //Fetch contentful fields
            var (propertyName, propertyPhone, propertyAddress, propertyInfo, unitDisplayName) = await GetContentfulPropertyDetails(request);


            // Log the currency equivalents from the request
            if (request.CurrencyEquivalent != null && request.CurrencyEquivalent.Count != 0)
            {
                var currencyDetails = string.Join(", ", request.CurrencyEquivalent.Select(ce =>
                    $"AmountType: {ce.AmountType}, Currency: {ce.Currency}, Amount: {ce.Amount}"
                ));

                _logger.LogInformation("{Prefix} To process Order Reservation details. Currency equivalents received: [{CurrencyDetails}]",
                    OneExchangeOrderServiceConstants.OrderServicePrefix,
                    currencyDetails
                );
            }
            else
            {
                _logger.LogInformation("{Prefix} No currency equivalents provided in the request.",
                    OneExchangeOrderServiceConstants.OrderServicePrefix
                );
                request.CurrencyEquivalent = [];
            }

            // TODO validation for the format
            _logger.LogInformation("{Prefix} To process Order Reservation details.", OneExchangeOrderServiceConstants.OrderServicePrefix);
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var memoComment = string.Empty;
                if (!string.IsNullOrEmpty(request?.Memo) && request?.SalesPersonProfile != null && !string.IsNullOrEmpty(request?.SalesPersonProfile?.FirstName))
                {
                    var currentDateTime = DateTime.Now.ToString("O", CultureInfo.InvariantCulture);
                    memoComment = $"{currentDateTime}|{request?.Memo ?? string.Empty}|{request?.SalesPersonProfile?.FirstName ?? string.Empty} {request?.SalesPersonProfile?.LastName ?? string.Empty}";
                }
                else if (!string.IsNullOrEmpty(request?.Memo))
                {
                    memoComment = request?.Memo ?? string.Empty;
                }
                var itemCode = request.OrderItems?.FirstOrDefault()?.ItemCode;
                var partnerCode = CommonHelper.GetCodeFromItemCode(itemCode!, OneExchangeOrderServiceConstants.PartnerCodeString);
                var orderItem = request.OrderItems?.FirstOrDefault();
                var checkInDateStr = orderItem?.Attributes?.FirstOrDefault(a => a.AttributeType == OneExchangeOrderServiceConstants.CheckInDateOrderItemAttributeTypeString)?.AttributeValue;
                var checkOutDateStr = orderItem?.Attributes?.FirstOrDefault(a => a.AttributeType == OneExchangeOrderServiceConstants.CheckOutDateOrderItemAttributeTypeString)?.AttributeValue;
                // Checking the RealTime availability for Choice in durable function now

                //var nanoId = await Nanoid.GenerateAsync(size: 8);
                var nanoId = CommonHelper.GenerateNanoId();
                var orderNumber = "ORD" + nanoId.Result;

                var orderId = Guid.NewGuid();
                var billToProfileId = Guid.NewGuid(); //The same will be used for Customer Profile in profile table
                var guestProfileId = Guid.NewGuid();
                var salesPersonProfileId = Guid.NewGuid();
                var guestOrderId = Guid.NewGuid();

                var customerProfile = request.CustomerProfile;
                var salesProfile = request.SalesPersonProfile;
                var customerExternalReferenceId = customerProfile?.Identifiers
                 ?.FirstOrDefault(i => i.IdentifierSource == OneExchangeOrderServiceConstants.BgvMemberIdentifierSource
                 && i.IdentifierType == OneExchangeOrderServiceConstants.MemberIdIdentifierString)?.Identifier;
                var customerExternalSourceSystem = "BGV";


                var customerEmailAddress = request.CustomerProfile?.DigitalAddresses?.
                    FirstOrDefault(i => i.DigitalAddressType == "EMAIL")?.DigitalAddressDetail;
                var membershipLevel = orderItem?.Attributes?.FirstOrDefault(a => a.AttributeType == OneExchangeOrderServiceConstants.MemberShipLevelAttributeTypeString)
               ?.AttributeValue;
                //Prepare the Data
                var unitTypeCode = CommonHelper.GetCodeFromItemCode(itemCode!, OneExchangeOrderServiceConstants.UnitTypeString);
                var tenantCode = CommonHelper.GetCodeFromTradingAgreement(request.TradingAgreementCode, OneExchangeOrderServiceConstants.TenantCodeString);

                var tenantGuidId = OrganizationHelper.GetTenantGuid(tenantCode!);
                var tradingAgreementGuidId = OrganizationHelper.GetTradingAgreementGuid(partnerCode!);
                var guestProfileEntity = new Domain.Entities.ExchangeProfile.Profile();
                var auditUser = ResolveAuditUserForBooking(request);

                //OrderItems Attributes
                var allowedAttributeTypes = new[]
                {
                OneExchangeOrderServiceConstants.CheckInDateOrderItemAttributeTypeString,
                OneExchangeOrderServiceConstants.CheckOutDateOrderItemAttributeTypeString,
                OneExchangeOrderServiceConstants.NumAdultsOrderItemAttributeTypeString,
                OneExchangeOrderServiceConstants.NumChildrenOrderItemAttributeTypeString,
                OneExchangeOrderServiceConstants.MemberShipLevelAttributeTypeString
            };

                //OrderItems Attributes
                var allowedDigitalAddressType = new[]
                {
                OneExchangeOrderServiceConstants.EmailDigitalAddressTypeString
            };

                var order = new Order
                {
                    Id = orderId,
                    TenantId = tenantGuidId,
                    TradingAgreementId = tradingAgreementGuidId,
                    OrderType = "BOOKING",
                    OrderNumber = orderNumber,
                    OrderStatus = "PENDING",
                    ExternalSourceSystem = request.ExternalOrderReference?.ExternalSourceSystem,
                    ExternalReferenceId = request.ExternalOrderReference?.ExternalReferenceId, //TODO Confirmation Number shared, for GP it is in payload, for choice it will be in patch call
                    OrderDate = DateTime.UtcNow,
                    SalesPersonFirstName = request.SalesPersonProfile?.FirstName,
                    SalesPersonLastName = request.SalesPersonProfile?.LastName,
                    SalesPersonEmail = request.SalesPersonProfile?.DigitalAddresses?.FirstOrDefault(i => i.DigitalAddressType == "EMAIL")?.DigitalAddressDetail,
                    BillToProfileId = billToProfileId, //TODO create entry in [exch_profile].[profile]  and update here customerProfile Billing Address
                    CustomerExternalReferenceId = customerExternalReferenceId,
                    CustomerExternalSourceSystem = customerExternalSourceSystem,
                    CustomerPhoneNumber = request.CustomerProfile?.Phones?.FirstOrDefault()?.Number,
                    CustomerEmailAddress = customerEmailAddress,
                    CustomerFirstName = request.CustomerProfile?.FirstName,
                    CustomerLastName = request.CustomerProfile?.LastName,
                    Currency = request.Currency,
                    Subtotal = request.Subtotal,
                    TaxCurrency = request.TaxCurrency,
                    TaxAmount = request.TaxAmount,
                    FeeCurrency = request.FeeCurrency,
                    OrderFee = request.OrderFee,
                    Memo = memoComment ?? string.Empty,
                    InsertedBy = auditUser,
                    CustomerMemberLevel = membershipLevel,
                    WaiveFee = request.WaiveFee,
                    OrderItems = (request?.OrderItems ?? []).Select((item, idx) =>
                    {
                        var orderItemId = Guid.NewGuid();

                        // Start with filtered attributes from the request
                        var attributes = item.Attributes?
                            .Where(a => allowedAttributeTypes.Contains(a.AttributeType))
                            .Select((a, i) => new OrderItemAttribute
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantGuidId,
                                OrderItemId = orderItemId,
                                AttributeType = a.AttributeType,
                                AttributeValue = a.AttributeValue,
                                Sequence = i, // zero-based within the item
                                InsertedBy = auditUser
                            })
                            .ToList() ?? [];

                        // Continue sequence from existing attributes
                        var seq = attributes.Count;

                        // Append UNIT_TYPE (from unitDisplayName)

                        attributes.Add(new OrderItemAttribute
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantGuidId,
                            OrderItemId = orderItemId,
                            AttributeType = OneExchangeOrderServiceConstants.PropertyUnitTypeString, // "UNIT_TYPE"
                            AttributeValue = unitDisplayName ?? string.Empty,
                            Sequence = seq++,
                            InsertedBy = auditUser
                        });


                        // Append PROPERTY_NAME

                        attributes.Add(new OrderItemAttribute
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantGuidId,
                            OrderItemId = orderItemId,
                            AttributeType = OneExchangeOrderServiceConstants.PropertyNameAttributeTypeString, // "PROPERTY_NAME"
                            AttributeValue = propertyName ?? string.Empty,
                            Sequence = seq++,
                            InsertedBy = auditUser
                        });

                        // Append PROPERTY_ADDRESS

                        attributes.Add(new OrderItemAttribute
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantGuidId,
                            OrderItemId = orderItemId,
                            AttributeType = OneExchangeOrderServiceConstants.PropertyAddressAttributeTypeString, // "PROPERTY_ADDRESS"
                            AttributeValue = propertyAddress ?? string.Empty,
                            Sequence = seq++,
                            InsertedBy = auditUser
                        });


                        return new OrderItem
                        {
                            Id = orderItemId,
                            TenantId = tenantGuidId,
                            OrderId = orderId,
                            ItemId = Guid.NewGuid(), // TODO: from Contentful
                            ItemCode = item.ItemCode, // use the current item's code (not the first item's)
                            Sequence = idx + 1,
                            ItemDescription = propertyName ?? string.Empty,
                            Quantity = 1,
                            UnitPrice = item.UnitPrice,
                            UnitPriceCurrency = item.Currency,
                            TaxCurrency = item.TaxCurrency,
                            TaxAmount = item.TaxAmount,
                            Memo = item.Memo,
                            InsertedBy = auditUser,
                            OrderItemAttributes = attributes
                        };
                    }).ToList()
                };

                var guest = request.Guests?.FirstOrDefault();

                if (guest != null)
                {
                    order.OrderGuests =
                    [
                        new()
                        {
                            Id = Guid.NewGuid(),
                            OrderId = orderId,
                            TenantId = order.TenantId,
                            ProfileId = guestProfileId,
                            IsPrimary = string.Equals(guest.ProfileType, "Primary", StringComparison.OrdinalIgnoreCase),
                            InsertedBy = auditUser
                        }
                    ];
                }

                var customerProfileEntity = new Domain.Entities.ExchangeProfile.Profile
                {
                    Id = billToProfileId,
                    ProfileType = "PERSON",
                    FirstName = customerProfile!.FirstName,
                    LastName = customerProfile.LastName,
                    ExternalSourceSystem = customerExternalReferenceId,
                    ExternalReferenceId = customerExternalSourceSystem,
                    InsertedBy = auditUser,
                    Addresses = customerProfile.Addresses?.Select((a, index) => new Address
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = billToProfileId,
                        Sequence = index + 1,
                        AddressType = OneExchangeOrderServiceConstants.Billing,
                        AddressPurpose = OneExchangeOrderServiceConstants.Primary,
                        Line1 = a.Line1,
                        Line2 = a.Line2,
                        Line3 = a.Line3,
                        City = a.City,
                        State = a.State,
                        Province = a.Province,
                        Postalcode = a.PostalCode,
                        Country = a.Country,
                        VerifiedDate = a.VerifiedDate,
                        PostalServiceVerified = a.PostalServiceVerified,
                        InsertedBy = auditUser
                    }).ToList() ?? [],
                    Phones = request.CustomerProfile.Phones?.Select((p, index) => new Phone
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = billToProfileId,
                        Sequence = index,
                        PhoneType = "MOBILE",
                        PhonePurpose = "PRIMARY",
                        Number = p.Number,
                        Extension = p.Extension,
                        VerifiedDate = p.VerifiedDate,
                        InsertedBy = auditUser
                    }).ToList() ?? [],
                    DigitalAddresses = request.CustomerProfile.DigitalAddresses?.Select((d, index) => new DigitalAddress
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = billToProfileId,
                        Sequence = index,
                        DigitalAddressType = d.DigitalAddressType,
                        VerifiedDate = d.VerifiedDate,
                        InsertedBy = auditUser
                    }).ToList() ?? []
                };
                if (guest != null)
                {
                    guestProfileEntity = new Domain.Entities.ExchangeProfile.Profile
                    {
                        Id = guestProfileId,
                        ProfileType = "PERSON",
                        FirstName = guest.FirstName,
                        LastName = guest.LastName,
                        ExternalSourceSystem = guest.ExternalSourceSystem,
                        ExternalReferenceId = guest.ExternalReferenceId,
                        InsertedBy = auditUser,
                        Addresses = guest.Addresses?.Select((a, index) => new Address
                        {
                            Id = Guid.NewGuid(),
                            ProfileId = guestProfileId,
                            Sequence = index + 1,
                            AddressType = "HOME",
                            AddressPurpose = "PRIMARY",
                            Line1 = a.Line1,
                            Line2 = a.Line2,
                            Line3 = a.Line3,
                            City = a.City,
                            State = a.State,
                            Province = a.Province,
                            Postalcode = a.PostalCode,
                            Country = a.Country,
                            VerifiedDate = a.VerifiedDate,
                            PostalServiceVerified = a.PostalServiceVerified,
                            InsertedBy = auditUser
                        }).ToList() ?? [],
                        Phones = guest.Phones?.Select((p, index) => new Phone
                        {
                            Id = Guid.NewGuid(),
                            ProfileId = guestProfileId,
                            Sequence = index + 1,
                            PhoneType = p.PhoneType,
                            PhonePurpose = p.PhonePurpose,
                            Number = p.Number,
                            Extension = p.Extension,
                            VerifiedDate = p.VerifiedDate,
                            InsertedBy = auditUser
                        }).ToList() ?? [],
                        DigitalAddresses = guest.DigitalAddresses?.Select((d, index) => new DigitalAddress
                        {
                            Id = Guid.NewGuid(),
                            ProfileId = guestProfileId,
                            Sequence = index + 1,
                            DigitalAddressType = d.DigitalAddressType,
                            VerifiedDate = d.VerifiedDate,
                            InsertedBy = auditUser,
                            DigitalAddress1 = d.DigitalAddressDetail
                        }).ToList() ?? []
                    };
                }
                await _unitOfWork.ProfileRepository.CreateProfileAsync(customerProfileEntity);
                await _unitOfWork.ProfileRepository.CreateProfileAsync(guestProfileEntity);

                await _unitOfWork.OrderRepository.CreateOrderAsync(order);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                // Map order to OrderRequestedEvent payload
                var orderRequestedEvent = MappingOrderToEventhubRequest.MapToOrderRequestedEvent(order, request, propertyName, propertyPhone, propertyAddress, membershipLevel, propertyInfo, unitDisplayName, request.CurrencyEquivalent);

                // Publish to Event Hub
                await _eventPublisher.PublishOrderAsync(orderRequestedEvent);
                return order.OrderNumber;
            }

            catch (Exception ex)
            {
                _logger.LogError("{Prefix} Failed to process Order Reservation {Error}.", OneExchangeOrderServiceConstants.OrderServicePrefix, ex.Message);
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

        }

        private async Task<(string propertyName, string propertyPhone, string propertyAddress, string propertyInfo, string? unitDisplayName)> GetContentfulPropertyDetails(OrderReservationRequest request)
        {
            var itemCode = request.OrderItems?.FirstOrDefault()?.ItemCode;
            if (string.IsNullOrEmpty(itemCode))
            {
                throw new ArgumentException("Item code is missing from the order request.");
            }

            var resortCode = CommonHelper.GetCodeFromItemCode(itemCode, OneExchangeOrderServiceConstants.CompleteResortCodeString);
            var unitTypeCode = CommonHelper.GetCodeFromItemCode(itemCode, OneExchangeOrderServiceConstants.UnitTypeString);
            if (string.IsNullOrWhiteSpace(resortCode))
            {
                throw new ArgumentException("Resort code is required.");
            }


            var resort = await _contentfulService.GetResortDetailsAsync(resortCode) ?? throw new InvalidOperationException($"Resort not found for code: {resortCode}");

            var propertyName = resort.DisplayName ?? string.Empty;
            var contact = resort.OverviewSectionContent?.ContactPhoneNumber ?? string.Empty;

            var location = resort.OverviewSectionContent?.Location;
            var addressParts = new[] { location?.Street1, location?.State, location?.City, location?.Country, location?.ZipCode };
            var propertyAddress = string.Join(", ", addressParts.Where(part => !string.IsNullOrWhiteSpace(part)));

            var unitDisplayName = resort.UnitTypes
                ?.FirstOrDefault(u => u.UnitTypeCode == unitTypeCode || u.UnitTypeCode == itemCode) // Example NQQ or CH-AZ01-NQQ
                ?.DisplayName;

            var propertyInfo = string.Join(
                Environment.NewLine,
                resort.OverviewSectionAlerts?.Alerts?
                    .Where(a => a?.Text != null)
                    .Select(alert => RichTextHtmlHelper.RenderDocumentToHtml(alert!.Text)) ?? []
            );

            return (propertyName, contact, propertyAddress, propertyInfo, unitDisplayName);
        }


        public async Task<List<TransactionHistoryResponse>> GetTransactionHistoryAsync(TransactionHistoryRequest request)
        {
            _logger.LogInformation(
                "{Prefix} GetTransactionHistoryAsync started. CustomerExternalReferenceId={CustomerExternalReferenceId}",
                OneExchangeOrderServiceConstants.OrderServicePrefix,
                request?.CustomerExternalReferenceId);

            try
            {
                if (!Guid.TryParse(OneExchangeOrderServiceConstants.BgvTenantGuidId, out var tenantGuid))
                {
                    _logger.LogError(
                        "{Prefix} GetTransactionHistoryAsync failed. Invalid tenant GUID configuration. BgvTenantGuidId={BgvTenantGuidId}, BgvTenantId={BgvTenantId}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        OneExchangeOrderServiceConstants.BgvTenantGuidId,
                        OneExchangeOrderServiceConstants.BgvTenantId);

                    throw new ArgumentException($"Invalid TenantId: {OneExchangeOrderServiceConstants.BgvTenantId}");
                }

                var validTxnTypes = new[]
                {
            TransactionType.PurchaseByPoints.ToValue(),
            TransactionType.PointsRefund.ToValue()
        };

                var resortAttrType = PropertyEnumAttributes.PropertyName.ToValue();

                _logger.LogInformation(
                    "{Prefix} Fetching transaction logs. TenantId={TenantId}, CustomerExternalReferenceId={CustomerExternalReferenceId}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix,
                    tenantGuid,
                    request?.CustomerExternalReferenceId);

                List<ExchangeTransactionLog> transactionLogs;
                try
                {
                    transactionLogs = await _transactionRepository.GetTransactionLogsAsync(tenantGuid, request.CustomerExternalReferenceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "{Prefix} GetTransactionHistoryAsync failed while calling repository. TenantId={TenantId}, CustomerExternalReferenceId={CustomerExternalReferenceId}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        tenantGuid,
                        request?.CustomerExternalReferenceId);

                    throw;
                }

                try
                {
                    var transactions = transactionLogs
                        .Where(txn => validTxnTypes.Contains(txn.TransactionType)
                            && (string.Equals(txn.Order?.OrderStatus, OneExchangeOrderServiceConstants.CancelledOrderStatus, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(txn.Order?.OrderStatus, OneExchangeOrderServiceConstants.ConfirmedOrderStatus, StringComparison.OrdinalIgnoreCase)))
                        .OrderByDescending(txn => txn.TransactionDate)
                        .Select(txn => new TransactionHistoryResponse
                        {
                            OrderId = txn.Order.Id,
                            CustomerExternalReferenceId = txn.Order.CustomerExternalReferenceId ?? string.Empty,
                            ConfirmationDate = txn.TransactionDate.DateTime,
                            ConfirmationNumber = txn.TransactionType == TransactionType.PurchaseByPoints.ToValue() ? txn.Order.ExternalReferenceId ?? string.Empty : txn.Order.OrderReturn?.ExternalReferenceId ?? string.Empty,
                            PointUsed = txn.TransactionType == TransactionType.PointsRefund.ToValue() ? TransactionAmountFormat.NegativeF5.FormatAmount(-txn.ToTransactionAmount) : TransactionAmountFormat.DefaultF4.FormatAmount(txn.FromTransactionAmount),
                            Currency = OneExchangeOrderServiceConstants.Currency,
                            Participant = OneExchangeOrderServiceConstants.Participant,
                            Resort = txn.Order.OrderItems
                                .SelectMany(oi => oi.OrderItemAttributes)
                                .FirstOrDefault(attr => attr.AttributeType == resortAttrType)
                                ?.AttributeValue
                        })
                        .ToList();

                    _logger.LogInformation(
                        "{Prefix} GetTransactionHistoryAsync completed successfully. TenantId={TenantId}, CustomerExternalReferenceId={CustomerExternalReferenceId}, Count={Count}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        tenantGuid,
                        request?.CustomerExternalReferenceId,
                        transactions?.Count ?? 0);

                    return transactions;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "{Prefix} GetTransactionHistoryAsync failed during filtering/mapping. TenantId={TenantId}, CustomerExternalReferenceId={CustomerExternalReferenceId}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        tenantGuid,
                        request?.CustomerExternalReferenceId);

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{Prefix} GetTransactionHistoryAsync failed. CustomerExternalReferenceId={CustomerExternalReferenceId}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix,
                    request?.CustomerExternalReferenceId);

                throw;
            }
        }


        /// <summary>
        /// To get particular order detail
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public async Task<OrderResponse> GetOrderAsync(string orderId)
        {
            _logger.LogInformation("{Prefix} GetOrderAsync started. OrderId={OrderId}",
       OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);

            if (string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogError("{Prefix} Error: GetOrderAsync failed. OrderId is empty/null.",
                    OneExchangeOrderServiceConstants.OrderServicePrefix);
                return null;
            }

            if (!Guid.TryParse(orderId, out var orderGuid))
            {
                _logger.LogError("{Prefix} GetOrderAsync failed. Invalid GUID format. OrderId={OrderId}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);
                return null;
            }

            try
            {
                var order = await _orderRepository.GetOrderAsync(orderGuid);

                if (order == null)
                {
                    _logger.LogError("{Prefix} Error: GetOrderAsync - Order not found in DB. OrderId={OrderId}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);
                    return null;
                }

                var mappedOrder = _mapper.Map<OrderResponse>(order);

                if (mappedOrder == null)
                {
                    _logger.LogError("{Prefix} Error GetOrderAsync - Mapping returned NULL. OrderId={OrderId}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);
                    return null;
                }

                _logger.LogInformation("{Prefix} GetOrderAsync completed successfully. OrderId={OrderId}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);

                return mappedOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error GetOrderAsync failed. OrderId={OrderId}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);
                throw;
            }
        }
        /// <summary>
        /// To get particular order detail
        /// </summary>
        /// <param name="orderId">OrderId</param>
        /// <returns>OrderDataResponse</returns>
        public async Task<OrderDataResponse> GetOrderDetailsAsync(string orderId)
        {
            _logger.LogInformation("{Prefix} GetOrderDetailsAsync started. OrderId={OrderId}",
        OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);

            if (string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogError("{Prefix} Error: GetOrderDetailsAsync failed. OrderId is empty/null.",
                    OneExchangeOrderServiceConstants.OrderServicePrefix);
                return null;
            }

            if (!Guid.TryParse(orderId, out var orderGuid))
            {
                _logger.LogError("{Prefix} Error: GetOrderDetailsAsync failed. Invalid GUID format. OrderId={OrderId}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);
                return null;
            }

            try
            {
                var order = await _orderRepository.GetOrderAsync(orderGuid);

                if (order == null)
                {
                    _logger.LogError("{Prefix} Error: GetOrderDetailsAsync - Order not found in DB. OrderId={OrderId}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);
                    return null;
                }

                var mappedOrder = _mapper.Map<OrderDataResponse>(order);

                if (mappedOrder == null)
                {
                    _logger.LogError("{Prefix} Error GetOrderDetailsAsync - Mapping returned NULL. OrderId={OrderId}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);
                    return null;
                }

                mappedOrder.CancelDate = order?.OrderReturn?.ReturnDate;

                _logger.LogInformation("{Prefix} GetOrderDetailsAsync completed successfully. OrderId={OrderId}, Status={OrderStatus}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix,
                    orderId,
                    order.OrderStatus);

                return mappedOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error GetOrderDetailsAsync failed. OrderId={OrderId}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);
                throw;
            }
        }
        /// <summary>
        /// To get order status by order number
        /// </summary>
        /// <param name="orderNumber"></param>
        /// <returns></returns>
        public async Task<ActionResult> GetOrderStatusAsync(string orderNumber)
        {
            _logger.LogInformation("{Prefix} GetOrderStatusAsync started. OrderNumber={OrderNumber}",
        OneExchangeOrderServiceConstants.OrderServicePrefix, orderNumber);

            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                _logger.LogError("{Prefix} GetOrderStatusAsync failed. OrderNumber is empty/null.",
                    OneExchangeOrderServiceConstants.OrderServicePrefix);

                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = "OrderNumber is required.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            try
            {
                var result = await _orderRepository.GetOrderStatusAsync(orderNumber);

                if (result == null || string.IsNullOrWhiteSpace(result.Value.OrderStatus))
                {
                    _logger.LogError("{Prefix} Order not found for OrderNumber={OrderNumber}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix, orderNumber);

                    return new NotFoundObjectResult(new ProblemDetails
                    {
                        Title = "Order not found",
                        Detail = $"No order found with OrderNumber: {orderNumber}",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                _logger.LogInformation("{Prefix} GetOrderStatusAsync completed successfully. OrderNumber={OrderNumber}, Status={Status}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix,
                    orderNumber,
                    result.Value.OrderStatus);

                return new OkObjectResult(new
                {
                    result.Value.OrderStatus,
                    result.Value.ExternalReferenceId,
                    StatusReason = result.Value.StatusReason ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Error: GetOrderStatusAsync failed. OrderNumber={OrderNumber}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix, orderNumber);
                throw;
            }
        }
        /// <summary>
        /// To get cancel order status by order number
        /// </summary>
        /// <param name="orderNumber"></param>
        /// <returns></returns>
        public async Task<ActionResult> GetCancelOrderDetailsAsync(string orderNumber)
        {
            _logger.LogInformation(
         "{Prefix} GetCancelOrderDetailsAsync started. To get cancel Order status for  OrderNumber={OrderNumber}",
         OneExchangeOrderServiceConstants.OrderServicePrefix,
         orderNumber);

            try
            {
                var message = string.Empty;

                var result = await _orderRepository.GetOrderStatusAsync(orderNumber);

                //  if repository returned null
                if (result == null)
                {
                    _logger.LogError(
                        "{Prefix} GetCancelOrderDetailsAsync failed. Repository returned NULL. OrderNumber={OrderNumber}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        orderNumber);

                    throw new InvalidOperationException("Repository returned null for GetOrderStatusAsync.");
                }

                // Guard against nullable Value usage (prevents NullReferenceException)
                if (!result.HasValue)
                {
                    _logger.LogError(
                        "{Prefix} GetCancelOrderDetailsAsync failed. Repository result has no value. OrderNumber={OrderNumber}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        orderNumber);

                    throw new InvalidOperationException("Repository returned no value for GetOrderStatusAsync.");
                }

                var cancellationStatus = result.Value.OrderStatus;
                var orderId = result.Value.orderId;
                var statusReason = result.Value.StatusReason;

                if (string.IsNullOrWhiteSpace(cancellationStatus))
                {
                    _logger.LogError(
                        "{Prefix} GetCancelOrderDetailsAsync failed. OrderStatus is NULL/EMPTY. OrderNumber={OrderNumber}, OrderId={OrderId}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        orderNumber,
                        orderId);

                    throw new InvalidOperationException("OrderStatus was null/empty for GetOrderStatusAsync result.");
                }

                if (orderId == null)
                {
                    _logger.LogError(
                        "{Prefix} GetCancelOrderDetailsAsync failed. OrderId is NULL. OrderNumber={OrderNumber}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        orderNumber);

                    throw new InvalidOperationException("OrderId was null for GetOrderStatusAsync result.");
                }

                string cancellationNumber;
                try
                {
                    cancellationNumber = await _orderReturnRepository.GetCancellationNumberAsync(orderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "{Prefix} GetCancelOrderDetailsAsync failed while fetching cancellation number. OrderNumber={OrderNumber}, OrderId={OrderId}",
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        orderNumber,
                        orderId);

                    throw;
                }

                // If there is any technical failure then order status will be confirmed.
                if (cancellationStatus.Equals(OrderStatus.CONFIRMED.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    message = OneExchangeOrderServiceConstants.OrderStatusUpdateFailureMessage;
                }

                _logger.LogInformation(
                    "{Prefix} GetCancelOrderDetailsAsync completed. OrderNumber={OrderNumber}, Status={Status}, CancellationNumber={CancellationNumber}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix,
                    orderNumber,
                    cancellationStatus,
                    cancellationNumber);

                return new OkObjectResult(new
                {
                    OrderStatus = cancellationStatus,
                    ExternalReferenceId = cancellationNumber,
                    StatusReason = statusReason,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{Prefix} GetCancelOrderDetailsAsync failed. OrderNumber={OrderNumber}",
                    OneExchangeOrderServiceConstants.OrderServicePrefix,
                    orderNumber);

                throw;
            }
        }


        public async Task<Order?> GetOrderByOrderNumberAsync(string orderNumber) => await _unitOfWork.OrderRepository.GetOrderByOrderNumberAsync(orderNumber);
        public async Task<bool> CheckBookingFeeEligibilityAsync(string memberId, string fromDate)
        {
            if (!Guid.TryParse(OneExchangeOrderServiceConstants.BgvTenantGuidId, out var tenantGuid))
            {
                throw new ArgumentException($"Invalid TenantId: {OneExchangeOrderServiceConstants.BgvTenantGuidId}");
            }

            var hasExceededLimit = await _orderRepository.HasExceededWaiverLimitForCurrentYearAsync(memberId, fromDate);

            var isEligible = !hasExceededLimit;

            _logger.LogInformation("{Prefix} Member {MemberId} booking fee waiver eligible: {IsEligible}",
                OneExchangeOrderServiceConstants.OrderServicePrefix, memberId, isEligible);

            return isEligible;
        }

        /// <summary>
        /// To update order status
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<string> UpdateOrderAsync(string orderId, OrderPatchRequest request)
        {
            try
            {
                _logger.LogInformation("{Prefix} To upadte Order status for OrderId : {orderId}", OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);


                var order = await _unitOfWork.OrderRepository.GetOrderAsync(Guid.Parse(orderId));
                if (order == null)
                {
                    _logger.LogError(
                        "{Prefix} Order not found for OrderId : {OrderId}.",
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        orderId);

                    throw new InvalidOperationException($"{OneExchangeOrderServiceConstants.OrderServicePrefix}Order '{orderId}' was not found for cancellation.");
                }
                var auditUser = ResolveAuditUserForCancellation(request, order);

                //If order status is Confirmed or  pending cancellation than.
                if (string.Equals(order.OrderStatus, OneExchangeOrderServiceConstants.ConfirmedOrderStatus, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(order.OrderStatus, OneExchangeOrderServiceConstants.OrderPendingCancellationStatus, StringComparison.OrdinalIgnoreCase))
                {
                    //Get billing address.
                    var address = await _profileRepository.GetProfileAddressAsync(OneExchangeOrderServiceConstants.Billing, order.BillToProfileId);

                    //To get all profile id.
                    var profileIds = order.OrderGuests.Select(x => x.ProfileId).ToList();
                    var guestDetails = await _profileRepository.GetOrderGuestsAsync(profileIds);
                    var checkInDateString = order?.OrderItems?.FirstOrDefault()?.OrderItemAttributes.FirstOrDefault(x => x.AttributeType == OneExchangeOrderServiceConstants.CheckInDateOrderItemAttributeTypeString)?.AttributeValue ?? string.Empty;
                    var orderCustomerMember = order?.CustomerMemberLevel ?? string.Empty;
                    var memberShipLevel = order?.OrderItems?.FirstOrDefault()?.OrderItemAttributes.FirstOrDefault(x => x.AttributeType == OneExchangeOrderServiceConstants.MemberShipLevelOrderItemAttributeTypeString)?.AttributeValue ?? orderCustomerMember;

                    if (!DateTime.TryParse(checkInDateString, out var checkInDate))
                    {
                        _logger.LogError(
                            "{Prefix} Invalid check-in date '{CheckInDate}' for OrderId : {OrderId}.",
                            OneExchangeOrderServiceConstants.OrderServicePrefix,
                            checkInDateString,
                            orderId);

                        throw new InvalidOperationException(
                            $"{OneExchangeOrderServiceConstants.OrderServicePrefix} Invalid check-in date '{checkInDateString}' for order '{orderId}' for cancellation.");
                    }
                    //Get absolute deadline.
                    var absoluteDeadline = order?.OrderItems?.FirstOrDefault()?.OrderItemAttributes.FirstOrDefault(x => x.AttributeType == OneExchangeOrderServiceConstants.AbsoluteDeadlineAttributeTypeString)?.AttributeValue ?? string.Empty;
                    // Check for required fields

                    // Membershiplevel check
                    if (string.IsNullOrWhiteSpace(memberShipLevel))
                    {
                        _logger.LogError(
                            "{Prefix} Membership level is missing for OrderId : {OrderId}. Cannot calculate cancellation fee.",
                            OneExchangeOrderServiceConstants.OrderServicePrefix,
                            orderId);

                        throw new InvalidOperationException($"Membership level is required to calculate cancellation fee for order '{orderId}'.");
                    }
                    // OrderDate check
                    if (order?.OrderDate == null || order?.OrderDate == default)
                    {
                        _logger.LogError(
                            "{Prefix} OrderDate is missing for OrderId : {OrderId}. Cannot calculate cancellation fee.",
                            OneExchangeOrderServiceConstants.OrderServicePrefix,
                            orderId);

                        throw new InvalidOperationException(
                            $"{OneExchangeOrderServiceConstants.OrderServicePrefix} Order date is required to calculate cancellation fee for order '{orderId}'.");
                    }


                    var cancellationFeeResponse = await _inRuleService.GetCancellationFee(order?.OrderDate, Convert.ToDateTime(checkInDate), memberShipLevel, absoluteDeadline);

                    if (cancellationFeeResponse == null)
                    {
                        _logger.LogError(
                            "{Prefix} InRule GetCancellationFee returned null for OrderId : {OrderId}.",
                            OneExchangeOrderServiceConstants.OrderServicePrefix,
                            orderId);

                        throw new InvalidOperationException($"{OneExchangeOrderServiceConstants.OrderServicePrefix} Failed to calculate cancellation fee for order '{orderId}'.");
                    }

                    // If CancellationFee is -1, it is considered as failure from InRule
                    if (cancellationFeeResponse.CancellationFee < 0)
                    {
                        var fee = cancellationFeeResponse.CancellationFee.ToString(CultureInfo.InvariantCulture);

                        LogInRuleCancellationFeeFailed(
                        _logger,
                        OneExchangeOrderServiceConstants.OrderServicePrefix,
                        orderId,
                        fee,
                        null);

                        throw new InvalidOperationException($"{OneExchangeOrderServiceConstants.OrderServicePrefix} InRule cancellation fee calculation failed and returned cancellationFee {fee}");
                    }


                    if (!string.Equals(order?.OrderStatus, request.Value.OrderStatus, StringComparison.OrdinalIgnoreCase) && order != null)
                    {
                        await _unitOfWork.BeginTransactionAsync();
                        order.OrderStatus = request.Value.OrderStatus;
                        order.UpdatedBy = auditUser;
                        order.UpdatedDateTime = DateTimeOffset.UtcNow;
                        await _unitOfWork.OrderRepository.UpdateOrderStatusAsync(order);
                        await _unitOfWork.SaveChangesAsync();
                        await _unitOfWork.CommitTransactionAsync();
                    }

                    // Map order to cancel request payload.
                    var cancelOrderRequestedEvent = await MapToCancelOrderRequestedEvent(order, [.. address], [.. guestDetails], cancellationFeeResponse, request);

                    var cancelOrderJson = JsonSerializer.Serialize(cancelOrderRequestedEvent, IndentedJsonOptions);

                    _logger.LogInformation("{Prefix} Cancel payload publishing on Event Hub : {Payload}", OneExchangeOrderServiceConstants.OrderServicePrefix, cancelOrderJson);

                    // Publish to Event Hub 
                    await _eventCancelOrderPublisher.PublishOrderAsync(cancelOrderRequestedEvent);

                    _logger.LogInformation("{Prefix} Publish finish on Event Hub :", OneExchangeOrderServiceConstants.OrderServicePrefix);
                }
                return order.OrderNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} An error occured while updating order details.", OneExchangeOrderServiceConstants.OrderServicePrefix);
                throw;
            }
        }



        /// <summary>
        /// To get property address  details.
        /// </summary>
        /// <param name="itemCode"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<(string propertyName, string propertyPhone, string propertyAddress, string propertyInfo, string? unitDisplayName)> GetContentfulPropertyDetailsCancelOrder(string itemCode)
        {
            if (string.IsNullOrEmpty(itemCode))
            {
                throw new ArgumentException("Item code is missing from the order request.");
            }

            var resortCode = CommonHelper.GetCodeFromItemCode(itemCode, OneExchangeOrderServiceConstants.CompleteResortCodeString);
            var unitTypeCode = CommonHelper.GetCodeFromItemCode(itemCode, OneExchangeOrderServiceConstants.UnitTypeString);

            if (string.IsNullOrWhiteSpace(resortCode))
            {
                throw new ArgumentException($"ResortCode not found for finding resort for itemcode {itemCode}");
            }
            var resort = await _contentfulService.GetResortDetailsAsync(resortCode) ?? throw new InvalidOperationException($"Resort not found for code: {resortCode}");

            var propertyName = resort.DisplayName ?? string.Empty;
            var contact = resort.OverviewSectionContent?.ContactPhoneNumber ?? string.Empty;

            var location = resort.OverviewSectionContent?.Location;
            var addressParts = new[] { location?.Street1, location?.State, location?.City, location?.Country, location?.ZipCode };
            var propertyAddress = string.Join(", ", addressParts.Where(part => !string.IsNullOrWhiteSpace(part)));

            var unitDisplayName = resort.UnitTypes
                ?.FirstOrDefault(u => u.UnitTypeCode == unitTypeCode)
                ?.DisplayName;

            var propertyInfo = string.Join(
                Environment.NewLine,
                resort.OverviewSectionAlerts?.Alerts?
                    .Where(a => a?.Text != null)
                    .Select(alert => RichTextHtmlHelper.RenderDocumentToHtml(alert!.Text)) ?? []
            );

            return (propertyName, contact, propertyAddress, propertyInfo, unitDisplayName);
        }


        /// <summary>
        /// To prepare cancel order object to publish in Event Hub.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="addresses"></param>
        /// <param name="guests"></param>
        /// <param name="cancelFeeDetails"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<CancelOrderEvent> MapToCancelOrderRequestedEvent(Order order, List<Address> addresses,
        List<Domain.Entities.ExchangeProfile.Profile> guests, CancelFeeResponse cancelFeeDetails, OrderPatchRequest request)
        {
            var nanoId = CommonHelper.GenerateNanoId();
            var orderReturnNumber = string.Concat(OneExchangeOrderServiceConstants.OrderCancelIdAbbreviation, nanoId.Result);
            var itemCode = order.OrderItems?.FirstOrDefault()?.ItemCode;
            if (string.IsNullOrEmpty(itemCode))
            {
                throw new ArgumentException("Item code is missing from the order request.");
            }

            var (propertyName, propertyPhone, propertyAddress, propertyInfo, unitDisplayName) = await GetContentfulPropertyDetailsCancelOrder(itemCode);
            var fee = cancelFeeDetails?.CancellationFee ?? 0d;

            var cancelOrderPayload = new CancelOrderEvent
            {
                Data = new CancelOrderEventhubData
                {
                    SystemMeta = new CancelSystemMetaEvent
                    {
                        TenantID = OneExchangeOrderServiceConstants.BgvTenantCode,
                        InsertedBy = OneExchangeOrderServiceConstants.OneXChange,
                        Status = true,
                        StatusReason = OneExchangeOrderServiceConstants.Active
                    },
                    IsImpersonationExpiredMember = request.Value.IsImpersonationExpiredMember,
                    OrderId = order.Id.ToString(),
                    TradingAgreementCode = Convert.ToString(order?.TradingAgreementId, CultureInfo.InvariantCulture) ?? string.Empty,
                    OrderType = order?.OrderType ?? string.Empty,
                    OrderNumber = order?.OrderNumber ?? string.Empty,
                    OrderStatus = order?.OrderStatus ?? string.Empty,
                    OrderDate = order?.OrderDate,
                    CancelDate = DateTime.Now,
                    Currency = order?.Currency ?? string.Empty,
                    Subtotal = order?.Subtotal ?? 0,
                    TaxCurrency = order?.TaxCurrency ?? string.Empty,
                    TaxAmount = order?.TaxAmount ?? 0,
                    FeeCurrency = order?.FeeCurrency ?? string.Empty,
                    OrderFee = order?.OrderFee ?? Convert.ToDecimal(0),
                    Memo = order?.Memo ?? string.Empty,
                    Addresses = [.. request.Value.CustomerProfile.Addresses.Select((a, i) => new
                    Domain.Models.Eventhub.OrderCancellation.Address
                    {
                        Sequence = a.Sequence,
                        AddressType = a.AddressType ?? string.Empty,
                        AddressPurpose = a.AddressPurpose ?? string.Empty,
                        Line1 = a.Line1 ?? string.Empty,
                        Line2 = a.Line2 ?? string.Empty,
                        Line3 = a.Line3 ?? string.Empty,
                        City = a.City ?? string.Empty,
                        State = a.State ?? string.Empty,
                        Country = a.Country ?? string.Empty,
                        Province = a.Province ?? string.Empty,
                        PostalCode = a.PostalCode ?? string.Empty,
                    })],
                    ExternalOrderReference = new CancelExternalOrderReferenceEvent
                    {
                        ExternalSourceSystem = order?.ExternalSourceSystem ?? string.Empty,
                        ExternalReferenceId = order?.ExternalReferenceId ?? string.Empty,
                    },
                    CustomerProfile = new CancelProfileEvent
                    {
                        Phones = [new() { Number = order?.CustomerPhoneNumber }],
                        DigitalAddresses =
                            [
                                new()
                                {
                                    DigitalAddressType = OneExchangeOrderServiceConstants.EmailDigitalAddressTypeString,
                                    DigitalAddress = order?.CustomerEmailAddress ?? string.Empty,
                                }
                            ],

                        Identifiers = [new()
                        {
                            IdentifierType = OneExchangeOrderServiceConstants.MemberIdIdentifierString,
                            Identifier = order?.CustomerExternalReferenceId ?? string.Empty,
                            IdentifierSource = order?.CustomerExternalSourceSystem ?? string.Empty
                        }
                       ],
                        Addresses = [.. addresses.Select((a, index) => new CancelAddressEvent
                        {
                            Sequence = index + 1,
                            AddressType = a.AddressType,
                            AddressPurpose = a.AddressPurpose,
                            Line1 = a.Line1,
                            Line2 = a.Line2,
                            Line3 = a.Line3,
                            City = a.City,
                            State = a.State,
                            Province = a.Province,
                            PostalCode = a.Postalcode,
                            Country = a.Country,
                        })],
                    },
                    Guests = [.. guests.Select(g => new CancelProfileEvent
                    {
                        ProfileType = g.ProfileType,
                        FirstName = g.FirstName,
                        LastName = g.LastName,
                        Addresses = g.Addresses?.Select((a, i) => new CancelAddressEvent
                        {
                            Sequence = i + 1,
                            AddressType = a.AddressType,
                            AddressPurpose = a.AddressPurpose,
                            Line1 = a.Line1,
                            Line2 = a.Line2,
                            Line3 = a.Line3,
                            City = a.City,
                            State = a.State,
                            Province = a.Province,
                            PostalCode = a.Postalcode,
                            Country = a.Country
                        }).ToList(),
                    })],
                    OrderItems = [.. order.OrderItems.Select((item, index) =>
                    {
                        var attributes = item.OrderItemAttributes?.Select(a => new CancelOrderItemAttributeEvent
                        {
                            AttributeType = a.AttributeType,
                            AttributeValue = a.AttributeValue
                        }).ToList() ?? [];

                        if (!string.IsNullOrWhiteSpace(propertyName))
                        {
                            attributes.Add(new CancelOrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.PropertyNameAttributeTypeString,
                                AttributeValue = propertyName
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(propertyPhone))
                        {
                            attributes.Add(new CancelOrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.PropertyPhoneAttributeTypeString,
                                AttributeValue = propertyPhone
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(propertyAddress))
                        {
                            attributes.Add(new CancelOrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.PropertyAddressAttributeTypeString,
                                AttributeValue = propertyAddress
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(unitDisplayName))
                        {
                            attributes.Add(new CancelOrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.PropertyUnitTypeString,
                                AttributeValue = unitDisplayName
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(propertyInfo))
                        {
                            attributes.Add(new CancelOrderItemAttributeEvent
                            {
                                AttributeType = OneExchangeOrderServiceConstants.PropertyImportantInfoString,
                                AttributeValue = propertyInfo
                            });
                        }

                        return new CancelOrderItemEventModelEvent
                        {
                            Id = item.Id,
                            Sequence = item.Sequence,
                            ItemId = item.ItemId,
                            ItemCode = order?.OrderItems?.FirstOrDefault()?.ItemCode ?? string.Empty,
                            ItemDescription = item.ItemDescription,
                            Quantity = item.Quantity,
                            Currency = item.UnitPriceCurrency ?? string.Empty,
                            UnitPrice = item.UnitPrice,
                            TaxCurrency = item.TaxCurrency,
                            TaxAmount = item.TaxAmount,
                            Memo = item?.Memo ?? string.Empty,
                            Attributes = attributes
                        };
                    })],
                    Payments = [.. order.OrderPayments.Select((a, index) => new CancelOrderPayment
                    {
                        Id = a.Id,
                        PaymentPurpose = a.PaymentPurpose,
                        Currency = a.Currency,
                        Amount = a.Amount,
                        PaymentAccountToken = request.Value.PaymentAccountToken,
                        ExpirationDate = request.Value.ExpirationDate

                    })],
                    OrderReturn = new CancelOrderReservationReturn
                    {
                        ReturnNumber = orderReturnNumber,
                        ReturnDate = DateTime.Now,
                        ReturnFeeCurrency = OneExchangeOrderServiceConstants.USD,// It will be USD
                        ReturnFeeAmount = fee < 1d ? "0" : fee.ToString(CultureInfo.InvariantCulture),
                        IsPointsTobeRefund = cancelFeeDetails?.IsRefundPoints ?? false,
                        WaiveFee = request.Value.WaiveFee,// It will be send by front end (boolean flag).
                        Memo = request.Value.Memo, // It will be send by front end (comment).
                        SalesPersonProfile = new CancelSalesPersonProfile
                        {
                            FirstName = request?.Value?.SalesPersonProfile?.FirstName ?? string.Empty,
                            LastName = request?.Value?.SalesPersonProfile?.LastName ?? string.Empty,
                            DigitalAddresses = request?.Value?.SalesPersonProfile?.DigitalAddresses?.Select(
                                a => new CancelDigitalAddress
                                {
                                    DigitalAdd = a.Address
                                }).ToList() ?? []
                        },
                    },
                    ExchangeTransactions = [.. order.ExchangeTransactionLogs.Select((a, index) => new CancelExchangeTransaction
                    {
                        TransactionDate = a.TransactionDate,
                        TransactionType = a.TransactionType,
                        FromAccountId = a.FromAccountId,
                        FromCurrency = a.FromCurrency,
                        FromTransactionAmount = a.FromTransactionAmount,
                        ToAccountId = a.ToAccountId,
                        ToCurrency = a.ToCurrency,
                        ToTransactionAmount = a.ToTransactionAmount,
                        TransactionReferenceId = a.TransactionReferenceId,
                        Memo = a.Memo,
                        ToConfirmationNumber = a.ToConfirmationNumber,
                        FromConfirmationNumber = a.FromConfirmationNumber
                    })]
                }
            };
            return cancelOrderPayload;
        }

        /// <summary>
        /// To update order comment
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> UpdateOrderCommentAsync(string orderId, UpdateOrderComment request)
        {
            try
            {
                _logger.LogInformation("{Prefix} To update Order memo for OrderId : {orderId}", OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);
                await _unitOfWork.BeginTransactionAsync();
                var order = await _unitOfWork.OrderRepository.GetOrderAsync(Guid.Parse(orderId));
                var currentDateTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                var comment = $"{currentDateTime}|{request.Message}|{request.UserName}";
                order.Memo = order.Memo?.Length > 0 ? $"{order.Memo}||{comment}" : comment;
                if (order.Memo.Length > 500)
                {
                    _logger.LogInformation("{Prefix} update to Order memo for OrderId : {orderId} failed. ", OneExchangeOrderServiceConstants.OrderServicePrefix, orderId);
                    throw new ColumnDataLimitException(OneExchangeOrderServiceConstants.ColumnLimit);
                }
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} An error occured while updating order comment details.", OneExchangeOrderServiceConstants.OrderServicePrefix);
                throw;
            }
        }
        public async Task<OrderDetailsResponse?> GetBookingDetailsAsync(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return null;
            }

            var bookingDetails = await _orderRepository.GetOrderDetailsByConfirmationNumberAsync(orderId);
            if (bookingDetails == null)
            {
                return null;
            }

            await PopulateGuestNameIfPresentAsync(bookingDetails);
            await PopulatePropertyInfoIfItemCodePresentAsync(bookingDetails);

            return bookingDetails;
        }

        private async Task PopulateGuestNameIfPresentAsync(OrderDetailsResponse bookingDetails)
        {
            if (bookingDetails.ProfileIds == null || bookingDetails.ProfileIds.Count == 0)
            {
                return;
            }

            var guests = await _profileRepository.GetGuestsAsync(bookingDetails.ProfileIds);
            var guest = guests.FirstOrDefault();
            if (guest == null)
            {
                return;
            }

            bookingDetails.FirstName = guest.FirstName ?? string.Empty;
            bookingDetails.LastName = guest.LastName ?? string.Empty;
        }

        private async Task PopulatePropertyInfoIfItemCodePresentAsync(OrderDetailsResponse bookingDetails)
        {
            if (string.IsNullOrEmpty(bookingDetails.ItemCode))
            {
                return;
            }

            var (propertyName, propertyPhone, propertyAddress, propertyInfo, _) =
                await GetContentfulPropertyDetailsCancelOrder(bookingDetails.ItemCode);

            bookingDetails.PropertyName = propertyName;
            bookingDetails.PropertyPhone = propertyPhone;
            bookingDetails.PropertyAddress = propertyAddress;

            var cancellationPolicy = BuildCancellationPolicyMessage(bookingDetails.CancellationPolicy);
            bookingDetails.PropertyInfo = PrependCancellationPolicy(propertyInfo, cancellationPolicy);
        }

        private static string BuildCancellationPolicyMessage(string? cancellationPolicyRaw)
        {
            var cancellationPolicy = OneExchangeOrderServiceConstants.OneExchangeCancellationPolicyMessage;

            if (string.IsNullOrEmpty(cancellationPolicyRaw))
            {
                return cancellationPolicy;
            }

            var penaltyValueMatch = MyRegex().Match(cancellationPolicyRaw);
            var days = penaltyValueMatch.Success
                ? int.Parse(penaltyValueMatch.Value, CultureInfo.InvariantCulture)
                : 0;

            var strictPenaltyText = string.Format(CultureInfo.InvariantCulture, StrictPenaltyDaysFormat, days);

            if (cancellationPolicyRaw.Contains(strictPenaltyText, StringComparison.Ordinal) &&
                days > OneExchangeOrderServiceConstants.CancelPolicyDays)
            {
                cancellationPolicy = string.Format(CultureInfo.InvariantCulture, ChoiceCancellationPolicyFormat, days);
            }

            return cancellationPolicy;
        }

        private static string PrependCancellationPolicy(string propertyInfo, string cancellationPolicy)
        {
            var infoLines = propertyInfo.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);

            var policyInfo = new List<string>(infoLines.Length + 1) { cancellationPolicy };
            policyInfo.AddRange(infoLines);

            return string.Join(Environment.NewLine, policyInfo);
        }

        public async Task<bool> UpdateRefundAsync(UpdateRefundRequest request)
        {
            _logger.LogInformation("{Prefix} To upadte Order status for OrderId : {orderId}", OneExchangeOrderServiceConstants.OrderServicePrefix, request.OrderId);
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var order = await _unitOfWork.OrderRepository.GetOrderAsync(Guid.Parse(request.OrderId));
                var orderRefund = new List<Domain.Entities.ExchangeOrder.OrderRefund>
                {
                    new() {
                        Id = Guid.NewGuid(),
                        TenantId = order.TenantId,
                        OrderId = order.Id,
                        OriginalPaymentId = Guid.Parse(request.OriginalPaymentId),
                        RefundPurpose = OneExchangeOrderServiceConstants.FEEREIMBURSEMENT,
                        Currency = OneExchangeOrderServiceConstants.USD,
                        Amount = request.RefundAmount,
                        RefundDate = DateTime.Now,
                        InsertedBy = request.SalesPersonEmail,
                        UpdatedBy = request.SalesPersonEmail,
                        InsertedDateTime = DateTime.Now,
                        UpdatedDateTime = DateTime.Now
                    }
                };

                await _unitOfWork.OrderRefundRepository.CreateOrderRefundAsync(orderRefund);

                var entity = new ExchangeTransactionLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = order.TenantId,
                    OrderId = order.Id,
                    TransactionDate = DateTime.UtcNow,
                    TransactionType = OneExchangeOrderServiceConstants.FEEREIMBURSEMENT,
                    ToAccountId = order.CustomerExternalReferenceId,
                    ToCurrency = "USD",
                    ToTransactionAmount = request.RefundAmount,
                    InsertedBy = request.SalesPersonEmail,
                    FromConfirmationNumber = request.ConfirmationNumber,
                };
                await _unitOfWork.ExchangeTransactionLogRepository.CreateTransactionAsync(entity);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating order {OrderId}: {Error}", request.OrderId, ex.Message);
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            return false;
        }


        private static string ResolveAuditUserForBooking(OrderReservationRequest request)
        {
            var sales = request.SalesPersonProfile;

            var salesPersonEmail = request.SalesPersonProfile?.DigitalAddresses?.FirstOrDefault(i => i.DigitalAddressType == "EMAIL")?.DigitalAddressDetail ?? string.Empty;
            if (sales != null && !string.IsNullOrWhiteSpace(salesPersonEmail))
            {
                return salesPersonEmail;
            }

            var member = request.CustomerProfile;
            var customerExternalReferenceId = request.CustomerProfile?.Identifiers
                ?.FirstOrDefault(i => i.IdentifierSource == OneExchangeOrderServiceConstants.BgvMemberIdentifierSource
                && i.IdentifierType == OneExchangeOrderServiceConstants.MemberIdIdentifierString)?.Identifier;
            var customerEmailAddress = request.CustomerProfile?.DigitalAddresses?.
                    FirstOrDefault(i => i.DigitalAddressType == "EMAIL")?.DigitalAddressDetail ?? string.Empty;

            if (member != null &&
                (!string.IsNullOrWhiteSpace(member.FirstName) ||
                 !string.IsNullOrWhiteSpace(member.LastName) || !string.IsNullOrWhiteSpace(customerExternalReferenceId)))
            {
                return customerEmailAddress;
            }

            // Fallback if somehow no names are present
            return OneExchangeOrderServiceConstants.PostOrderInsertedBy; // "Order-Service"
        }
        private static string ResolveAuditUserForCancellation(OrderPatchRequest request, Order order)
        {
            var sales = request?.Value?.SalesPersonProfile;
            var salePersonEmail = sales?.DigitalAddresses?.FirstOrDefault(i => i.DigitalAddressType == "EMAIL")?.Address ?? string.Empty;

            if (sales != null &&
                (!string.IsNullOrWhiteSpace(sales.FirstName) ||
                 !string.IsNullOrWhiteSpace(sales.LastName) || !string.IsNullOrWhiteSpace(salePersonEmail)))
            {
                return salePersonEmail;
            }

            // 2. Customer email from order
            if (!string.IsNullOrWhiteSpace(order?.CustomerEmailAddress))
            {
                return order.CustomerEmailAddress;
            }

            // 3. Fallback, if needed
            return OneExchangeOrderServiceConstants.PatchOrderUpdatedBy;

        }
        private static string ResolveAuditEmailForPatch(Order order, OrderPatchModel patchModel)
        {
            // 1. Cancellation flow: salesperson in OrderReturn (from cancel call)
            if (patchModel.OrderReturn != null)
            {
                var emailSaleFromReturn = patchModel.OrderReturn?.SalesPersonEmail;
                if (!string.IsNullOrWhiteSpace(emailSaleFromReturn))
                {
                    return emailSaleFromReturn ?? string.Empty;
                }

                return order?.CustomerEmailAddress ?? string.Empty;
            }

            // 2. Booking / general: salesperson stored on order
            if (!string.IsNullOrWhiteSpace(order.SalesPersonEmail))
            {
                return order.SalesPersonEmail;
            }

            // 3. Fallback: customer email from order row
            if (!string.IsNullOrWhiteSpace(order.CustomerEmailAddress))
            {
                return order.CustomerEmailAddress;
            }

            // 4. Last fallback
            return OneExchangeOrderServiceConstants.PatchOrderUpdatedBy;
        }

        [GeneratedRegex(OneExchangeOrderServiceConstants.PenaltyDaysExpression)]
        private static partial Regex MyRegex();
        public async Task<UpdateOrderResponse> ApplyPatchToOrderAsync(string orderId, JsonPatchDocument<OrderPatchModel> patchDoc)
        {
            var orderGuid = Guid.Parse(orderId);
            var order = await _unitOfWork.OrderRepository.GetOrderAsync(orderGuid);

            if (order == null)
            {
                return BuildOrderNotFoundResponse();
            }

            var existingStatus = order.OrderStatus;

            var newStatusOperation = FindOperation(patchDoc, OneExchangeOrderServiceConstants.OrderStatusPath);
            var newReasonOp = FindOperation(patchDoc, OneExchangeOrderServiceConstants.StatusReasonPath);
            var hasCancelItemAttrsOp = HasOperation(patchDoc, OneExchangeOrderServiceConstants.OrderItemAttributeCancelPath);

            if (IsSameStatusPatch(newStatusOperation, existingStatus))
            {
                return BuildSameStatusResponse(order);
            }

            var patchModel = BuildPatchModelFromOrder(order);

            patchDoc.ApplyTo(patchModel);

            var auditEmail = ResolveAuditEmailForPatch(order, patchModel);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                ApplyOrderPatch(order, patchModel, auditEmail, newReasonOp);

                await _unitOfWork.OrderRepository.UpdateOrderAsync(order);

                if (hasCancelItemAttrsOp)
                {
                    await AppendCancelOrderItemAttributesIfPresentAsync(order, patchModel, auditEmail);
                }

                var orderReturnId = Guid.NewGuid();

                if (patchModel.OrderReturn != null)
                {
                    await CreateOrderReturnIfPresentAsync(order, orderGuid, patchModel, auditEmail, orderReturnId);
                }

                if (patchModel.Addresses?.Count > 0 && order.BillToProfileId is not null)
                {
                    await CreateAddressesIfPresentAsync(order, patchModel, auditEmail);
                }

                if (patchModel.Payments?.Count > 0)
                {
                    await CreatePaymentsIfPresentAsync(order, patchModel, auditEmail);
                }

                if (patchModel.OrderRefund != null && patchModel.OrderRefund?.Count > 0)
                {
                    await CreateRefundsIfPresentAsync(order, orderGuid, patchModel, auditEmail);
                }

                if (patchModel.ExchangeTransactions?.Count > 0)
                {
                    await CreateExchangeTransactionsIfPresentAsync(order, patchModel, auditEmail);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return BuildSuccessResponse(order);
            }
            catch (Exception ex)
            {
                LogOrderUpdateFailed(_logger, orderId, ex.Message, ex);
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private static Microsoft.AspNetCore.JsonPatch.Operations.Operation<OrderPatchModel>? FindOperation(
        JsonPatchDocument<OrderPatchModel> patchDoc,
        string path)
        => patchDoc.Operations.FirstOrDefault(op => op.path.Equals(path, StringComparison.OrdinalIgnoreCase));

        private static bool HasOperation(JsonPatchDocument<OrderPatchModel> patchDoc, string path)
            => patchDoc.Operations.Any(op => op.path.Equals(path, StringComparison.OrdinalIgnoreCase));

        private static bool IsSameStatusPatch(
            Microsoft.AspNetCore.JsonPatch.Operations.Operation<OrderPatchModel>? newStatusOperation,
            string existingStatus)
            => newStatusOperation?.value is not null
               && newStatusOperation.value.ToString()!.Equals(existingStatus, StringComparison.OrdinalIgnoreCase);

        private static UpdateOrderResponse BuildOrderNotFoundResponse() => new()
        {
            OrderNumber = string.Empty,
            OrderId = string.Empty,
            OrderStatus = string.Empty,
            Message = OneExchangeOrderServiceConstants.OrderNotFoundUpdateMessage,
            IsUpdatedSuccessfully = false
        };

        private static UpdateOrderResponse BuildSameStatusResponse(Order order) => new()
        {
            OrderNumber = order.OrderNumber,
            OrderId = order.Id.ToString(),
            OrderStatus = order.OrderStatus,
            Message = OneExchangeOrderServiceConstants.OrderSameStatusUpdateMessage,
            IsUpdatedSuccessfully = false
        };

        private static UpdateOrderResponse BuildSuccessResponse(Order order) => new()
        {
            OrderNumber = order.OrderNumber,
            OrderId = order.Id.ToString(),
            OrderStatus = order.OrderStatus,
            Message = OneExchangeOrderServiceConstants.SuccessfulUpdateMessage,
            IsUpdatedSuccessfully = true
        };
        private static OrderPatchModel BuildPatchModelFromOrder(Order order) => new()
        {
            OrderStatus = order.OrderStatus,
            Memo = order.Memo ?? string.Empty,
            ExternalOrderReference = new ExternalOrderReference
            {
                ExternalSourceSystem = order.ExternalSourceSystem ?? string.Empty,
                ExternalReferenceId = order.ExternalReferenceId ?? string.Empty
            },
            StatusReason = order.StatusReason ?? string.Empty
        };

        private static void ApplyOrderPatch(
            Order order,
            OrderPatchModel patchModel,
            string auditEmail,
            Microsoft.AspNetCore.JsonPatch.Operations.Operation<OrderPatchModel>? newReasonOp)
        {
            order.OrderStatus = patchModel.OrderStatus;
            order.Memo = patchModel.Memo;
            order.ExternalSourceSystem = patchModel.ExternalOrderReference?.ExternalSourceSystem;
            order.ExternalReferenceId = patchModel.ExternalOrderReference?.ExternalReferenceId;
            order.UpdatedBy = auditEmail;
            order.UpdatedDateTime = DateTimeOffset.UtcNow;

            if (newReasonOp is not null && !string.IsNullOrWhiteSpace(patchModel.StatusReason))
            {
                order.StatusReason = patchModel.StatusReason;
            }
        }
        private async Task AppendCancelOrderItemAttributesIfPresentAsync(
        Order order,
        OrderPatchModel patchModel,
        string auditEmail)
        {
            var primaryOrderItem = order.OrderItems?.OrderBy(i => i.Sequence).FirstOrDefault();
            if (primaryOrderItem == null)
            {
                return;
            }

            var existingAttrs = primaryOrderItem.OrderItemAttributes ?? [];

            var existingTypes = BuildExistingAttributeTypes(existingAttrs);

            var incoming = BuildIncomingAttributes(patchModel);

            var nextSeq = existingAttrs.Count != 0 ? existingAttrs.Max(x => x.Sequence) + 1 : 0;

            var toAdd = BuildAttributesToAdd(order, primaryOrderItem.Id, incoming, existingTypes, ref nextSeq, auditEmail);

            if (toAdd.Count > 0)
            {
                await _unitOfWork.OrderItemAttributeRepository.AddRangeAsync(toAdd);
            }
        }

        private static HashSet<string> BuildExistingAttributeTypes(IEnumerable<OrderItemAttribute> existingAttrs)
        {
            var existingTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in existingAttrs)
            {
                var type = (e.AttributeType ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(type))
                {
                    existingTypes.Add(type);
                }
            }

            return existingTypes;
        }

        private static List<(string Type, string Value)> BuildIncomingAttributes(OrderPatchModel patchModel) => patchModel.OrderItemAttributes?
                .Where(a => !string.IsNullOrWhiteSpace(a.AttributeType))
                .Select(a => (Type: a.AttributeType!.Trim(), Value: (a.AttributeValue ?? string.Empty).Trim()))
                .GroupBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList()
                ?? [];

        private static List<OrderItemAttribute> BuildAttributesToAdd(
            Order order,
            Guid primaryOrderItemId,
            List<(string Type, string Value)> incoming,
            HashSet<string> existingTypes,
            ref int nextSeq,
            string auditEmail)
        {
            var toAdd = new List<OrderItemAttribute>();

            foreach (var a in incoming)
            {
                if (existingTypes.Contains(a.Type))
                {
                    continue;
                }

                toAdd.Add(new OrderItemAttribute
                {
                    Id = Guid.NewGuid(),
                    TenantId = order.TenantId,
                    OrderItemId = primaryOrderItemId,
                    AttributeType = a.Type,
                    AttributeValue = a.Value,
                    Sequence = nextSeq++,
                    InsertedBy = auditEmail,
                    InsertedDateTime = DateTime.Now
                });

                existingTypes.Add(a.Type);
            }

            return toAdd;
        }

        private async Task CreateOrderReturnIfPresentAsync(
        Order order,
        Guid orderId,
        OrderPatchModel patchModel,
        string auditEmail,
        Guid orderReturnId)
        {
            var r = patchModel.OrderReturn!;

            var objOrderReturn = new Domain.Entities.ExchangeOrder.OrderReturn
            {
                Id = orderReturnId,
                OrderId = orderId,
                ReturnNumber = r.ReturnNumber,
                ReturnDate = r.ReturnDate,
                TenantId = order.TenantId,
                ReturnFeeCurrency = r.ReturnFeeCurrency,
                ReturnFeeAmount = r.ReturnFeeAmount,
                Memo = r.Memo,
                SalesPersonFirstName = r.SalesPersonFirstName,
                SalesPersonLastName = r.SalesPersonLastName,
                SalesPersonEmail = r.SalesPersonEmail,
                ExternalSourceSystem = r.ExternalSourceSystem,
                ExternalReferenceId = r.ExternalReferenceId,
                InsertedBy = auditEmail,
                UpdatedBy = auditEmail,
                InsertedDateTime = DateTime.Now,
                UpdatedDateTime = DateTime.Now,
                WaiveFee = r.WaiveFee
            };

            await _unitOfWork.OrderReturnRepository.CreateOrderReturnsAsync(objOrderReturn);
        }

        private async Task CreateAddressesIfPresentAsync(
            Order order,
            OrderPatchModel patchModel,
            string auditEmail)
        {
            var profileId = (Guid)order.BillToProfileId!;

            var addresses = patchModel.Addresses!.Select(add => new Address
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                Sequence = add.Sequence ?? 0,
                AddressType = OneExchangeOrderServiceConstants.Billing,
                AddressPurpose = OneExchangeOrderServiceConstants.Primary,
                Line1 = add.Line1,
                Line2 = add.Line2,
                Line3 = add.Line3,
                City = add.City,
                State = add.State,
                Province = add.Province,
                Postalcode = add.PostalCode,
                Country = add.Country,
                InsertedBy = auditEmail
            }).ToList();

            await _unitOfWork.AddressRepository.CreateAddressesAsync(addresses);
        }

        private async Task CreatePaymentsIfPresentAsync(
        Order order,
        OrderPatchModel patchModel,
        string auditEmail)
        {
            var orderPayments = patchModel.Payments!.Select(p => new OrderPayment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                TenantId = order.TenantId,
                PaymentPurpose = p.PaymentPurpose,
                Currency = p.Currency,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentAccountToken = p.PaymentAccountToken,
                InsertedBy = auditEmail
            }).ToList();

            await _unitOfWork.OrderPaymentRepository.CreateOrderPaymentsAsync(orderPayments);
        }

        private async Task CreateRefundsIfPresentAsync(
        Order order,
        Guid orderId,
        OrderPatchModel patchModel,
        string auditEmail)
        {
            var orderRefund = patchModel.OrderRefund!.Select(o => new Domain.Entities.ExchangeOrder.OrderRefund
            {
                Id = Guid.NewGuid(),
                TenantId = order.TenantId,
                OrderId = orderId,
                OriginalPaymentId = o.OriginalPaymentId,
                RefundPurpose = o.RefundPurpose,
                Currency = o.Currency,
                Amount = o.Amount,
                RefundDate = DateTime.Now,
                InsertedBy = auditEmail,
                UpdatedBy = auditEmail,
                InsertedDateTime = DateTime.Now,
                UpdatedDateTime = DateTime.Now
            }).ToList();

            await _unitOfWork.OrderRefundRepository.CreateOrderRefundAsync(orderRefund);
        }

        private async Task CreateExchangeTransactionsIfPresentAsync(
        Order order,
        OrderPatchModel patchModel,
        string auditEmail)
        {
            foreach (var tx in patchModel.ExchangeTransactions!)
            {
                var entity = new ExchangeTransactionLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = order.TenantId,
                    OrderId = order.Id,
                    TransactionDate = tx.TransactionDate,
                    TransactionType = tx.TransactionType,
                    FromAccountId = tx.FromAccountId,
                    FromCurrency = tx.FromCurrency,
                    FromTransactionAmount = tx.FromTransactionAmount,
                    FromConfirmationNumber = tx.FromConfirmationNumber,
                    ToAccountId = tx.ToAccountId,
                    ToCurrency = tx.ToCurrency,
                    ToTransactionAmount = tx.ToTransactionAmount,
                    ToConfirmationNumber = tx.ToConfirmationNumber,
                    TransactionReferenceId = Guid.TryParse(tx.TransactionReferenceId, out var guid) ? guid : Guid.NewGuid(),
                    Memo = tx.Memo,
                    InsertedBy = auditEmail
                };

                await _unitOfWork.ExchangeTransactionLogRepository.CreateTransactionAsync(entity);
            }
        }
    }
}
