using ExchangeReservationFunction.Model.Reservation.Create.Request;
using ExchangeReservationFunction.Model.Reservation.GetReservation;
using ExchangeReservationFunction.Model.Reservation.Search;
using ExchangeReservationFunction.Model.Common;
using System.Threading.Tasks;

namespace ExchangeReservationFunction.Services.Interfaces
{
    public interface IReservationService
    {
        public Task<CommonResponseDto<OTAGetReservationResponse>> GetReservationByIdAsync(string reservationId);
        public Task<CommonResponseDto<OTACreateReservationResponse>> CreateReservationAsync(OTACreateReservationRequest otaRequest);
        public Task<CommonResponseDto<OTASearchReservationResponse>> SearchReservationAsync(OTASearchReservationRequest otaRequest, int page, int pageSize);
    }
}
