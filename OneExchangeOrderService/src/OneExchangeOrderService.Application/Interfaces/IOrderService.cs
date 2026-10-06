using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using OneExchangeOrderService.Domain;
using OneExchangeOrderService.Domain.Entities.ExchangeOrder;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Models.Eventhub.BookingAlternateEmail;
using OneExchangeOrderService.Domain.Models.PostOrderModels;
using OneExchangeOrderService.Domain.Models.UpdateOrder.Request;
using OneExchangeOrderService.Domain.Models.V2.Cancellation.Request;
using OneExchangeOrderService.Domain.Models.V2.Cancellation.Response;
using OneExchangeOrderService.Domain.Models.V2.Reservation.Request;
using OneExchangeOrderService.Domain.Models.V2.Reservation.Response;
using OneExchangeOrderService.Domain.Models.V2.Retrieval.Request;
using OneExchangeOrderService.Domain.Models.V2.Retrieval.Response;
using OneExchangeOrderService.DTO.Request;

namespace OneExchangeOrderService.Application.Interfaces
{
    public interface IOrderService
    {
        public Task<IEnumerable<OrderResponse>> GetOrdersAsync(OrderRequest request);
        public Task<string> CreateOrderAsync(OrderReservationRequest request);
        Task<List<TransactionHistoryResponse>> GetTransactionHistoryAsync(TransactionHistoryRequest request);
        public Task<OrderResponse> GetOrderAsync(string orderId);
        public Task<OrderDataResponse> GetOrderDetailsAsync(string orderId);
        Task<ActionResult> GetOrderStatusAsync(string orderNumber);

        Task<ActionResult> GetCancelOrderDetailsAsync(string orderNumber);
        public Task<Order?> GetOrderByOrderNumberAsync(string orderNumber);
        public Task<UpdateOrderResponse> ApplyPatchToOrderAsync(string orderId, JsonPatchDocument<OrderPatchModel> patchDoc);
        Task<bool> CheckBookingFeeEligibilityAsync(string memberId, string fromDate);
        public Task<string> UpdateOrderAsync(string orderId, OrderPatchRequest request);
        public Task<bool> UpdateOrderCommentAsync(string orderId, UpdateOrderComment request);
        Task<OrderDetailsResponse?> GetBookingDetailsAsync(string orderId);
        public Task<bool> UpdateRefundAsync(UpdateRefundRequest request);
        Task<ReservationResponse> CreateReservationAsync(ReservationRequest request, CancellationToken ct);
        public Task<CancellationResponse> CancelReservationAsync(string bookingID, CancellationRequest request, CancellationToken ct);
        public Task<ReservationSearchResponse> SearchReservationsAsync(ReservationSearchRequest request, int page, int pageSize, CancellationToken ct);
        public Task<ReservationGetResponse> GetReservationByIdAsync(string reservationID, CancellationToken ct);
    }
}