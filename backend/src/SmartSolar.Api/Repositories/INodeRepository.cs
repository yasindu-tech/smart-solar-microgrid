using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public interface INodeRepository
{
    Task<Node?> GetByNodeIdAsync(string nodeId);

    Task<(List<Node> Items, int Total)> GetAllAsync(bool? active, int page, int pageSize);

    Task CreateAsync(Node node);

    Task UpdateAsync(Node node);

    Task<bool> ExistsAsync(string nodeId);

    Task<int> CountActiveAsync();
}
