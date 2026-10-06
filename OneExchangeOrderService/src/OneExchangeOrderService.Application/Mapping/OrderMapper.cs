using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Models;

namespace OneExchangeOrderService.Application.Mapping
{
    public static class OrderMapper
    {
        public static OrderResponse MapToResponse(Order order)
        {
            if (order == null)
            {
                return null;
            }

            return new OrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderStatus = order.OrderStatus,
                Subtotal = order.Subtotal,
                OrderFee = order.OrderFee,
                SalesPersonFirstName = order.SalesPersonFirstName,
                SalesPersonLastName = order.SalesPersonLastName,
                CustomerExternalReferenceId = order.CustomerExternalReferenceId,
                ProfileId = order.OrderGuests?.Select(g => g.ProfileId).ToList(),
                OrderDate = order.OrderDate,
                CancelDate = order.CancelDate,
                SalesPersonEmailAddress = order.SalesPersonEmail,
                CustomerFirstName = order.CustomerFirstName,
                CustomerLastName = order.CustomerLastName,
                CustomerEmailAddress = order.CustomerEmailAddress,
                CustomerPhoneNumber = order.CustomerPhoneNumber,
                TaxAmount = order.TaxAmount,
                Memo = order.Memo,
                IsUserEligibleForBookingFeeWaiver = order.WaiveFee,

                ExchangeTransactions = order.ExchangeTransactionLogs?.Select(log => new ExchangeTransactionLogDetails
                {
                    Id = log.Id,
                    TenantId = log.TenantId,
                    OrderId = log.OrderId,
                    TransactionDate = log.TransactionDate,
                    TransactionType = log.TransactionType,
                    FromAccountId = log.FromAccountId,
                    FromCurrency = log.FromCurrency,
                    FromTransactionAmount = log.FromTransactionAmount,
                    ToAccountId = log.ToAccountId,
                    ToCurrency = log.ToCurrency,
                    ToTransactionAmount = log.ToTransactionAmount,
                    TransactionReferenceId = log.TransactionReferenceId,
                    Memo = log.Memo
                }).ToList(),

                OrderPayments = order.OrderPayments?.Select(p => new OrderPaymentResponse
                {
                    TenantId = p.TenantId,
                    OrderId = p.OrderId,
                    PaymentPurpose = p.PaymentPurpose,
                    Currency = p.Currency,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PaymentAccountToken = p.PaymentAccountToken
                }).ToList(),

                OrderRefunds = order.OrderReturn?.OrderRefunds?.Select(refund => new OrderRefundResponse
                {
                    TenantId = refund.TenantId,
                    OrderReturnId = refund.OrderReturnId,
                    RefundPurpose = refund.RefundPurpose,
                    OriginalPaymentId = refund.OriginalPaymentId,
                    SalesPersonEmail = refund.SalesPersonEmail,
                    SalesPersonFirstName = refund.SalesPersonFirstName,
                    SalesPersonLastName = refund.SalesPersonLastName,
                    Currency = refund.Currency,
                    Amount = refund.Amount,
                    RefundDate = refund.RefundDate,
                    RefundAccountToken = refund.RefundAccountToken
                }).ToList()
            };
        }
    }






}
