
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OneExchangeOrderService.DTO.Request
{
    [ExcludeFromCodeCoverage]
    public class UpdateOrderComment
    {

        [Required]
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;
    }
}