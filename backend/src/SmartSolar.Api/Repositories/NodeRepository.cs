using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public class NodeRepository : INodeRepository
{
    // TODO: hold IMongoCollection<Node> here, resolved from the injected database.

    /// <summary>
    /// Fetch one node by NodeId. Plain equality filter - the unique index on
    /// nodeId makes this an index hit. Do not use a case-insensitive regex
    /// (D1's UserRepository does, and it forces a collection scan).
    /// </summary>
    public Task<Node?> GetByNodeIdAsync(string nodeId)
        => throw new NotImplementedException();

    /// <summary>
    /// Page through nodes. Build an empty filter when active is null, otherwise
    /// filter on IsActive. Count first, then Find + Skip((page-1)*pageSize) +
    /// Limit(pageSize). Sort by CreatedAt so paging is stable.
    /// </summary>
    public Task<(List<Node> Items, int Total)> GetAllAsync(bool? active, int page, int pageSize)
        => throw new NotImplementedException();

    /// <summary>Insert. Let the unique index on nodeId be the final guard.</summary>
    public Task CreateAsync(Node node)
        => throw new NotImplementedException();

    /// <summary>Replace the whole document, matched on NodeId.</summary>
    public Task UpdateAsync(Node node)
        => throw new NotImplementedException();

    /// <summary>
    /// Existence check for D1's assignedNodeIds validation. Prefer a count or a
    /// projection over fetching the whole document.
    /// </summary>
    public Task<bool> ExistsAsync(string nodeId)
        => throw new NotImplementedException();

    /// <summary>Count of IsActive = true, for the Backoffice dashboard.</summary>
    public Task<int> CountActiveAsync()
        => throw new NotImplementedException();
}
