using Microsoft.EntityFrameworkCore;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Constants.Enums;
using OneExchangeOrderService.Domain.Entities.Common;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Models.Eventhub.BookingAlternateEmail;
using OneExchangeOrderService.Domain.Persistence.Repositories.ExchangeOrder;
using OneExchangeOrderService.Infrastructure.DbContexts;
using System.Globalization;

namespace OneExchangeOrderService.Infrastructure.Persistance.Repositories.ExchangeOrder
{
    public sealed class OrderRepository(ExchangeOrderDbContext context) : BaseExchangeOrderRepository<BaseEntity>(context), IOrderRepository
    {
        /// <summary>
        /// To get order details.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IEnumerable<OrderResponse>> GetOrdersAsync(OrderRequest request, List<string> orderStatus)
        {
            var orders = await context.Orders.AsNoTracking()
                .Where(x => x.CustomerExternalReferenceId == request.CustomerExternalReferenceId
                         && orderStatus.Contains(x.OrderStatus))
                .Select(order => new OrderResponse
                {
                    Id = order.Id,
                    OrderStatus = order.OrderStatus ?? string.Empty,
                    OrderNumber = order.ExternalReferenceId ?? string.Empty,
                    CancellationNumber = order.OrderReturn.ExternalReferenceId ?? string.Empty,
                    Subtotal = order.Subtotal,
                    OrderFee = order.OrderFee,
                    CancellationFee = order.OrderReturn.ReturnFeeAmount,
                    SalesPersonFirstName = order.SalesPersonFirstName ?? string.Empty,
                    SalesPersonLastName = order.SalesPersonLastName ?? string.Empty,
                    SalesPersonEmailAddress = order.SalesPersonEmail ?? string.Empty,
                    CustomerFirstName = order.CustomerFirstName ?? string.Empty,
                    CustomerLastName = order.CustomerLastName ?? string.Empty,
                    CustomerEmailAddress = order.CustomerEmailAddress ?? string.Empty,
                    OrderDate = order.OrderDate,
                    CancelDate = order.OrderReturn.ReturnDate,
                    Memo = order.Memo,
                    RefundPoints = order.OrderRefunds
                        .Where(x => x.RefundPurpose == OneExchangeOrderServiceConstants.RefundPointsPaymentPurposeString)
                        .Select(r => (decimal?)r.Amount)
                        .FirstOrDefault() ?? 0m,
                    ProfileId = order.OrderGuests
                        .Where(guest => guest.IsPrimary)
                        .Select(profile => profile.ProfileId)
                        .ToList(),
                    Item = order.OrderItems.Select(orderItem => new OrderItems
                    {
                        ItemDescription = orderItem.ItemDescription,
                        ItemCode = orderItem.ItemCode,
                        Attributes = orderItem.OrderItemAttributes.Select(orderAttribute => new OrderAttributeModel
                        {
                            AttributeType = orderAttribute.AttributeType,
                            AttributeValue = orderAttribute.AttributeValue,
                        }).ToList()
                    }).FirstOrDefault()
                })
                .ToListAsync();

            return orders;
        }

        public async Task<string> CreateOrderAsync(Order order)
        {
            Context.Orders.Add(order);
            return order.OrderNumber;
        }


        /// <summary>
        ///  To get order id.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Order> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var order = await context.Orders.Where(x => x.Id == orderId)
                     .Include(x => x.OrderItems)
                     .ThenInclude(x => x.OrderItemAttributes)
                     .Include(x => x.OrderGuests)
                     .Include(x => x.OrderPayments)
                     .Include(x => x.OrderReturn)
                     .Include(x => x.ExchangeTransactionLogs)
                     .Include(x => x.OrderRefunds)
                     .TagWith(nameof(GetOrderAsync)).FirstOrDefaultAsync(cancellationToken);

