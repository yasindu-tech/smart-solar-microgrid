// ---------------------------------------------------------------------------
//  NodeLocationDto.cs - wire shape for a node's GPS position and address.
//  REQ-NODE - Microgrid Node Management
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.DTOs.Common.Nodes;

/// <summary>GPS position and address, as the clients send and receive it.</summary>
public record NodeLocationDto(
    double Latitude,
    double Longitude,
    string AddressLine);
