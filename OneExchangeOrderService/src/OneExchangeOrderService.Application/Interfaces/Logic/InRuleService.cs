using Microsoft.Extensions.Logging;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.DTO.Request;
using OneExchangeOrderService.DTO.Response;
using System.Net.Http.Json;
using System.Globalization;

namespace OneExchangeOrderService.Application.Interfaces.Logic
{
    public class InRuleService(IHttpClientFactory httpClientFactory, ILogger<InRuleService> logger) : IInRuleService
    {
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient(OneExchangeOrderServiceConstants.InRuleClient);
        private readonly ILogger<InRuleService> _logger = logger;

        /// <summary>
        /// To get cancellation fee.
        /// </summary>
        /// <param name="orderDate"></param>
        /// <param name="checkInDate"></param>
        /// <param name="membershipLevel"></param>
        /// <param name="absoluteDeadline"></param> 
        /// <returns></returns>
        public async Task<CancelFeeResponse> GetCancellationFee(DateTime? orderDate, DateTime? checkInDate, string membershipLevel, string? absoluteDeadline)
        {
            //Check particular reservation is having strict cancellation, absolute deadline passed or not.
            if (!string.IsNullOrEmpty(absoluteDeadline))
            {
                //Check absolute deadline of reservation 
                if (DateTime.TryParse(absoluteDeadline, out var deadline) && DateTime.Compare(DateTime.Now, deadline) > 0)
                {
                    return new CancelFeeResponse()
                    {
                        CancellationFee = 0,  // Fee will be zero, in case of deadline cross.
                        IsRefundPoints = false,
                    };
                }
            }

            //In rule api.
            const string tenantCode = OneExchangeOrderServiceConstants.BgvTenantCode;
            const string buyerPartnerCode = OneExchangeOrderServiceConstants.BGVBuyerPartnerCode;
            const string allSupplierPartnerCode = OneExchangeOrderServiceConstants.AllSupplierPartnerCode;

            var tradingAgreementCode = $"{tenantCode}-{buyerPartnerCode}-{allSupplierPartnerCode}";

            //Prepare cancel fees request object.
            var request = new CancelFeeRequest
            {
                TradingAgreementCode = tradingAgreementCode,
                MembershipClub = OneExchangeOrderServiceConstants.MembershipClub,
                MembershipLevel = membershipLevel,
                ReservationTime = Convert.ToDateTime(orderDate, CultureInfo.InvariantCulture),
                CurrentTime = DateTime.Now,
                CheckInDate = Convert.ToDateTime(checkInDate, CultureInfo.InvariantCulture).ToShortDateString()
            };

            _logger.LogInformation("{Prefix} Calling In-Rule Cancel fee API", OneExchangeOrderServiceConstants.InRulePrefix);

            var cancelDetails = await PostAsync<CancelFeeRequest, CancelFeeResponseWrapper, CancelFeeResponse>(
                OneExchangeOrderServiceConstants.CancellationFee,
                request,
                result => result?.Response ?? new CancelFeeResponse
                {
                    CancellationFee = -1,
                    IsRefundPoints = false,
                    Messages =
                    [
                        new ValueAttributeResponse
                        {
                            Value = "No response from In-Rule Cancel API."
                        }
                    ]
                });
            return cancelDetails;
        }

        private async Task<TOut> PostAsync<TIn, TWrapped, TOut>(string decisionName, TIn request, Func<TWrapped, TOut> map)
        {
            var response = await _httpClient.PostAsync(decisionName, JsonContent.Create(request));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("InRule API call to {Path} failed: {StatusCode}", decisionName, response.StatusCode);
                throw new HttpRequestException($"InRule call to {decisionName} failed: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<TWrapped>();
            return map(result!);
        }
    }
}
