// ---------------------------------------------------------------------------
//  File        : EnergyBookingSlotServiceTests.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Executable specification for EnergyBookingSlotService -
//                REQ-SLOT's window/overlap/ownership validation and CRUD
//                behaviour, to the extent testable without EnergyReservation.
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Request.Slots;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;
using SmartSolar.Api.Services;
using Xunit;

namespace SmartSolar.Api.Tests;

// ---------------------------------------------------------------------------
//  Fakes
// ---------------------------------------------------------------------------

/// <summary>In-memory stand-in for the Mongo-backed slot repository.</summary>
internal sealed class FakeEnergyBookingSlotRepository : IEnergyBookingSlotRepository
{
    public readonly List<EnergyBookingSlot> Slots = new();

    public Task<EnergyBookingSlot?> GetBySlotIdAsync(string slotId)
        => Task.FromResult(Slots.FirstOrDefault(s => s.SlotId == slotId));

    public Task<List<EnergyBookingSlot>> GetByNodeIdAsync(string nodeId, DateTime? from, DateTime? to)
    {
        var matching = Slots.Where(s => s.NodeId == nodeId);
        if (from.HasValue) matching = matching.Where(s => s.StartTime >= from.Value);
        if (to.HasValue) matching = matching.Where(s => s.StartTime < to.Value);
        return Task.FromResult(matching.OrderBy(s => s.StartTime).ToList());
    }

    public Task<List<EnergyBookingSlot>> GetOverlappingAsync(
        string nodeId, DateTime startTime, DateTime endTime, string? excludeSlotId)
    {
        var overlapping = Slots.Where(s =>
            s.NodeId == nodeId &&
            s.IsActive &&
            s.StartTime < endTime &&
            s.EndTime > startTime &&
            (excludeSlotId == null || s.SlotId != excludeSlotId));

        return Task.FromResult(overlapping.ToList());
    }

    public Task CreateAsync(EnergyBookingSlot slot)
    {
        Slots.Add(slot);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EnergyBookingSlot slot)
    {
        var index = Slots.FindIndex(s => s.SlotId == slot.SlotId);
        if (index >= 0) Slots[index] = slot;
        return Task.CompletedTask;
    }
}

/// <summary>In-memory stand-in for the Mongo-backed user repository (ownership checks only).</summary>
internal sealed class FakeUserRepository : IUserRepository
{
    public readonly List<User> Users = new();

    public Task<User?> GetByEmailAsync(string email)
        => Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

    public Task<User?> GetByUserIdAsync(string userId)
        => Task.FromResult(Users.FirstOrDefault(u => u.UserId == userId));

    public Task<(List<User> Items, int Total)> GetAllAsync(string? role, int page, int pageSize)
        => Task.FromResult((Users.ToList(), Users.Count));

    public Task CreateAsync(User user)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user) => Task.CompletedTask;

    public Task<int> CountActiveBackofficeAsync() => Task.FromResult(Users.Count(u => u.Role == "Backoffice" && u.IsActive));

    public Task<bool> EmailExistsAsync(string email, string? excludeUserId = null)
        => Task.FromResult(Users.Any(u => u.Email == email));
}

// ---------------------------------------------------------------------------
//  Tests
// ---------------------------------------------------------------------------

public class EnergyBookingSlotServiceTests
{
    private readonly FakeEnergyBookingSlotRepository _slots = new();
    private readonly FakeSolarStationRepository _nodes = new();
    private readonly FakeUserRepository _users = new();
    private readonly EnergyBookingSlotService _service;

    private const string BackofficeCaller = "USR-0001";
    private const string OperatorCaller = "USR-0002";
    private const string NodeId = "ND-0001";

    public EnergyBookingSlotServiceTests()
    {
        _service = new EnergyBookingSlotService(_slots, _nodes, _users, new FakeCounterRepository());

        // An active node open Monday 06:00-18:00, the shared fixture every test builds on.
        _nodes.Nodes.Add(new SolarStationInfo
        {
            NodeId = NodeId,
            Name = "Moratuwa Microgrid Hub A",
            IsActive = true,
            Schedule = new List<ScheduleWindow>
            {
                new() { DayOfWeek = "Mon", OpenTime = "06:00", CloseTime = "18:00" },
            },
        });

        _users.Users.Add(new User { UserId = OperatorCaller, Role = "Operator", AssignedNodeIds = new List<string> { NodeId } });
    }

