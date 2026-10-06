using ExchangeReservationFunction.Model.Cancellation;
using ExchangeReservationFunction.Model.Common;
using System.Threading.Tasks;

namespace ExchangeReservationFunction.Services.Interfaces
{
    public interface ICancellationService
    {
        public Task<CommonResponseDto<OTACancelPostResponse>> CancelReservationAsync(OTACancelPostRequest otaRequest, string reservationId);
    }
}
