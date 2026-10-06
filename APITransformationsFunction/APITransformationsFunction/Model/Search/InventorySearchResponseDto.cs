#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace APITransformationsFunction.Model.Search
{
    [ExcludeFromCodeCoverage]
    public class InventorySearchResponseDto<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public OTAErrorResponse? Error { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class OTAErrorResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
    }
}
