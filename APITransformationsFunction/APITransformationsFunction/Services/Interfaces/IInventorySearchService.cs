using APITransformationsFunction.Model.Search;
using APITransformationsFunction.Model.Shared;
using System.Threading.Tasks;

namespace APITransformationsFunction.Services.Interfaces
{
    public interface IInventorySearchService
    {
        public Task<CommonResponseDto<OTAInventorySearchResponse>> InventorySearchAsync(OTAInventorySearchRequest otaRequest, int page, int pageSize);
    }
}
