using GraphConnectorFunction.Interfaces;
using GraphConnectorFunction.Services;
using GraphConnectorFunction.Utilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ExternalConnectors;

namespace GraphConnectorFunction.Functions
{
    public class CreateSchemaOutput
    {
        [QueueOutput("load-content")]
        public required IList<string> RoadmapContent { get; set; }
    }
    public class CreateSchema
    {
        private readonly ILogger<CreateSchema> _logger;
        private readonly IGraphService _graphService;
        public CreateSchema(ILogger<CreateSchema> logger, IGraphService graphService)
        {
            _logger = logger;
            _graphService = graphService;
        }

        [Function("CreateSchema")]
        public async Task<CreateSchemaOutput> Run([QueueTrigger("create-schema", Connection = "AzureWebJobsStorage")] string SchemaMessage)
        {
            _logger.LogInformation($"CreateSchema HTTP trigger function executed at: {DateTime.Now}");

            var roadmapContent = new List<string>();
            roadmapContent.Add(ConnectionConfiguration.ConnectionID);

            var graphConnection = await _graphService.GetConnectionAsync(ConnectionConfiguration.ConnectionID);

            //Note:Commented this condition check to throw error "The requested resource does not exist."
            if (graphConnection?.State != ConnectionState.Draft)
            {
                //Check if schema exists, if not create it
                //Note: You will get an error if you comment out the previous condition. The error "The requested resource does not exist."
                var schema = await _graphService.GetSchemaAsync(ConnectionConfiguration.ConnectionID);
                if (schema != null)
                {
                    return new CreateSchemaOutput
                    {
                        RoadmapContent = roadmapContent
                    };
                }
            }

            _logger.LogInformation("Creating schema...");
            await _graphService.RegisterSchemaAsync(ConnectionConfiguration.ConnectionID, ConnectionConfiguration.Schema);
            _logger.LogInformation("Schema created");

            return new CreateSchemaOutput
            {
                RoadmapContent = roadmapContent
            };
        }
    }
}
