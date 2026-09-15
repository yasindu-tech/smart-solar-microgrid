// ---------------------------------------------------------------------------
//  File        : ReservationStateMachine.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Services
//  Description : The only place a reservation status change is authorised.
//                Mirrors the transition table in
//                docs/architecture/state-machines.md section 1.
//  Author      : <member name - M3>
//  Created     : 2026-09-13
// ---------------------------------------------------------------------------

using SmartSolar.Api.Middleware;

namespace SmartSolar.Api.Services;

public static class ReservationStateMachine
{
    public const string Pending   = "Pending";
    public const string Approved  = "Approved";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
    public const string Expired   = "Expired";

    // Allowed target statuses for each current status. Anything absent is refused.
    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        [Pending]   = new[] { Pending, Approved, Cancelled, Expired },
        [Approved]  = new[] { Approved, Cancelled, Completed, Expired },
        [Cancelled] = Array.Empty<string>(),
        [Completed] = Array.Empty<string>(),
        [Expired]   = Array.Empty<string>()
    };

    // Returns true when the domain permits moving from one status to another.
    public static bool CanTransition(string from, string to)
    {
        return Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
    }

    // Guard called before every status write. Throws E4105 on an illegal move.
    public static void AssertTransition(string from, string to)
    {
        if (!CanTransition(from, to))
        {
            throw new DomainException(
                "E4105",
                $"A reservation cannot move from {from} to {to}.",
                409,
                "status");
        }
    }
}
