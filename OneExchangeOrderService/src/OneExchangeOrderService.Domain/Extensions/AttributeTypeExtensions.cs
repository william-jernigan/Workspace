
using OneExchangeOrderService.Domain.Constants.Enums;

namespace OneExchangeOrderService.Domain.Extensions
{
    public static class AttributeTypeExtensions
    {
        public static string ToValue(this PropertyEnumAttributes type) => type switch
        {
            PropertyEnumAttributes.PropertyName => "PROPERTY_NAME",
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unhandled attribute type: {type}")
        };

        public static PropertyEnumAttributes FromValue(string value) => value switch
        {
            "PROPERTY_NAME" => PropertyEnumAttributes.PropertyName,
            _ => throw new ArgumentOutOfRangeException(nameof(value), $"Invalid attribute type: {value}")
        };
    }
}
