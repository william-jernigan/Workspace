using APITransformationsFunction.Model.Property;
using APITransformationsFunction.Model.Shared;
using System.Threading.Tasks;

namespace APITransformationsFunction.Services.Interfaces
{
    public interface IPropertyService
    {
        public Task<CommonResponseDto<OTAPropertySearchResponse>> GetPropertiesAsync(OTAPropertySearchRequest otaRequest, int page, int pageSize);
        public Task<CommonResponseDto<OTAPropertySearchResponse>> GetPropertyByCodeAsync(string propertyCode, string includeMedia);
    }
}

