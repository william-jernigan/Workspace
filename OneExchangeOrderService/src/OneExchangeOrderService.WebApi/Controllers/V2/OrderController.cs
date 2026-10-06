using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OneExchangeOrderService.Application.Interfaces;
using OneExchangeOrderService.Domain.Models.V2.Cancellation.Request;
using OneExchangeOrderService.Domain.Models.V2.Reservation.Request;
using OneExchangeOrderService.Domain.Models.V2.Retrieval.Request;
using System.Net.Mime;

namespace OneExchangeOrderService.WebApi.Controllers.V2
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/[controller]")]
    public class OrderController(
        IOrderService orderService,
        ILogger<OrderController> logger) : ControllerBase
    {
        private readonly IOrderService _orderService = orderService;
        private readonly ILogger<OrderController> _logger = logger;

        /// <summary>
        /// Create reservation
        /// </summary>
        /// <param name="request">ReservationRequest</param>
        /// <param name="ct">CancellationToken</param>
        [HttpPost("reservations")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> CreateReservation([FromBody] ReservationRequest request, CancellationToken ct)
        {
            _logger.LogInformation("V2 CreateReservation called. ce.id={Id}, src={Src}, type={Type}", request.Id, request.Source, request.Type);

            var response = await _orderService.CreateReservationAsync(request, ct);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpPost("reservations/{bookingID}/cancel")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> CancelReservation([FromRoute] string bookingID, [FromBody] CancellationRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(bookingID))
            {
                return BadRequest("BookingId is required.");
            }
            _logger.LogInformation("V2 CancelReservation called. cr.id={Id}, src={Src}, type={Type}", request.Id, request.Source, request.Type);

            var response = await _orderService.CancelReservationAsync(bookingID, request, ct);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpPost("reservations/search")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> SearchReservationsAysnc([FromBody] ReservationSearchRequest request, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            _logger.LogInformation("V2 SearchReservations called. ce.id={Id}, src={Src}, type={Type}", request.Id, request.Source, request.Type);

            var response = await _orderService.SearchReservationsAsync(request, page, pageSize, ct);
            return StatusCode(StatusCodes.Status200OK, response);
        }

        [HttpGet("reservations/{reservationID}")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> GetReservationByIdAsync([FromRoute] string reservationID, CancellationToken ct)
        {
            _logger.LogInformation("V2 GetReservationById called. reservationId={reservationID}", reservationID);

            var response = await _orderService.GetReservationByIdAsync(reservationID, ct);
            return StatusCode(StatusCodes.Status200OK, response);
        }
    }
}
