using GraphConnectorFunction.Interfaces;
using GraphConnectorFunction.Models;
using GraphConnectorFunction.Utilities;
using Microsoft.Graph.Models.ExternalConnectors;
using System.Text.Json;

namespace GraphConnectorFunction.Providers
{
    public class M365RoadmapService : IM365RoadmapService
    {
        private readonly HttpClient _client;
        private readonly string _apiBaseUrl = Constants.RoadmapFeaturesApiBaseUrl;
        public M365RoadmapService(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
        }

        public async Task<List<RoadmapFeature>> ExtractRoadmapFeaturesAsync()
        {
            List<RoadmapFeature> roadmapFeatures = new List<RoadmapFeature>();
            //concatenate the base url with the path into a single string
            Uri moduleEndPoint = new Uri(_apiBaseUrl);

            var response = await _client.GetAsync(moduleEndPoint);
            if (response.EnsureSuccessStatusCode().IsSuccessStatusCode)
            {
                var jsonText = await response.Content.ReadAsStringAsync();
                roadmapFeatures = JsonSerializer.Deserialize<List<RoadmapFeature>>(jsonText);
            }
            else
            {
                throw new Exception($"Error fetching data from API: {response.StatusCode} - {response.ReasonPhrase}");
            }
            return roadmapFeatures;
        }

        public async Task<ExternalItem> TransformToExternalItemAsync(RoadmapFeature roadmapFeature)
        {
            var externalItem = new ExternalItem
            {
                Id = roadmapFeature.id.ToString(),
                Properties = new Properties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "title", roadmapFeature.title ?? string.Empty },
                        { "description", roadmapFeature.description ?? string.Empty },
                        { "url", $"{Constants.RoadmapFeatureUrl}{roadmapFeature.id}" },
                        { "iconUr", $"{Constants.RoadmapFeatureUrl}{roadmapFeature.id}" },
                        { "created", roadmapFeature.created },
                        { "modified", roadmapFeature.modified },
                        { "moreInfoLink", roadmapFeature.moreInfoLink ?? $"{Constants.RoadmapFeatureUrl}{roadmapFeature.id}" }
                    }
                },
                Content = new()
                {
                    Value = roadmapFeature.description ?? "",
                    Type = ExternalItemContentType.Text
                },
                Acl = new()
                {
                    new()
                    {
                        Type = AclType.Everyone,
                        Value = "everyone",
                        AccessType = AccessType.Grant
                    }
                }
            };

            return externalItem;
        }
    }
}