    // Next Monday, so every test has a stable in-schedule, in-the-future day regardless of when it runs.
    private static DateTime NextMonday(TimeSpan timeOfDay)
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        var monday = today.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
        return monday + timeOfDay;
    }

    private static CreateSlotRequest ValidCreate(int totalCapacity = 4, double energyOfferedKwh = 20.0)
        => new(NextMonday(TimeSpan.FromHours(9)), NextMonday(TimeSpan.FromHours(11)), totalCapacity, energyOfferedKwh);

    private static UpdateSlotRequest ValidUpdate(int totalCapacity = 4, double energyOfferedKwh = 20.0)
        => new(NextMonday(TimeSpan.FromHours(9)), NextMonday(TimeSpan.FromHours(11)), totalCapacity, energyOfferedKwh);

    // -- Create: happy path ---------------------------------------------------

    [Fact] // AC-1
    public async Task CreateAsync_WithValidRequest_StoresTheSlotWithAvailableEqualToTotal()
    {
        var result = await _service.CreateAsync(NodeId, ValidCreate(totalCapacity: 4), BackofficeCaller, "Backoffice");

        Assert.Equal(4, result.AvailableCapacity);
        Assert.True(result.IsActive);
        Assert.Single(_slots.Slots);
    }

    [Fact] // data-model.md 1.4 - slotId is "SL-0001" style and sequential
    public async Task CreateAsync_GeneratesSequentialPaddedSlotIds()
    {
        var first = await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");
        var second = await _service.CreateAsync(
            NodeId, ValidCreate() with { StartTime = NextMonday(TimeSpan.FromHours(13)), EndTime = NextMonday(TimeSpan.FromHours(15)) },
            BackofficeCaller, "Backoffice");

        Assert.Equal("SL-0001", first.SlotId);
        Assert.Equal("SL-0002", second.SlotId);
    }

    [Fact] // unknown node is a 404, not a silently-created orphan slot
    public async Task CreateAsync_UnknownNode_IsRejectedWithNotFound()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync("ND-9999", ValidCreate(), BackofficeCaller, "Backoffice"));

        Assert.Equal("E4404", ex.Code);
    }

    [Fact] // REQ-SLOT preconditions - the parent node must be Active
    public async Task CreateAsync_InactiveNode_IsRejected()
    {
        _nodes.Nodes.Single(n => n.NodeId == NodeId).IsActive = false;

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice"));

        Assert.Equal("E4002", ex.Code);
    }

    // -- Create: time ordering and horizon -------------------------------------

    [Fact] // AC-10 / BR-08 - a slot cannot start in the past
    public async Task CreateAsync_StartTimeInThePast_IsRejectedWithE4104()
    {
        var pastRequest = new CreateSlotRequest(
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1), 4, 20.0);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(NodeId, pastRequest, BackofficeCaller, "Backoffice"));

        Assert.Equal("E4104", ex.Code);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact] // start must be strictly before end
    public async Task CreateAsync_StartNotBeforeEnd_IsRejected()
    {
        var start = NextMonday(TimeSpan.FromHours(10));
        var request = new CreateSlotRequest(start, start, 4, 20.0);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(NodeId, request, BackofficeCaller, "Backoffice"));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("startTime", ex.Field);
    }

    [Fact] // total capacity must be at least 1
    public async Task CreateAsync_TotalCapacityBelowOne_IsRejected()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(NodeId, ValidCreate(totalCapacity: 0), BackofficeCaller, "Backoffice"));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("totalCapacity", ex.Field);
    }

    // -- Create: schedule containment -------------------------------------------

    [Fact] // AC-7 - outside the node's opening hours for that day
    public async Task CreateAsync_OutsideNodeSchedule_IsRejected()
    {
        // The fixture node only opens Monday 06:00-18:00 - 19:00 is after close.
        var request = new CreateSlotRequest(
            NextMonday(TimeSpan.FromHours(19)), NextMonday(TimeSpan.FromHours(20)), 4, 20.0);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(NodeId, request, BackofficeCaller, "Backoffice"));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("startTime", ex.Field);
    }

    [Fact] // a day the node has no window for at all
    public async Task CreateAsync_OnADayWithNoScheduleWindow_IsRejected()
    {
        var tuesday = NextMonday(TimeSpan.FromHours(9)).AddDays(1);
        var request = new CreateSlotRequest(tuesday, tuesday.AddHours(2), 4, 20.0);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(NodeId, request, BackofficeCaller, "Backoffice"));

        Assert.Equal("E4002", ex.Code);
    }

    [Fact] // touching the window's boundary exactly is inside it, not outside
    public async Task CreateAsync_ExactlyAtScheduleBoundary_IsAccepted()
    {
        var request = new CreateSlotRequest(
            NextMonday(TimeSpan.FromHours(6)), NextMonday(TimeSpan.FromHours(18)), 4, 20.0);

        var result = await _service.CreateAsync(NodeId, request, BackofficeCaller, "Backoffice");

        Assert.Equal(NodeId, result.NodeId);
    }

    // -- Create: overlap ----------------------------------------------------------

    [Fact] // AC-2 - refused, and the message names the overlapping slot
    public async Task CreateAsync_OverlappingAnExistingSlot_IsRejected()
    {
        var existing = await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");

        var overlapping = new CreateSlotRequest(
            NextMonday(TimeSpan.FromHours(10)), NextMonday(TimeSpan.FromHours(12)), 2, 10.0);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(NodeId, overlapping, BackofficeCaller, "Backoffice"));

        Assert.Equal("E4002", ex.Code);
        Assert.Contains(existing.SlotId, ex.Message);
    }

    [Fact] // touching at the boundary is not overlapping - back-to-back slots are legal
    public async Task CreateAsync_AdjacentSlot_IsAccepted()
    {
        await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");

        var adjacent = new CreateSlotRequest(
            NextMonday(TimeSpan.FromHours(11)), NextMonday(TimeSpan.FromHours(13)), 2, 10.0);

        var result = await _service.CreateAsync(NodeId, adjacent, BackofficeCaller, "Backoffice");

        Assert.Equal(2, _slots.Slots.Count);
        Assert.Equal(2, result.TotalCapacity);
    }

    // -- Create: operator ownership (EC-4) -----------------------------------------

    [Fact] // an operator assigned to the node may create a slot on it
    public async Task CreateAsync_OperatorAssignedToNode_IsAccepted()
    {
        var result = await _service.CreateAsync(NodeId, ValidCreate(), OperatorCaller, "Operator");

        Assert.Equal(NodeId, result.NodeId);
    }

    [Fact] // EC-4 - an operator not assigned to the node is refused with E4303
    public async Task CreateAsync_OperatorNotAssignedToNode_IsRejectedWithE4303()
    {
        _users.Users.Single(u => u.UserId == OperatorCaller).AssignedNodeIds.Clear();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(NodeId, ValidCreate(), OperatorCaller, "Operator"));

        Assert.Equal("E4303", ex.Code);
        Assert.Equal(403, ex.StatusCode);
    }

    // -- Get by node (AC-9) ----------------------------------------------------------

    [Fact] // Backoffice/Operator see everything, including full or past slots
    public async Task GetByNodeIdAsync_ForStaff_ReturnsEverySlot()
    {
        await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");
        var slot = _slots.Slots[0];
        slot.AvailableCapacity = 0;

        var results = await _service.GetByNodeIdAsync(NodeId, from: null, to: null, callerRole: "Backoffice");

        Assert.Single(results);
    }

    [Fact] // AC-9 - a prosumer only ever sees future, active, bookable slots
    public async Task GetByNodeIdAsync_ForProsumer_HidesFullSlots()
    {
        await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");
        _slots.Slots[0].AvailableCapacity = 0;

        var results = await _service.GetByNodeIdAsync(NodeId, from: null, to: null, callerRole: "Prosumer");

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByNodeIdAsync_UnknownNode_IsRejectedWithNotFound()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.GetByNodeIdAsync("ND-9999", null, null, "Backoffice"));

        Assert.Equal("E4404", ex.Code);
    }

    // -- Update -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_UnknownSlot_IsRejectedWithNotFound()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.UpdateAsync("SL-9999", ValidUpdate(), BackofficeCaller, "Backoffice"));

        Assert.Equal("E4404", ex.Code);
    }

    [Fact] // an update can move a slot's time forward without needing a new id
    public async Task UpdateAsync_ChangesTheWindow()
    {
        var created = await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");
        var request = new UpdateSlotRequest(
            NextMonday(TimeSpan.FromHours(14)), NextMonday(TimeSpan.FromHours(16)), 4, 20.0);

        var updated = await _service.UpdateAsync(created.SlotId, request, BackofficeCaller, "Backoffice");

        Assert.Equal(NextMonday(TimeSpan.FromHours(14)), updated.StartTime);
    }

    [Fact] // an update can put a slot into the same invalid state a create can
    public async Task UpdateAsync_RevalidatesTheSchedule()
    {
        var created = await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");
        var request = new UpdateSlotRequest(
            NextMonday(TimeSpan.FromHours(19)), NextMonday(TimeSpan.FromHours(20)), 4, 20.0);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.UpdateAsync(created.SlotId, request, BackofficeCaller, "Backoffice"));

        Assert.Equal("E4002", ex.Code);
    }

    [Fact] // a slot does not overlap itself when its own window is unchanged
    public async Task UpdateAsync_WithoutChangingTheWindow_DoesNotConflictWithItself()
    {
        var created = await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");
        var request = new UpdateSlotRequest(created.StartTime, created.EndTime, 6, 30.0);

        var updated = await _service.UpdateAsync(created.SlotId, request, BackofficeCaller, "Backoffice");

        Assert.Equal(6, updated.TotalCapacity);
    }

    [Fact] // EC-4 applies to updates too
    public async Task UpdateAsync_OperatorNotAssignedToNode_IsRejectedWithE4303()
    {
        var created = await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");
        _users.Users.Single(u => u.UserId == OperatorCaller).AssignedNodeIds.Clear();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.UpdateAsync(created.SlotId, ValidUpdate(), OperatorCaller, "Operator"));

        Assert.Equal("E4303", ex.Code);
    }

    [Fact] // reducing totalCapacity clamps availableCapacity so it can never exceed it
    public async Task UpdateAsync_ReducingTotalCapacity_ClampsAvailableCapacity()
    {
        var created = await _service.CreateAsync(NodeId, ValidCreate(totalCapacity: 4), BackofficeCaller, "Backoffice");
        var request = new UpdateSlotRequest(created.StartTime, created.EndTime, 2, created.EnergyOfferedKwh);

        var updated = await _service.UpdateAsync(created.SlotId, request, BackofficeCaller, "Backoffice");

        Assert.Equal(2, updated.AvailableCapacity);
    }

    // -- Update availability (EC-6) ----------------------------------------------------

    [Fact]
    public async Task UpdateAvailabilityAsync_WithinBounds_IsApplied()
    {
        var created = await _service.CreateAsync(NodeId, ValidCreate(totalCapacity: 4), BackofficeCaller, "Backoffice");

        var updated = await _service.UpdateAvailabilityAsync(
            created.SlotId, new UpdateSlotAvailabilityRequest(1), BackofficeCaller, "Backoffice");

        Assert.Equal(1, updated.AvailableCapacity);
    }

    [Theory] // EC-6 - available can never exceed total, nor go negative
    [InlineData(-1)]
    [InlineData(5)]
    public async Task UpdateAvailabilityAsync_OutOfBounds_IsRejected(int availableCapacity)
    {
        var created = await _service.CreateAsync(NodeId, ValidCreate(totalCapacity: 4), BackofficeCaller, "Backoffice");

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.UpdateAvailabilityAsync(
            created.SlotId, new UpdateSlotAvailabilityRequest(availableCapacity), BackofficeCaller, "Backoffice"));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("availableCapacity", ex.Field);
    }

    [Fact] // EC-4 applies to the availability endpoint too
    public async Task UpdateAvailabilityAsync_OperatorNotAssignedToNode_IsRejectedWithE4303()
    {
        var created = await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");
        _users.Users.Single(u => u.UserId == OperatorCaller).AssignedNodeIds.Clear();

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.UpdateAvailabilityAsync(
            created.SlotId, new UpdateSlotAvailabilityRequest(1), OperatorCaller, "Operator"));

        Assert.Equal("E4303", ex.Code);
    }

    // -- Delete --------------------------------------------------------------------

    [Fact] // deletion is a soft-delete, matching the node pattern
    public async Task DeleteAsync_MarksTheSlotInactive()
    {
        var created = await _service.CreateAsync(NodeId, ValidCreate(), BackofficeCaller, "Backoffice");

        await _service.DeleteAsync(created.SlotId);

        Assert.False(_slots.Slots.Single(s => s.SlotId == created.SlotId).IsActive);
    }

    [Fact]
    public async Task DeleteAsync_UnknownSlot_IsRejectedWithNotFound()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.DeleteAsync("SL-9999"));

        Assert.Equal("E4404", ex.Code);
    }
}
