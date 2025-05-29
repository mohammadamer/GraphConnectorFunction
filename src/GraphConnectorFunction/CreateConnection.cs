using GraphConnectorFunction.Interfaces;
using GraphConnectorFunction.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GraphConnectorFunction;
public class CreateConnectionOutput
{
    [QueueOutput("create-schema")]
    public required IList<string> Schemas { get; set; }
}
public class CreateConnection
{
    private readonly ILogger<CreateConnection> _logger;
    private readonly IGraphService _graphService;

    public CreateConnection(ILogger<CreateConnection> logger, IGraphService graphService)
    {
        _logger = logger;
        _graphService = graphService;
    }

    [Function(nameof(CreateConnection))]
    public async Task<CreateConnectionOutput> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        var schemas = new List<string>();
        schemas.Add(ConnectionConfiguration.ConnectionID);

        _logger.LogInformation("CreateConnection function HTTP trigger function processed a request.");
        _logger.LogInformation("Creating connection....");

        var graphConnection = await _graphService.GetConnectionAsync(ConnectionConfiguration.ConnectionID);
        if (graphConnection != null)
        {
            return new CreateConnectionOutput
            {
                Schemas = schemas
            };
        }

        await _graphService.CreateConnectionAsync();
        _logger.LogInformation("Created");

        return new CreateConnectionOutput
        {
            Schemas = schemas
        };
    }
}