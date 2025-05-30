using Microsoft.Graph.Models.ExternalConnectors;
using GraphConnectorFunction.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using GraphConnectorFunction.Utilities;
using Microsoft.Graph.Models.ODataErrors;
using System.Net;
using GraphConnectorFunction.Interfaces;
using Azure.Identity;

namespace GraphConnectorFunction.Services
{
    public class GraphService : IGraphService
    {
        private readonly AzureFunctionSettings _configSettings;
        private readonly ILogger _logger;
        private static HttpClient _httpClient;
        private static GraphServiceClient _graphAppClient;

        public GraphService(ILoggerFactory loggerFactory, AzureFunctionSettings settings)
        {
            _logger = loggerFactory.CreateLogger<GraphService>();
            _httpClient = GraphClientFactory.Create();
            _configSettings = settings;
        }

        public GraphServiceClient GetAppGraphClient()
        {
            if (_graphAppClient == null)
            {
                var tenantId = _configSettings.TenantId;
                var clientId = _configSettings.ClientId;
                var clientSecret = _configSettings.ClientSecret;

                if (string.IsNullOrEmpty(tenantId) ||
                    string.IsNullOrEmpty(clientId) ||
                    string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("Required settings missing: 'TenantId', 'ClientId', and 'ClientSecret'.");
                    return null;
                }

                // using Azure.Identity;
                var options = new ClientSecretCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                };
                var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);

                _graphAppClient = new GraphServiceClient(clientSecretCredential);
            }

            return _graphAppClient;
        }

        public async Task<ExternalConnection> CreateConnectionAsync()
        {
            var graphClient = GetAppGraphClient();
            _ = graphClient ?? throw new MemberAccessException("graphClient is null");

            var connection = await _graphAppClient.External.Connections.PostAsync(ConnectionConfiguration.ExternalConnection);

            // Ensure the return value is not null
            return connection ?? throw new InvalidOperationException("Failed to create connection. The returned connection is null.");
        }

        public async Task UpdateConnectionAsync(string connectionId)
        {
            var graphClient = GetAppGraphClient();
            _ = graphClient ?? throw new MemberAccessException("graphClient is null");
            _ = connectionId ?? throw new ArgumentException("connectionId is required");
            await _graphAppClient.External.Connections[connectionId].PatchAsync(ConnectionConfiguration.ExternalConnection);
        }

        public async Task<ExternalConnection?> GetConnectionAsync(string connectionId)
        {
            var graphClient = GetAppGraphClient();
            _ = graphClient ?? throw new MemberAccessException("graphClient is null");

            try
            {
                var connection = await _graphAppClient.External.Connections[connectionId].GetAsync();
                return connection;
            }
            catch (ODataError odataError)
            {
                //Note:Commente this condition check to throw error "ItemNotFound. The requested resource does not exist."
                if (odataError.ResponseStatusCode == (int)HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Connection with ID {connectionId} was not found.");
                    return null;
                }

                _logger.LogError(odataError.Error?.Code);
                _logger.LogError(odataError.Error?.Message);
                throw;
            }
        }

        public async Task DeleteConnectionAsync(string connectionId)
        {
            try
            {
                var graphClient = GetAppGraphClient();
                _ = graphClient ?? throw new MemberAccessException("graphClient is null");
                _ = connectionId ?? throw new ArgumentException("connectionId is required");

                await _graphAppClient.External.Connections[connectionId].DeleteAsync();
            }
            catch (ODataError odataError)
            {
                _logger.LogError(odataError.Error?.Code);
                _logger.LogError(odataError.Error?.Message);
                throw;
            }
        }

        public async Task RegisterSchemaAsync(string connectionId, Schema schema)
        {
            var graphClient = GetAppGraphClient();
            _ = graphClient ?? throw new MemberAccessException("graphClient is null");
            _ = _httpClient ?? throw new MemberAccessException("httpClient is null");
            _ = connectionId ?? throw new ArgumentException("connectionId is required");
            // Use the Graph SDK's request builder to generate the request URL
            var requestInfo = _graphAppClient.External.Connections[connectionId].Schema.ToGetRequestInformation();

            requestInfo.SetContentFromParsable(_graphAppClient.RequestAdapter, "application/json", schema);

            // Convert the SDK request to an HttpRequestMessage
            var requestMessage = await _graphAppClient.RequestAdapter.ConvertToNativeRequestAsync<HttpRequestMessage>(requestInfo);
            _ = requestMessage ?? throw new Exception("Could not create native HTTP request");
            requestMessage.Method = HttpMethod.Post;
            requestMessage.Headers.Add("Prefer", "respond-async");

            // Send the request
            var responseMessage = await _httpClient.SendAsync(requestMessage) ?? throw new Exception("No response returned from API");

            if (responseMessage.IsSuccessStatusCode)
            {
                // The operation ID is contained in the Location header returned in the response
                var operationId = responseMessage.Headers.Location?.Segments.Last() ??
                    throw new Exception("Could not get operation ID from Location header");
                await WaitForOperationToCompleteAsync(connectionId, operationId);
            }
            else
            {
                throw new ServiceException("Registering schema failed", responseMessage.Headers, (int)responseMessage.StatusCode);
            }
        }

        public async Task<Schema> GetSchemaAsync(string connectionId)
        {
            var graphClient = GetAppGraphClient();
            _ = graphClient ?? throw new MemberAccessException("graphClient is null");
            _ = connectionId ?? throw new ArgumentException("connectionId is null");

            var schema = await _graphAppClient.External.Connections[connectionId].Schema.GetAsync();
            return schema ?? throw new InvalidOperationException("Failed to retrieve schema. The returned schema is null.");
        }

        public async Task CreateItemAsync(ExternalItem externalItem)
        {
            var graphClient = GetAppGraphClient();
            _ = graphClient ?? throw new MemberAccessException("graphClient is null");
            _ = externalItem ?? throw new ArgumentException("roadmapFeatures is null");

            // Retry mechanism with exponential backoff to handle throttling
            const int maxRetries = 5;
            int retryCount = 0;
            TimeSpan delay = TimeSpan.FromSeconds(5);

            while (true)
            {
                try
                {
                    await _graphAppClient.External.Connections[Uri.EscapeDataString(ConnectionConfiguration.ExternalConnection.Id!)]
                        .Items[externalItem.Id]
                        .PutAsync(externalItem);
                    break;
                }
                catch (ODataError odataError) when (odataError.ResponseStatusCode == (int)HttpStatusCode.NotFound)
                {
                    _logger.LogError(odataError.Error?.Code);
                    _logger.LogError(odataError.Error?.Message);

                    retryCount++;
                    if (retryCount > maxRetries)
                    {
                        throw new InvalidOperationException("Maximum retry attempts exceeded due to throttling.");
                    }

                    // Wait for the delay before retrying
                    await Task.Delay(delay);
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
                }
            }
        }


        private async Task WaitForOperationToCompleteAsync(string connectionId, string operationId)
        {
            var graphClient = GetAppGraphClient();
            _ = graphClient ?? throw new MemberAccessException("graphClient is null");

            do
            {
                var operation = await _graphAppClient.External.Connections[connectionId].Operations[operationId].GetAsync();

                if (operation?.Status == ConnectionOperationStatus.Completed)
                {
                    return;
                }
                else if (operation?.Status == ConnectionOperationStatus.Failed)
                {
                    throw new ServiceException($"Schema operation failed: {operation?.Error?.Code} {operation?.Error?.Message}");
                }

                // Wait 5 seconds and check again
                await Task.Delay(5000);
            } while (true);
        }

    }
}