// ---------------------------------------------------------------------------
//  File        : EnergyBookingSlotService.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Slot CRUD and REQ-SLOT validation (time ordering, schedule
//                window containment, overlap, operator ownership). All
//                business logic for booking slots lives here - SlotsController
//                only binds, authorises and maps.
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Request.Slots;
using SmartSolar.Api.DTOs.Response.Slots;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class EnergyBookingSlotService
{
    // .NET's DayOfWeek enum is 0-indexed from Sunday - this mirrors that order
    // so (int)dateTime.DayOfWeek indexes straight into it.
    private static readonly string[] DayAbbreviations = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

    private readonly IEnergyBookingSlotRepository _slotRepo;
    private readonly ISolarStationRepository _nodeRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICounterRepository _counterRepo;

    public EnergyBookingSlotService(
        IEnergyBookingSlotRepository slotRepo,
        ISolarStationRepository nodeRepo,
        IUserRepository userRepo,
        ICounterRepository counterRepo)
    {
        // Inject the data-access contracts. No IMongoCollection ever reaches this class.
        _slotRepo = slotRepo;
        _nodeRepo = nodeRepo;
        _userRepo = userRepo;
        _counterRepo = counterRepo;
    }

    public async Task<SlotResponse> CreateAsync(
        string nodeId, CreateSlotRequest request, string callerUserId, string callerRole)
    {
        // Load the node first - every other check depends on its schedule and active state.
        var node = await _nodeRepo.GetByNodeIdAsync(nodeId)
            ?? throw new DomainException("E4404", $"Node '{nodeId}' was not found.", 404);

        await AssertOperatorOwnsNodeAsync(callerUserId, callerRole, node.NodeId);

        if (!node.IsActive)
            throw new DomainException("E4002", $"Node '{nodeId}' is not active.", 400, "nodeId");

        await ValidateWindowAsync(node, request.StartTime, request.EndTime, request.TotalCapacity, excludeSlotId: null);

        var sequence = await _counterRepo.GetNextSequenceValueAsync("slotid");
        var now = DateTime.UtcNow;

        var slot = new EnergyBookingSlot
        {
            SlotId = $"SL-{sequence:D4}",
            NodeId = node.NodeId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TotalCapacity = request.TotalCapacity,
            AvailableCapacity = request.TotalCapacity,
            EnergyOfferedKwh = request.EnergyOfferedKwh,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _slotRepo.CreateAsync(slot);
        return MapToResponse(slot);
    }

    public async Task<List<SlotResponse>> GetByNodeIdAsync(
        string nodeId, DateTime? from, DateTime? to, string callerRole)
    {
        // Unknown node is a 404 - callers always hold a concrete nodeId.
        _ = await _nodeRepo.GetByNodeIdAsync(nodeId)
            ?? throw new DomainException("E4404", $"Node '{nodeId}' was not found.", 404);

        var slots = await _slotRepo.GetByNodeIdAsync(nodeId, from, to);

        // AC-9: a prosumer only ever sees future, active, bookable slots.
        // Backoffice/Operator manage the full set, including full or past ones.
        if (callerRole == "Prosumer")
        {
            var now = DateTime.UtcNow;
            slots = slots.Where(s => s.IsActive && s.StartTime > now && s.AvailableCapacity > 0).ToList();
        }

        return slots.Select(MapToResponse).ToList();
    }

    public async Task<SlotResponse> UpdateAsync(
        string slotId, UpdateSlotRequest request, string callerUserId, string callerRole)
    {
        // Load first so an unknown id fails fast, before any validation work.
        var slot = await _slotRepo.GetBySlotIdAsync(slotId)
            ?? throw new DomainException("E4404", $"Slot '{slotId}' was not found.", 404);

        var node = await _nodeRepo.GetByNodeIdAsync(slot.NodeId)
            ?? throw new DomainException("E4404", $"Node '{slot.NodeId}' was not found.", 404);

        await AssertOperatorOwnsNodeAsync(callerUserId, callerRole, node.NodeId);

        // Re-run every CreateAsync validation - an update can put a slot into the same invalid state a create can.
        await ValidateWindowAsync(node, request.StartTime, request.EndTime, request.TotalCapacity, excludeSlotId: slot.SlotId);

        // BR-19 (E4003) would refuse a totalCapacity cut below already-approved reservations,
        // and REQ-SLOT 4.3 would propagate a startTime change onto every referencing
        // reservation's slotStartTime. Neither is checkable yet - EnergyReservation
        // (REQ-RESV) does not exist in this codebase. Until it does, available capacity
        // is simply clamped so it can never exceed the new total.
        slot.StartTime = request.StartTime;
        slot.EndTime = request.EndTime;
        slot.TotalCapacity = request.TotalCapacity;
        slot.AvailableCapacity = Math.Min(slot.AvailableCapacity, request.TotalCapacity);
        slot.EnergyOfferedKwh = request.EnergyOfferedKwh;
        slot.UpdatedAt = DateTime.UtcNow;

        await _slotRepo.UpdateAsync(slot);
        return MapToResponse(slot);
    }

    public async Task<SlotResponse> UpdateAvailabilityAsync(
        string slotId, UpdateSlotAvailabilityRequest request, string callerUserId, string callerRole)
    {
        var slot = await _slotRepo.GetBySlotIdAsync(slotId)
            ?? throw new DomainException("E4404", $"Slot '{slotId}' was not found.", 404);

        await AssertOperatorOwnsNodeAsync(callerUserId, callerRole, slot.NodeId);

        // EC-6: available capacity can never exceed the slot's total - that is the one
        // consistency check possible without EnergyReservation to derive the real value from.
        if (request.AvailableCapacity < 0 || request.AvailableCapacity > slot.TotalCapacity)
            throw new DomainException(
                "E4002", $"Available capacity must be between 0 and {slot.TotalCapacity}.", 400, "availableCapacity");

        slot.AvailableCapacity = request.AvailableCapacity;
        slot.UpdatedAt = DateTime.UtcNow;

        await _slotRepo.UpdateAsync(slot);
        return MapToResponse(slot);
    }

    public async Task DeleteAsync(string slotId)
    {
        var slot = await _slotRepo.GetBySlotIdAsync(slotId)
            ?? throw new DomainException("E4404", $"Slot '{slotId}' was not found.", 404);

        // AC-8/E4004 would refuse this while a reservation still references the slot.
        // That check needs EnergyReservation (REQ-RESV), which does not exist yet -
        // deleting is unconditional for now. Soft-deleted, like a node, so history reads cleanly.
        slot.IsActive = false;
        slot.UpdatedAt = DateTime.UtcNow;

        await _slotRepo.UpdateAsync(slot);
    }

    // EC-4: an Operator may only touch slots on a node in their own assignedNodeIds.
    // Backoffice is exempt - it manages every node.
    private async Task AssertOperatorOwnsNodeAsync(string callerUserId, string callerRole, string nodeId)
    {
        if (callerRole != "Operator")
            return;

        var caller = await _userRepo.GetByUserIdAsync(callerUserId);
        if (caller == null || !caller.AssignedNodeIds.Contains(nodeId))
            throw new DomainException("E4303", "You are not assigned to this node.", 403);
    }

    // Shared by create and update: time ordering, the 7-day-window schedule containment,
    // and the no-overlap-with-another-slot check (AC-2, AC-7, AC-10, BR-08).
    private async Task ValidateWindowAsync(
        SolarStationInfo node, DateTime startTime, DateTime endTime, int totalCapacity, string? excludeSlotId)
    {
        if (totalCapacity < 1)
            throw new DomainException("E4002", "Total capacity must be at least 1.", 400, "totalCapacity");

        if (startTime >= endTime)
            throw new DomainException("E4002", "Slot start time must be strictly before its end time.", 400, "startTime");

        // BR-08: a slot cannot start in the past.
        if (startTime <= DateTime.UtcNow)
            throw new DomainException("E4104", "Slot start time must be in the future.", 400, "startTime");

        // AC-7: the window must fall inside one of the node's opening-hour windows for that day.
        // ScheduleWindow.OpenTime/CloseTime never cross midnight (SolarStationService enforces
        // open < close on the same day), so neither can a slot that fits inside one.
        var dayAbbreviation = DayAbbreviations[(int)startTime.DayOfWeek];
        var startOfDay = startTime.Date;
        var fitsSchedule = node.Schedule
            .Where(w => w.DayOfWeek == dayAbbreviation)
            .Any(w =>
            {
                var open = startOfDay + TimeOnly.Parse(w.OpenTime).ToTimeSpan();
                var close = startOfDay + TimeOnly.Parse(w.CloseTime).ToTimeSpan();
                return startTime >= open && endTime <= close;
            });

        if (!fitsSchedule)
            throw new DomainException(
                "E4002", $"Slot falls outside {node.NodeId}'s opening hours for {dayAbbreviation}.", 400, "startTime");

        // AC-2: no two active slots at the same node may overlap.
        var overlapping = await _slotRepo.GetOverlappingAsync(node.NodeId, startTime, endTime, excludeSlotId);
        if (overlapping.Count > 0)
            throw new DomainException(
                "E4002", $"Overlaps with existing slot '{overlapping[0].SlotId}'.", 400, "startTime");
    }

    private static SlotResponse MapToResponse(EnergyBookingSlot slot)
        // Convert the storage model to its DTO counterpart, never leak the Mongo document.
        => new(
            slot.SlotId,
            slot.NodeId,
            slot.StartTime,
            slot.EndTime,
            slot.TotalCapacity,
            slot.AvailableCapacity,
            slot.EnergyOfferedKwh,
            slot.IsActive,
            slot.CreatedAt,
            slot.UpdatedAt);
}
