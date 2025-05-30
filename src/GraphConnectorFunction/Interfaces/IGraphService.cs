using Microsoft.Graph.Models.ExternalConnectors;

namespace GraphConnectorFunction.Interfaces
{
    public interface IGraphService
    {
        Task<ExternalConnection> CreateConnectionAsync();
        Task UpdateConnectionAsync(string connectionId);
        Task<ExternalConnection> GetConnectionAsync(string connectionId);
        Task DeleteConnectionAsync(string connectionId);
        Task RegisterSchemaAsync(string connectionId, Schema schema);
        Task<Schema> GetSchemaAsync(string connectionId);
        Task CreateItemAsync(ExternalItem externalItem);
    }
}
