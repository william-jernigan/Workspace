using APITransformationsFunction.Model.Cancellation;
using APITransformationsFunction.Model.Shared;
using System.Threading.Tasks;

namespace APITransformationsFunction.Services.Interfaces
{
    public interface ICancellationService
    {
        public Task<CommonResponseDto<OTACancelPostResponse>> CancelReservationAsync(OTACancelPostRequest otaRequest, string reservationId);
    }
}
