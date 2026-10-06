using APITransformationsFunction.Model.Reservation.Create.Request;
using APITransformationsFunction.Model.Reservation.Get;
using APITransformationsFunction.Model.Reservation.Search;
using APITransformationsFunction.Model.Shared;
using System.Threading.Tasks;

namespace APITransformationsFunction.Services.Interfaces
{
    public interface IReservationService
    {
        Task<CommonResponseDto<OTAGetReservationResponse>> GetReservationByIdAsync(string reservationId);
        Task<CommonResponseDto<OTACreateReservationResponse>> CreateReservationAsync(OTACreateReservationRequest otaRequest);
        Task<CommonResponseDto<OTASearchReservationResponse>> SearchReservationAsync(OTASearchReservationRequest otaRequest, int page, int pageSize);
    }
}
