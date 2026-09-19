// ---------------------------------------------------------------------------
//  File        : ISolarStationRepository.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Data-access contract for the "SolarStationInfo" collection.
// ---------------------------------------------------------------------------

using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public interface ISolarStationRepository
{
    // Fetch one node by its human-readable id ("ND-0003"). Null if absent.
    Task<SolarStationInfo?> GetByNodeIdAsync(string nodeId);

    // Page through nodes. active=null returns every node.
    Task<(List<SolarStationInfo> Items, int Total)> GetAllAsync(bool? active, int page, int pageSize);

    // Insert a new node document.
    Task CreateAsync(SolarStationInfo node);

    // Replace an existing node document, matched on NodeId.
    Task UpdateAsync(SolarStationInfo node);

    // True if a node with this id exists.
    Task<bool> ExistsAsync(string nodeId);

    // Count of nodes with IsActive = true.
    Task<int> CountActiveAsync();
}
