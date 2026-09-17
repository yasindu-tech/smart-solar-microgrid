using SmartSolar.Api.DTOs.Nodes;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;
using SmartSolar.Api.Services;
using Xunit;

namespace SmartSolar.Api.Tests;

// ---------------------------------------------------------------------------
//  Fakes
// ---------------------------------------------------------------------------

/// <summary>In-memory stand-in for the Mongo-backed node repository.</summary>
internal sealed class FakeNodeRepository : INodeRepository
{
    public readonly List<Node> Nodes = new();

    // Recorded so tests can assert what the service passed down.
    public bool? LastActiveFilter;
    public int LastPage;
    public int LastPageSize;

    public Task<Node?> GetByNodeIdAsync(string nodeId)
        => Task.FromResult(Nodes.FirstOrDefault(n => n.NodeId == nodeId));

    public Task<(List<Node> Items, int Total)> GetAllAsync(bool? active, int page, int pageSize)
    {
        LastActiveFilter = active;
        LastPage = page;
        LastPageSize = pageSize;

        var matching = Nodes.Where(n => active == null || n.IsActive == active).ToList();
        var pageItems = matching.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((pageItems, matching.Count));
    }

    public Task CreateAsync(Node node)
    {
        Nodes.Add(node);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Node node)
    {
        var index = Nodes.FindIndex(n => n.NodeId == node.NodeId);
        if (index >= 0) Nodes[index] = node;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string nodeId)
        => Task.FromResult(Nodes.Any(n => n.NodeId == nodeId));

    public Task<int> CountActiveAsync()
        => Task.FromResult(Nodes.Count(n => n.IsActive));
}

/// <summary>In-memory sequence generator.</summary>
internal sealed class FakeCounterRepository : ICounterRepository
{
    private readonly Dictionary<string, int> _sequences = new();

    public Task<int> GetNextSequenceValueAsync(string sequenceName)
    {
        _sequences.TryGetValue(sequenceName, out var current);
        current++;
        _sequences[sequenceName] = current;
        return Task.FromResult(current);
    }
}

// ---------------------------------------------------------------------------
//  Tests
// ---------------------------------------------------------------------------

public class NodeServiceTests
{
    private readonly FakeNodeRepository _nodes = new();
    private readonly NodeService _service;

    private const string Caller = "USR-0001";

    public NodeServiceTests()
    {
        _service = new NodeService(_nodes, new FakeCounterRepository());
    }

    // Real Colombo-area coordinates, per the D2 definition of done.
    private static CreateNodeRequest ValidCreate(
        double latitude = 6.7729,
        double longitude = 79.8816,
        double capacityKwh = 250.0,
        int batterySlotCount = 8,
        string type = "AC",
        List<ScheduleWindowDto>? schedule = null)
        => new(
            "Moratuwa Microgrid Hub A",
            new NodeLocationDto(latitude, longitude, "Katubedda, Moratuwa"),
            capacityKwh,
            batterySlotCount,
            type,
            schedule ?? new List<ScheduleWindowDto> { new("Mon", "06:00", "18:00") });

    private static UpdateNodeRequest ValidUpdate(
        string name = "Moratuwa Microgrid Hub A (renamed)",
        double latitude = 6.7729,
        double longitude = 79.8816,
        double capacityKwh = 300.0,
        int batterySlotCount = 10,
        string type = "AC",
        List<ScheduleWindowDto>? schedule = null)
        => new(
            name,
            new NodeLocationDto(latitude, longitude, "Katubedda, Moratuwa"),
            capacityKwh,
            batterySlotCount,
            type,
            schedule ?? new List<ScheduleWindowDto> { new("Mon", "06:00", "18:00") });

    // -- Create: happy path ---------------------------------------------------

    [Fact] // REQ-NODE 4.1 step 4
    public async Task CreateAsync_WithValidRequest_StoresTheNodeAsActive()
    {
        var result = await _service.CreateAsync(ValidCreate(), Caller);

        Assert.True(result.IsActive);
        var stored = Assert.Single(_nodes.Nodes);
        Assert.True(stored.IsActive);
        Assert.Equal("Moratuwa Microgrid Hub A", stored.Name);
        Assert.Equal(6.7729, stored.Location.Latitude);
    }

    [Fact] // data-model.md 1.3 - nodeId is "ND-0003" style and sequential
    public async Task CreateAsync_GeneratesSequentialPaddedNodeIds()
    {
        var first = await _service.CreateAsync(ValidCreate(), Caller);
        var second = await _service.CreateAsync(ValidCreate(), Caller);

        Assert.Equal("ND-0001", first.NodeId);
        Assert.Equal("ND-0002", second.NodeId);
    }

