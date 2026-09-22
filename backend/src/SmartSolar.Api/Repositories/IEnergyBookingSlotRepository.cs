// ---------------------------------------------------------------------------
//  File        : IEnergyBookingSlotRepository.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Data-access contract for the "EnergyBookingSlots" collection.
// ---------------------------------------------------------------------------

using SmartSolar.Api.Models;

namespace SmartSolar.Api.Repositories;

public interface IEnergyBookingSlotRepository
{
    // Fetch one slot by its human-readable id ("SL-0042"). Null if absent.
    Task<EnergyBookingSlot?> GetBySlotIdAsync(string slotId);

    // All slots at a node, optionally bounded to [from, to) on StartTime.
    Task<List<EnergyBookingSlot>> GetByNodeIdAsync(string nodeId, DateTime? from, DateTime? to);

    // Active slots at the same node whose window overlaps [startTime, endTime),
    // excluding the slot identified by excludeSlotId (used when editing a slot).
    Task<List<EnergyBookingSlot>> GetOverlappingAsync(
        string nodeId, DateTime startTime, DateTime endTime, string? excludeSlotId);

    // Insert a new slot document.
    Task CreateAsync(EnergyBookingSlot slot);

    // Replace an existing slot document, matched on SlotId.
    Task UpdateAsync(EnergyBookingSlot slot);

    // Atomically decrements availableCapacity if availableCapacity > 0. Returns true on success.
    Task<bool> TryDecrementCapacityAsync(string slotId);

    // Atomically restores availableCapacity by 1 upon cancellation of an approved booking.
    Task IncrementCapacityAsync(string slotId);
}
