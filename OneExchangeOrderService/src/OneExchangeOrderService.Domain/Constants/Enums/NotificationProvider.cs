using System.ComponentModel;

namespace OneExchangeOrderService.Domain.Constants.Enums
{
    public enum NotificationProvider
    {
        [Description("adobemc")]
        Adobe = 0,

        [Description("smsprovider")]
        Sms = 1,

        [Description("emailservice")]
        Email = 2
    }
}