    [Fact] // createdBy is the authenticated caller, never the request body
    public async Task CreateAsync_RecordsTheCallerAsCreatedBy()
    {
        var result = await _service.CreateAsync(ValidCreate(), Caller);

        Assert.Equal(Caller, result.CreatedBy);
    }

    [Fact] // BR-13 - server clock only
    public async Task CreateAsync_StampsCreatedAtAndUpdatedAtInUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await _service.CreateAsync(ValidCreate(), Caller);

        Assert.InRange(result.CreatedAt, before, DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
    }

    // -- Create: coordinate validation ---------------------------------------

    [Theory] // REQ-NODE AC-2
    [InlineData(95.0)]
    [InlineData(90.001)]
    [InlineData(-95.0)]
    public async Task CreateAsync_LatitudeOutsideRange_IsRejected(double latitude)
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(ValidCreate(latitude: latitude), Caller));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("latitude", ex.Field);
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory] // bounds are inclusive - [-90, 90]
    [InlineData(90.0)]
    [InlineData(-90.0)]
    [InlineData(0.0)]
    public async Task CreateAsync_LatitudeAtOrInsideTheBounds_IsAccepted(double latitude)
    {
        var result = await _service.CreateAsync(ValidCreate(latitude: latitude), Caller);

        Assert.Equal(latitude, result.Location.Latitude);
    }

    [Theory]
    [InlineData(180.001)]
    [InlineData(-180.001)]
    [InlineData(200.0)]
    public async Task CreateAsync_LongitudeOutsideRange_IsRejected(double longitude)
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(ValidCreate(longitude: longitude), Caller));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("longitude", ex.Field);
    }

    [Theory]
    [InlineData(180.0)]
    [InlineData(-180.0)]
    public async Task CreateAsync_LongitudeAtTheBounds_IsAccepted(double longitude)
    {
        var result = await _service.CreateAsync(ValidCreate(longitude: longitude), Caller);

        Assert.Equal(longitude, result.Location.Longitude);
    }

    // -- Create: capacity and battery slots -----------------------------------

    [Theory] // REQ-NODE 4.1 step 3 - capacity must be > 0
    [InlineData(0.0)]
    [InlineData(-5.0)]
    public async Task CreateAsync_CapacityNotPositive_IsRejected(double capacity)
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(ValidCreate(capacityKwh: capacity), Caller));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("capacityKwh", ex.Field);
    }

    [Theory] // REQ-NODE 4.1 step 3 - battery slots >= 1
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_BatterySlotCountBelowOne_IsRejected(int slots)
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(ValidCreate(batterySlotCount: slots), Caller));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("batterySlotCount", ex.Field);
    }

    [Theory] // data-model.md 1.3 - type is AC or DC
    [InlineData("HVDC")]
    [InlineData("")]
    [InlineData("ac-ish")]
    public async Task CreateAsync_UnknownNodeType_IsRejected(string type)
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(ValidCreate(type: type), Caller));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("type", ex.Field);
    }

    // -- Create: schedule validation ------------------------------------------

    [Theory] // REQ-NODE 4.1 step 3 - open must be strictly before close
    [InlineData("18:00", "06:00")]
    [InlineData("12:00", "12:00")]
    public async Task CreateAsync_WindowThatDoesNotOpenBeforeItCloses_IsRejected(
        string open, string close)
    {
        var schedule = new List<ScheduleWindowDto> { new("Mon", open, close) };

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(ValidCreate(schedule: schedule), Caller));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("schedule", ex.Field);
    }

    // data-model.md 1.3 stores these as zero-padded "HH:mm". Parse strictly with
    // TimeOnly.TryParseExact - plain TryParse happily accepts "6am" and "6:00",
    // which would then sit in Mongo in a format nothing else expects.
    [Theory]
    [InlineData("6am")]
    [InlineData("6:00")]
    [InlineData("25:00")]
    [InlineData("noon")]
    [InlineData("")]
    public async Task CreateAsync_TimeNotInStrictHHmmFormat_IsRejected(string openTime)
    {
        var schedule = new List<ScheduleWindowDto> { new("Mon", openTime, "18:00") };

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(ValidCreate(schedule: schedule), Caller));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("schedule", ex.Field);
    }

    [Fact] // REQ-NODE EC-2 - overlapping windows are ambiguous availability
    public async Task CreateAsync_OverlappingWindowsOnTheSameDay_IsRejected()
    {
        var schedule = new List<ScheduleWindowDto>
        {
            new("Mon", "06:00", "14:00"),
            new("Mon", "12:00", "18:00"),
        };

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(ValidCreate(schedule: schedule), Caller));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("schedule", ex.Field);
    }

    [Fact] // touching at the boundary is not overlapping - a split shift is legal
    public async Task CreateAsync_AdjacentWindowsOnTheSameDay_IsAccepted()
    {
        var schedule = new List<ScheduleWindowDto>
        {
            new("Mon", "06:00", "12:00"),
            new("Mon", "12:00", "18:00"),
        };

        var result = await _service.CreateAsync(ValidCreate(schedule: schedule), Caller);

        Assert.Equal(2, result.Schedule.Count);
    }

    [Fact] // the same clock times on different days do not overlap
    public async Task CreateAsync_SameHoursOnDifferentDays_IsAccepted()
    {
        var schedule = new List<ScheduleWindowDto>
        {
            new("Mon", "06:00", "18:00"),
            new("Tue", "06:00", "18:00"),
        };

        var result = await _service.CreateAsync(ValidCreate(schedule: schedule), Caller);

        Assert.Equal(2, result.Schedule.Count);
    }

    // -- Create: nothing is written when validation fails ----------------------

    [Fact] // REQ-NODE AC-2 - "nothing written"
    public async Task CreateAsync_WhenValidationFails_WritesNothing()
    {
        await Assert.ThrowsAsync<DomainException>(
            () => _service.CreateAsync(ValidCreate(latitude: 95.0), Caller));

        Assert.Empty(_nodes.Nodes);
    }

    // -- Read ------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_UnknownNode_IsRejectedWithNotFound()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.GetByIdAsync("ND-9999"));

        Assert.Equal("E4404", ex.Code);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task GetByIdAsync_KnownNode_ReturnsIt()
    {
        var created = await _service.CreateAsync(ValidCreate(), Caller);

        var fetched = await _service.GetByIdAsync(created.NodeId);

        Assert.Equal(created.NodeId, fetched.NodeId);
        Assert.Equal("Moratuwa Microgrid Hub A", fetched.Name);
    }

    [Fact] // the active filter is the repository's job, not a post-filter here
    public async Task GetAllAsync_PassesTheActiveFilterToTheRepository()
    {
        await _service.GetAllAsync(active: true, page: 1, pageSize: 20);

        Assert.True(_nodes.LastActiveFilter);
    }

    [Theory] // an unbounded pageSize lets one request pull the whole collection
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(5000)]
    public async Task GetAllAsync_ClampsAnUnreasonablePageSize(int pageSize)
    {
        await _service.GetAllAsync(active: null, page: 1, pageSize: pageSize);

        Assert.InRange(_nodes.LastPageSize, 1, 100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetAllAsync_ClampsPageNumberToAtLeastOne(int page)
    {
        await _service.GetAllAsync(active: null, page: page, pageSize: 20);

        Assert.True(_nodes.LastPage >= 1);
    }

    // -- Update ----------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_UnknownNode_IsRejectedWithNotFound()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.UpdateAsync("ND-9999", ValidUpdate()));

        Assert.Equal("E4404", ex.Code);
    }

    [Fact]
    public async Task UpdateAsync_AppliesTheEditableFields()
    {
        var created = await _service.CreateAsync(ValidCreate(), Caller);

        var updated = await _service.UpdateAsync(created.NodeId, ValidUpdate());

        Assert.Equal("Moratuwa Microgrid Hub A (renamed)", updated.Name);
        Assert.Equal(300.0, updated.CapacityKwh);
        Assert.Equal(10, updated.BatterySlotCount);
    }

    [Fact] // nodeId is the reference every reservation holds - it must not move
    public async Task UpdateAsync_DoesNotChangeIdentityOrCreationMetadata()
    {
        var created = await _service.CreateAsync(ValidCreate(), Caller);

        var updated = await _service.UpdateAsync(created.NodeId, ValidUpdate());

        Assert.Equal(created.NodeId, updated.NodeId);
        Assert.Equal(created.CreatedBy, updated.CreatedBy);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_MovesUpdatedAtForward()
    {
        var created = await _service.CreateAsync(ValidCreate(), Caller);

        var updated = await _service.UpdateAsync(created.NodeId, ValidUpdate());

        Assert.True(updated.UpdatedAt >= created.UpdatedAt);
    }

    [Fact] // an update can produce exactly the same invalid state a create can
    public async Task UpdateAsync_RevalidatesCoordinates()
    {
        var created = await _service.CreateAsync(ValidCreate(), Caller);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.UpdateAsync(created.NodeId, ValidUpdate(latitude: 95.0)));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("latitude", ex.Field);
    }

    [Fact]
    public async Task UpdateAsync_RevalidatesTheSchedule()
    {
        var created = await _service.CreateAsync(ValidCreate(), Caller);
        var bad = new List<ScheduleWindowDto>
        {
            new("Mon", "06:00", "14:00"),
            new("Mon", "12:00", "18:00"),
        };

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.UpdateAsync(created.NodeId, ValidUpdate(schedule: bad)));

        Assert.Equal("E4002", ex.Code);
        Assert.Equal("schedule", ex.Field);
    }
}