            return order;
        }
        /// <summary>
        /// To get order status by order number
        /// </summary>
        /// <param name="orderNumber"></param>
        /// <returns></returns>
        public async Task<(string? OrderStatus, string? ExternalReferenceId, string? orderId, string? StatusReason)?>
        GetOrderStatusAsync(string orderNumber)
        {
            var result = await context.Orders.AsNoTracking()
                .Where(x => x.OrderNumber == orderNumber)
                .Select(x => new
                {
                    x.OrderStatus,
                    x.Id,
                    x.ExternalReferenceId,
                    x.StatusReason
                })
                .SingleOrDefaultAsync();

            if (result == null)
            {
                return null;
            }
            var statusReason = string.IsNullOrEmpty(result.StatusReason)
                ? null
                : result.StatusReason;

            return (result.OrderStatus, result.ExternalReferenceId, result.Id.ToString(), statusReason);
        }

        public async Task<Order?> GetOrderByOrderNumberAsync(string orderNumber) => await Context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        public Task UpdateOrderAsync(Order order)
        {
            Context.Orders.Update(order);
            return Task.CompletedTask;
        }

        /// <summary>
        /// To check member exceeded waive fee limit for particular year.
        /// </summary>
        /// <param name="customerExternalReferenceId"></param>
        /// <returns></returns>
        public async Task<bool> HasExceededWaiverLimitForCurrentYearAsync(string customerExternalReferenceId, string fromDate)
        {
            if (string.IsNullOrWhiteSpace(customerExternalReferenceId))
            {
                return false;
            }

            if (!DateTime.TryParse(fromDate, out var startDate))
            {
                return false;
            }

            var yearToCheck = startDate.Year.ToString(CultureInfo.InvariantCulture);

#pragma warning disable CA1310 // Specify StringComparison for correctness
            var checkInDetails = await context.Orders
            .AsNoTracking()
            .Where(order =>
                order.CustomerExternalReferenceId == customerExternalReferenceId &&
                order.OrderStatus == OrderStatus.CONFIRMED.ToString())
              .Select(o => new
              {
                  CheckIn = o.OrderItems.SelectMany(oi => oi.OrderItemAttributes)
                            .Where(a => a.AttributeType == OneExchangeOrderServiceConstants.Checkin &&
                             a.AttributeValue.StartsWith(yearToCheck)).Select(a => a.AttributeValue)
                      .FirstOrDefault() ?? string.Empty,
              }).ToListAsync();
#pragma warning restore CA1310 // Cannot use string comparison due to EF core mapping issue

            var userBookingCount = checkInDetails.Count(x => x.CheckIn != string.Empty);

            return userBookingCount >= OneExchangeOrderServiceConstants.MaxWaivedBookingsPerYear;
        }

        /// <summary>
        /// To update an order.
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<string> UpdateOrderStatusAsync(Order order)
        {
            Context.Orders.Update(order).Property(x => x.ClustRowId).IsModified = false;
            return order.OrderNumber;
        }

