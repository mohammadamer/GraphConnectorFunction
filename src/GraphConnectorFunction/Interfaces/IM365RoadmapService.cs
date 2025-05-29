using GraphConnectorFunction.Models;
using Microsoft.Graph.Models.ExternalConnectors;

namespace GraphConnectorFunction.Interfaces
{
    public interface IM365RoadmapService
    {
        Task<List<RoadmapFeature>> ExtractRoadmapFeaturesAsync();
        Task<ExternalItem> TransformToExternalItemAsync(RoadmapFeature roadmapFeature);
    }
}
