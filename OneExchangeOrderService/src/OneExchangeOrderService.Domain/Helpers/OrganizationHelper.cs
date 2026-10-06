using OneExchangeOrderService.Domain.Constants;

namespace OneExchangeOrderService.Domain.Helpers
{
    public static class OrganizationHelper
    {
        // TODO This is a Temporary solution which will be replaced Post MVP.
        public static Guid GetTenantGuid(string tenantCode) =>
            tenantCode switch
            {
                var code when string.Equals(code, OneExchangeOrderServiceConstants.BgvTenantCode, StringComparison.OrdinalIgnoreCase)
                    => Guid.Parse(OneExchangeOrderServiceConstants.BgvTenantGuidId),

                // Add more tenant mappings here as needed
                _ => Guid.NewGuid(),
            };

        public static Guid GetTradingAgreementGuid(string partnerCode)
        {
            if (!string.IsNullOrEmpty(partnerCode) &&
                string.Equals(partnerCode, OneExchangeOrderServiceConstants.ChoPartnerCode, StringComparison.OrdinalIgnoreCase))
            {
                return Guid.Parse(OneExchangeOrderServiceConstants.ChoTradingAgreementGuidId);
            }
            if (!string.IsNullOrEmpty(partnerCode) &&
                string.Equals(partnerCode, OneExchangeOrderServiceConstants.GpxPartnerCode, StringComparison.OrdinalIgnoreCase))
            {
                return Guid.Parse(OneExchangeOrderServiceConstants.GpxTradingAgreementGuidId);
            }

            return Guid.NewGuid();
        }
    }
}
