using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Search;
using Microsoft.Extensions.Configuration;
using OneExchangeOrderService.Application.Constants;
using OneExchangeOrderService.Application.Interfaces.Logic;
using OneExchangeOrderService.Application.Models.Resort;
using OneExchangeOrderService.Domain.Constants;
using OneExchangeOrderService.Domain.Helpers;
namespace OneExchangeOrderService.Application.Logic
{
    public class ContentfulService : IContentfulService
    {
        private readonly ContentfulClient _deliveryClient;
        private readonly IConfiguration _config;

        public ContentfulService(IConfiguration configuration)
        {
            _config = configuration;
            var deliveryToken = configuration["ContentfulSettings:DeliveryToken"];
            var spaceId = configuration["ContentfulSettings:SpaceId"];
            var environmentId = configuration["ContentfulSettings:EnvironmentId"] ?? "master";
            var deploymentEnvironment = (CommonHelper.GetSetting("DeploymentEnvironment", _config) ?? string.Empty).Trim();
            var isStgEnv = string.Equals(deploymentEnvironment, OneExchangeOrderServiceConstants.StgDeploymentEnviornment, StringComparison.OrdinalIgnoreCase);

            var options = new ContentfulOptions();

            if (!isStgEnv)
            {
                options = new ContentfulOptions
                {
                    DeliveryApiKey = deliveryToken,
                    SpaceId = spaceId,
                    Environment = environmentId
                };
            }
            else
            {
                options = new ContentfulOptions
                {
                    PreviewApiKey = deliveryToken,
                    SpaceId = spaceId,
                    Environment = environmentId,
                    UsePreviewApi = true
                };
            }



            _deliveryClient = new ContentfulClient(new HttpClient(), options);
        }
        public async Task<ResortPage> GetResortDetailsAsync(string resortCode)
        {
            var queryBuilder = QueryBuilder<ResortPage>.New
                .ContentTypeIs(Resort.ResortPage)
                .Include(10);

            if (!string.IsNullOrEmpty(resortCode))
            {
                queryBuilder = queryBuilder.FieldIncludes(Resort.ResortCodeField, [resortCode]);
            }

            queryBuilder = queryBuilder
                .OrderBy(Resort.ResortDisplayField)
                .Limit(1);

            var resort = (await _deliveryClient.GetEntries(queryBuilder)).SingleOrDefault();
            return resort;
        }
    }

}
