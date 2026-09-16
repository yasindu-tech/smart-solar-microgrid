namespace SmartSolar.Api.Repositories;

// Minimal stub for M2's NodeRepository to allow D1 logic to compile and pass validation.
public class NodeRepository
{
    public Task<bool> ExistsAsync(string nodeId)
    {
        // Assume any node ID starting with "N" exists for testing, or just return true.
        return Task.FromResult(!string.IsNullOrWhiteSpace(nodeId));
    }

    public Task<int> CountActiveAsync()
    {
        return Task.FromResult(3);
    }
}