        public async Task<IEnumerable<OrderResponse>> GetOrdersFreeTextSearchAsync(OrderRequest request)
        {
            var orders = await context.Orders.AsNoTracking()
                .Include(o => o.OrderReturn)
                .Include(o => o.OrderGuests)
                .Include(o => o.OrderPayments)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OrderItemAttributes)
                .Where(order =>
                    order.CustomerExternalReferenceId.Contains(request.FreeTextSearch) ||
                    order.OrderStatus.Contains(request.FreeTextSearch) ||
                    order.ExternalReferenceId.Contains(request.FreeTextSearch) ||
                    order.Id.ToString().Contains(request.FreeTextSearch) ||
                    (order.OrderReturn != null &&
                     order.OrderReturn.ExternalReferenceId.Contains(request.FreeTextSearch)) ||
                    order.OrderItems.Any(oi =>
                        oi.OrderItemAttributes.Any(attr =>
                            attr.AttributeType == $"{OneExchangeOrderServiceConstants.PropertyNameAttributeTypeString}" &&
                            attr.AttributeValue.Contains(request.FreeTextSearch))))
                            .Select(order => new OrderResponse()
                            {
                                Id = order.Id,
                                OrderStatus = order.OrderStatus ?? string.Empty,
                                OrderNumber = order.ExternalReferenceId ?? string.Empty,
                                CancellationNumber = order.OrderReturn != null ? order.OrderReturn.ExternalReferenceId ?? string.Empty : string.Empty,
                                ExternalReferenceId = order.ExternalReferenceId ?? string.Empty,
                                Subtotal = order.Subtotal,
                                OrderFee = order.OrderFee,
                                SalesPersonFirstName = order.SalesPersonFirstName ?? string.Empty,
                                SalesPersonLastName = order.SalesPersonLastName ?? string.Empty,
                                SalesPersonEmailAddress = order.SalesPersonEmail ?? string.Empty,
                                CustomerFirstName = order.CustomerFirstName ?? string.Empty,
                                CustomerLastName = order.CustomerLastName ?? string.Empty,
                                CustomerEmailAddress = order.CustomerEmailAddress ?? string.Empty,
                                CustomerExternalReferenceId = order.CustomerExternalReferenceId ?? string.Empty,
                                TaxAmount = order.TaxAmount,
                                OrderDate = order.OrderDate,
                                CancelDate = order.OrderReturn != null ? order.OrderReturn.ReturnDate : null,
                                Memo = order.Memo,
                                ProfileId = order.OrderGuests
                                                  .Where(guest => guest.IsPrimary)
                                                  .Select(profile => profile.ProfileId)
                                                  .ToList(),
                                Item = order.OrderItems.Select(oi => new OrderItems
                                {
                                    ItemDescription = oi.ItemDescription,
                                    ItemCode = oi.ItemCode,
                                    Attributes = oi.OrderItemAttributes
                                                   .Select(orderAttribute => new OrderAttributeModel
                                                   {
                                                       AttributeType = orderAttribute.AttributeType,
                                                       AttributeValue = orderAttribute.AttributeValue
                                                   }).ToList()
                                }).FirstOrDefault(),
                                ExchangeTransactions = order.ExchangeTransactionLogs.Where(etl => new[]
                                {
                                    OneExchangeOrderServiceConstants.ORDERFEE,
                                    OneExchangeOrderServiceConstants.CancellationFeeTransactionType,
                                    OneExchangeOrderServiceConstants.FEEREIMBURSEMENT
                                }
                                .Contains(etl.TransactionType))
                                .Select(etl => new ExchangeTransactionLogDetails
                                {
                                    Id = etl.Id,
                                    TenantId = etl.TenantId,
                                    OrderId = etl.OrderId,
                                    TransactionDate = etl.TransactionDate,
                                    TransactionType = etl.TransactionType,
                                    FromAccountId = etl.FromAccountId,
                                    FromCurrency = etl.FromCurrency,
                                    FromTransactionAmount = etl.FromTransactionAmount,
                                    ToAccountId = etl.ToAccountId,
                                    ToCurrency = etl.ToCurrency,
                                    ToTransactionAmount = etl.ToTransactionAmount,
                                    TransactionReferenceId = etl.TransactionReferenceId,
                                    Memo = etl.Memo
                                }).ToList(),
                                OrderPayments = order.OrderPayments.Where(op => new[]
                                {
                                    OneExchangeOrderServiceConstants.ORDERFEE,
                                    OneExchangeOrderServiceConstants.CancellationFeeTransactionType
                                }
                                .Contains(op.PaymentPurpose))
                                .Select(op => new OrderPaymentResponse
                                {
                                    Id = op.Id,
                                    TenantId = op.TenantId,
                                    OrderId = op.OrderId,
                                    PaymentPurpose = op.PaymentPurpose,
                                    Currency = op.Currency,
                                    Amount = op.Amount,
                                    PaymentDate = op.PaymentDate,
                                    PaymentAccountToken = op.PaymentAccountToken
                                }).ToList(),
                                OrderRefunds = order.OrderRefunds.Where(or => new[] {
                                   OneExchangeOrderServiceConstants.POINTSREFUND,
                                   OneExchangeOrderServiceConstants.FEEREIMBURSEMENT,
                                   OneExchangeOrderServiceConstants.FEEREFUND
                                }
                                .Contains(or.RefundPurpose))
                                .Select(or => new OrderRefundResponse
                                {
                                    TenantId = or.TenantId,
                                    OrderReturnId = or.OrderReturnId,
                                    RefundPurpose = or.RefundPurpose,
                                    Currency = or.Currency,
                                    Amount = or.Amount,
                                    RefundDate = or.RefundDate,
                                    OriginalPaymentId = or.OriginalPaymentId

                                }).ToList()
                            }).Take(100).ToListAsync();

