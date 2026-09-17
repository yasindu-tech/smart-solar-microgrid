using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public interface INodeRepository
{
    /// <summary>Fetch one node by its human-readable id ("ND-0003"). Null if absent.</summary>
    Task<Node?> GetByNodeIdAsync(string nodeId);

    /// <summary>
    /// Page through nodes. active=null returns every node; active=true or false
    /// filters on IsActive. Returns the page plus the total matching count.
    /// </summary>
    Task<(List<Node> Items, int Total)> GetAllAsync(bool? active, int page, int pageSize);

    /// <summary>Insert a new node document.</summary>
    Task CreateAsync(Node node);

    /// <summary>Replace an existing node document, matched on NodeId.</summary>
    Task UpdateAsync(Node node);

    /// <summary>True if a node with this id exists. Used by D1 to validate assignedNodeIds.</summary>
    Task<bool> ExistsAsync(string nodeId);

    /// <summary>Count of nodes with IsActive = true. Used by the Backoffice dashboard.</summary>
    Task<int> CountActiveAsync();
}
