using APITransformationsFunction.Model.Member;
using APITransformationsFunction.Model.Shared;
using Microsoft.AspNetCore.JsonPatch;
using System.Threading.Tasks;

namespace APITransformationsFunction.Services.Interfaces
{
    public interface IMemberService
    {
        Task<CommonResponseDto<OTAGetMemberResponse>> GetMemberByIdAsync(string memberId);

        Task<CommonResponseDto<OTAPostMemberResponse>> CreateMemberAsync(OTAPostMemberRequest otaRequest);
        Task<CommonResponseDto<OTAPostMemberResponse>> UpdateMemberAsync(string memberId, JsonPatchDocument<OTAGetMemberResponse> patchDoc);
    }
}


