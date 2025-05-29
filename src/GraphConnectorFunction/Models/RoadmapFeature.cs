using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphConnectorFunction.Models
{
    public class RoadmapFeature
    {
        public int id { get; set; }
        public string? title { get; set; }
        public string? description { get; set; }
        public string? moreInfoLink { get; set; }
        public string? publicDisclosureAvailabilityDate { get; set; }
        public string? publicPreviewDate { get; set; }
        public DateTime created { get; set; }
        public string? publicRoadmapStatus { get; set; }
        public string? status { get; set; }
        public DateTime modified { get; set; }
        public List<Tag>? tags { get; set; }
        public TagsContainer? tagsContainer { get; set; }
    }
    public class Tag
    {
        public string tagName { get; set; }
    }

    public class TagsContainer
    {
        public List<Product> products { get; set; }
        public List<CloudInstance> cloudInstances { get; set; }
        public List<ReleasePhase> releasePhase { get; set; }
        public List<Platform> platforms { get; set; }
    }
    public class CloudInstance
    {
        public string tagName { get; set; }
    }

    public class Platform
    {
        public string tagName { get; set; }
    }

    public class Product
    {
        public string tagName { get; set; }
    }

    public class ReleasePhase
    {
        public string tagName { get; set; }
    }
}
