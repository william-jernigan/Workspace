using Asp.Versioning;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using OneExchangeOrderService.Application.Interfaces;
using OneExchangeOrderService.Domain;
using OneExchangeOrderService.Domain.Exceptions;
using OneExchangeOrderService.Domain.Models;
using OneExchangeOrderService.Domain.Models.PostOrderModels;
using OneExchangeOrderService.Domain.Models.UpdateOrder.Request;
using OneExchangeOrderService.DTO.Request;
using Swashbuckle.AspNetCore.Annotations;

namespace OneExchangeOrderService.WebApi.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class OrderController(
        IOrderService orderService) : ControllerBase
    {
        private readonly IOrderService _orderService = orderService;

        /// <summary>
        /// To get order details
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet]
        [SwaggerOperation(Tags = ["orders"], OperationId = "orders")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OrderResponse>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetOrders([FromQuery] OrderRequest request)
        {
            var orders = await _orderService.GetOrdersAsync(request);
            return Ok(orders);
        }
        /// <summary>
        /// Get transaction history for a customer
        /// </summary>
        [HttpGet("transactions")]
        [SwaggerOperation(Tags = ["orders"], OperationId = "transactionHistory")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TransactionHistoryResponse>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<IEnumerable<TransactionHistoryResponse>>> GetTransactionHistory([FromQuery] TransactionHistoryRequest request)
        {
            var result = await _orderService.GetTransactionHistoryAsync(request);
            return Ok(result);
        }

        [HttpPost]
        [SwaggerOperation(Tags = ["orders"], OperationId = "createOrder")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderReservationRequest request)
        {
            var orderNumber = await _orderService.CreateOrderAsync(request);
            if (string.IsNullOrEmpty(orderNumber))
            {
                // Return 200 OK with empty order number
                return Ok(new { orderNumber = string.Empty });
            }
            return Ok(new { orderNumber });
        }
        /// <summary>
        /// To get order details by orderid.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>

        [HttpGet("{order-id}")]
        [SwaggerOperation(Tags = ["Retrieve"], OperationId = "retrieveOrderData")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult> GetOrder([FromRoute(Name = "order-id")] string orderId)
        {
            var order = await _orderService.GetOrderAsync(orderId);
            return Ok(order);
        }

        // TODO This endpoint will be removed later
        [HttpGet("details/{orderId}")]
        [SwaggerOperation(Tags = ["orders"])]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderDataResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult> GetOrderDetails([FromRoute] string orderId)
        {
            var order = await _orderService.GetOrderDetailsAsync(orderId);
            return Ok(order);
        }
        /// <summary>
        /// To get order status by order number.
        /// </summary>
        /// <param name="orderNumber">Order number</param>
        /// <returns>Order status</returns>
        [HttpGet("{orderNumber}/status")]
        [SwaggerOperation(Tags = ["orders"], OperationId = "getOrderStatus")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult> GetOrderStatus([FromRoute] string orderNumber) => await _orderService.GetOrderStatusAsync(orderNumber);
        /// <summary>
        /// To get order status by order number.
        /// </summary>
        /// <param name="orderNumber">Order number</param>
        /// <returns>Order status</returns>
        [HttpGet("{orderNumber}/cancel/status")]
        [SwaggerOperation(Tags = ["orders"], OperationId = "getCancelOrderStatus")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult> GetCancelOrderStatus([FromRoute] string orderNumber) => await _orderService.GetCancelOrderDetailsAsync(orderNumber);

        /// <summary>
        /// To update order.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpPatch("{order-id}/status/update")]
        [SwaggerOperation(Tags = ["patch"], OperationId = "updateOrderStatus")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult> UpdateOrderStatusAsync([FromRoute(Name = "order-id")] string orderId, [FromBody] OrderPatchRequest request)
        {
            var orderNumber = await _orderService.UpdateOrderAsync(orderId, request);
            if (string.IsNullOrEmpty(orderNumber))
            {
                // Return 200 OK with empty order number
                return Ok(new { orderNumber = string.Empty });
            }
            return Ok(new { orderNumber });
        }

        [HttpPatch("{orderId}")]
        [Consumes("application/json-patch+json")]
        [SwaggerOperation(Tags = ["orders"], OperationId = "updateOrderById")]
        public async Task<IActionResult> UpdateOrderById([FromRoute] string orderId, [FromBody] JsonPatchDocument<OrderPatchModel> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest("Invalid patch document.");
            }

            var result = await _orderService.ApplyPatchToOrderAsync(orderId, patchDoc);

            if (result == null)
            {
                return NotFound(new { message = $"Order {orderId} not found." });
            }

            return Ok(result);
        }

        /// <summary>
        /// To update comments from Transaction Search.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpPatch("{order-id}/comment/update")]
        [SwaggerOperation(Tags = ["patch"], OperationId = "updateOrderComment")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult> UpdateOrderCommentAsync([FromRoute(Name = "order-id")] string orderId, [FromBody] UpdateOrderComment request)
        {
            var updateStatus = await _orderService.UpdateOrderCommentAsync(orderId, request);
            if (!updateStatus)
            {
                throw new DataNotFoundException($"Update failed for orderId {orderId}.");
            }
            return Ok(new { message = "Success" });
        }

        [HttpPost("{orderId}/email")]
        public async Task<IActionResult> GetBookingDetails([FromRoute] string orderId)
        {
            var result = await _orderService.GetBookingDetailsAsync(orderId);
            if (result == null)
            {
                return NotFound(new { Message = "No booking found for the provided confirmation number." });
            }

            return Ok(result);
        }

        /// <summary>
        /// To update refund transaction.
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpPatch("refund/update")]
        [SwaggerOperation(Tags = ["patch"], OperationId = "updateRefund")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult> UpdateRefund([FromBody] UpdateRefundRequest request)
        {
            var updateStatus = await _orderService.UpdateRefundAsync(request);
            if (!updateStatus)
            {
                throw new DataNotFoundException($"Refund update failed for orderId {request.OrderId}.");
            }
            return Ok(updateStatus);
        }

    }

}