            return orders;
        }



        public async Task<OrderDetailsResponse?> GetOrderDetailsByConfirmationNumberAsync(string orderId)
        {


            var order = await context.Orders
                .AsNoTracking()
                .Where(o => o.ExternalReferenceId == orderId)
                .Select(o => new OrderDetailsResponse
                {
                    BookingDate = o.OrderDate,
                    CheckIn = o.OrderItems
                        .SelectMany(oi => oi.OrderItemAttributes)
                        .Where(a => a.AttributeType == OneExchangeOrderServiceConstants.Checkin)
                        .Select(a => a.AttributeValue)
                        .FirstOrDefault() ?? string.Empty,
                    CheckOut = o.OrderItems
                        .SelectMany(oi => oi.OrderItemAttributes)
                        .Where(a => a.AttributeType == OneExchangeOrderServiceConstants.Checkout)
                        .Select(a => a.AttributeValue)
                        .FirstOrDefault() ?? string.Empty,
                    UnitType = o.OrderItems
                        .SelectMany(oi => oi.OrderItemAttributes)
                        .Where(a => a.AttributeType == OneExchangeOrderServiceConstants.UNITTYPE)
                        .Select(a => a.AttributeValue)
                        .FirstOrDefault() ?? string.Empty,
                    NumberOfAdults = o.OrderItems
                        .SelectMany(oi => oi.OrderItemAttributes)
                        .Where(a => a.AttributeType == OneExchangeOrderServiceConstants.NUMADULTS)
                        .Select(a => a.AttributeValue)
                        .FirstOrDefault() ?? string.Empty,
                    NumberOfChildren = o.OrderItems
                        .SelectMany(oi => oi.OrderItemAttributes)
                        .Where(a => a.AttributeType == OneExchangeOrderServiceConstants.NUMCHILDREN)
                        .Select(a => a.AttributeValue)
                        .FirstOrDefault() ?? string.Empty,
                    CancellationPolicy = o.OrderItems
                        .SelectMany(oi => oi.OrderItemAttributes)
                        .Where(a => a.AttributeType == OneExchangeOrderServiceConstants.PenaltyDescriptionAttributeTypeString)
                        .Select(a => a.AttributeValue)
                        .FirstOrDefault() ?? string.Empty,
                    ItemCode = o.OrderItems
                        .Select(oi => oi.ItemCode)
                        .FirstOrDefault() ?? string.Empty,
                    BookingFees = o.OrderFee,
                    Taxes = o.TaxAmount ?? 0,
                    TotalPoints = o.Subtotal,
                    ImportantInformation = o.Memo ?? string.Empty,
                    ProfileIds = o.OrderGuests.Select(g => g.ProfileId).ToList()
                })
                .FirstOrDefaultAsync();

            return order;
        }


        public async Task<Order?> GetOrderByExternalReferenceIdAsync(string externalReferenceId, CancellationToken cancellationToken = default)
        {
            var order = await context.Orders.Where(x => x.ExternalReferenceId == externalReferenceId)
                     .Include(x => x.OrderItems)
                     .ThenInclude(x => x.OrderItemAttributes)
                     .Include(x => x.OrderGuests)
                     .Include(x => x.OrderPayments)
                     .Include(x => x.OrderReturn)
                     .Include(x => x.ExchangeTransactionLogs)
                     .Include(x => x.OrderRefunds)
                     .TagWith(nameof(GetOrderByExternalReferenceIdAsync))
                     .FirstOrDefaultAsync(cancellationToken);

            return order;
        }

        public async Task<(IEnumerable<Order> Orders, int TotalCount)> SearchOrdersAsync(
        string? status,
        string? reservationId,
        List<Guid>? profileIds,
        DateTime? reservationDate,
        int? reservationDateInterval,
        DateTime? arrivalDate,
        int? arrivalDateInterval,
        int page,
        int pageSize,
        string? orderByField,
        string? orderByDirection,
        CancellationToken ct)
        {
            var query = BuildBaseQuery();

            query = ApplyBasicFilters(query, status, reservationId, profileIds, reservationDate, reservationDateInterval);

            var filteredOrders = ApplyArrivalFilter(query, arrivalDate, arrivalDateInterval);

            var ordered = ApplyOrdering(filteredOrders, orderByField, orderByDirection);

            var totalCount = ordered.Count();

            var pagedOrders = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedOrders, totalCount);
        }

        private IQueryable<Order> BuildBaseQuery() =>
             context.Orders.AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OrderItemAttributes)
                .Include(o => o.OrderGuests)
                .Where(o => o.ExternalSourceSystem == "HGV");

        private static IQueryable<Order> ApplyBasicFilters(
            IQueryable<Order> query,
            string? status,
            string? reservationId,
            List<Guid>? profileIds,
            DateTime? reservationDate,
            int? reservationDateInterval)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(reservationId))
            {
                query = query.Where(o => o.ExternalReferenceId == reservationId);
            }

            if (profileIds != null && profileIds.Count != 0)
            {
                query = query.Where(o =>
                    (o.BillToProfileId.HasValue && profileIds.Contains(o.BillToProfileId.Value)) ||
                    o.OrderGuests.Any(g => profileIds.Contains(g.ProfileId)));
            }

            if (reservationDate.HasValue)
            {
                var start = reservationDate.Value.Date;
                var end = start.AddDays(reservationDateInterval ?? 0).Date;
                query = query.Where(o => o.OrderDate >= start && o.OrderDate <= end);
            }

            return query;
        }
        private static List<Order> ApplyArrivalFilter(
            IQueryable<Order> query,
            DateTime? arrivalDate,
            int? arrivalDateInterval)
        {
            if (!arrivalDate.HasValue)
            {
                return [.. query];
            }
            var start = arrivalDate.Value.Date;
            var end = start.AddDays(arrivalDateInterval ?? 0).Date;

            var ordersTemp = query.ToList();

            return ordersTemp
                .Where(o => o.OrderItems.Any(oi =>
                    oi.OrderItemAttributes.Any(a =>
                        a.AttributeType == OneExchangeOrderServiceConstants.Checkin &&
                        DateTime.TryParse(a.AttributeValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var checkinDate) &&
                        checkinDate >= start && checkinDate <= end)))
                .ToList();
        }
        private static IEnumerable<Order> ApplyOrdering(
            List<Order> filteredOrders,
            string? orderByField,
            string? orderByDirection)
        {
            if (string.IsNullOrEmpty(orderByField))
            {
                return filteredOrders.OrderByDescending(o => o.OrderDate);
            }
            switch (orderByField.ToLower(CultureInfo.CurrentCulture))
            {
                case "arrivaldate":
                    var ordered = filteredOrders.OrderBy(o =>
                        o.OrderItems
                            .SelectMany(i => i.OrderItemAttributes)
                            .Where(a => a.AttributeType == OneExchangeOrderServiceConstants.Checkin)
                            .Select(a => DateTime.TryParse(a.AttributeValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : DateTime.MaxValue)
                            .DefaultIfEmpty(DateTime.MaxValue)
                            .FirstOrDefault());

                    return orderByDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true
                        ? ordered.Reverse()
                        : ordered;

                case "reservationdate":
                    return orderByDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true
                        ? filteredOrders.OrderByDescending(o => o.OrderDate)
                        : filteredOrders.OrderBy(o => o.OrderDate);

                default:
                    return filteredOrders.OrderByDescending(o => o.OrderDate);
            }
        }

    }
}