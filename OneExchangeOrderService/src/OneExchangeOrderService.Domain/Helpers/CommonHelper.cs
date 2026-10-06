using Microsoft.Extensions.Configuration;
using NanoidDotNet;
namespace OneExchangeOrderService.Domain.Helpers
{
    public static class CommonHelper
    {
        public static string? GetCodeFromItemCode(string itemCode, string codeType)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                return null;
            }

            var parts = itemCode.Split('-');

            // Expected formats:
            // Format 1: partner-resort-unit-subunit → "GP-GP01-1-1" → parts[0], [1], [2], [3]
            // Format 2: partner-resort-unit → "CH-WY044-BNK" → parts[0], [1], [2]

            return codeType switch
            {
                "PartnerCode" => parts.Length >= 1 ? parts[0] : string.Empty,
                "ResortCode" => parts[1],
                "UnitType" => parts.Length >= 3 ? parts[2] : string.Empty,
                "SubUnitType" => parts.Length >= 4 ? parts[3] : string.Empty,
                "CompleteResortCode" => parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : string.Empty,
                _ => string.Empty,
            };
        }

        public static string? GetCodeFromTradingAgreement(string tradingAgreementCode, string codeType)
        {
            if (string.IsNullOrWhiteSpace(tradingAgreementCode))
            {
                return null;
            }

            var parts = tradingAgreementCode.Split('-');

            // Expected formats:
            // Format 1: {Tenant Code}-{Buyer Partner Code}-{Supplier Partner Code} → "BGV-BGV-CHO" → parts[0], [1], [2]

            return codeType switch
            {
                "TenantCode" => parts.Length >= 1 ? parts[0] : string.Empty,
                "BuyerPartnerCode" => parts[1],
                "SupplierPartnerCode" => parts.Length >= 3 ? parts[2] : string.Empty,
                _ => string.Empty,
            };
        }

        public static Task<string> GenerateNanoId()
        {
            var nanoId = Nanoid.GenerateAsync("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", 8);
            return nanoId;
        }

        public static string? GetSetting(string key, IConfiguration config) =>
                // 1) Real env var (Azure + local host flattens Values -> env vars)
                Environment.GetEnvironmentVariable(key)
                // 2) local.settings.json -> Values:<key>
                ?? config[$"Values:{key}"]
                // 3) fallback if you ever load a json without "Values" wrapper
                ?? config[key];
    }
}
