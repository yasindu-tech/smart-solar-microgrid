using System.Globalization;
using SmartSolar.Api.DTOs.Common.Nodes;
using SmartSolar.Api.DTOs.Request.Nodes;
using SmartSolar.Api.DTOs.Response.Nodes;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class NodeService
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private readonly INodeRepository _nodeRepo;
    private readonly ICounterRepository _counterRepo;

    public NodeService(INodeRepository nodeRepo, ICounterRepository counterRepo)
    {
        _nodeRepo = nodeRepo;
        _counterRepo = counterRepo;
    }

    public async Task<NodeResponse> CreateAsync(CreateNodeRequest request, string callerUserId)
    {
        var schedule = ValidateAndMapSchedule(
            request.Location, request.CapacityKwh, request.BatterySlotCount, request.Type, request.Schedule);

        var sequence = await _counterRepo.GetNextSequenceValueAsync("nodeid");
        var now = DateTime.UtcNow;

        var node = new Node
        {
            NodeId = $"ND-{sequence:D4}",
            Name = request.Name,
            Location = ToModel(request.Location),
            CapacityKwh = request.CapacityKwh,
            BatterySlotCount = request.BatterySlotCount,
            Type = request.Type,
            Schedule = schedule,
            IsActive = true,
            CreatedBy = callerUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _nodeRepo.CreateAsync(node);
        return MapToResponse(node);
    }

    public async Task<NodeResponse> GetByIdAsync(string nodeId)
    {
        var node = await _nodeRepo.GetByNodeIdAsync(nodeId)
            ?? throw new DomainException("E4404", $"Node '{nodeId}' was not found.", 404);

        return MapToResponse(node);
    }

    public async Task<(List<NodeResponse> Items, int Total, int Page, int PageSize)> GetAllAsync(
        bool? active, int page, int pageSize)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < MinPageSize ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var (items, total) = await _nodeRepo.GetAllAsync(active, safePage, safePageSize);
        return (items.Select(MapToResponse).ToList(), total, safePage, safePageSize);
    }

    public async Task<NodeResponse> UpdateAsync(string nodeId, UpdateNodeRequest request)
    {
        var node = await _nodeRepo.GetByNodeIdAsync(nodeId)
            ?? throw new DomainException("E4404", $"Node '{nodeId}' was not found.", 404);

        var schedule = ValidateAndMapSchedule(
            request.Location, request.CapacityKwh, request.BatterySlotCount, request.Type, request.Schedule);

        node.Name = request.Name;
        node.Location = ToModel(request.Location);
        node.CapacityKwh = request.CapacityKwh;
        node.BatterySlotCount = request.BatterySlotCount;
        node.Type = request.Type;
        node.Schedule = schedule;
        node.UpdatedAt = DateTime.UtcNow;

        await _nodeRepo.UpdateAsync(node);
        return MapToResponse(node);
    }

    private static List<ScheduleWindow> ValidateAndMapSchedule(
        NodeLocationDto location,
        double capacityKwh,
        int batterySlotCount,
        string type,
        List<ScheduleWindowDto> schedule)
    {
        if (location.Latitude is < -90 or > 90)
            throw new DomainException("E4002", "Latitude must be between -90 and 90.", 400, "latitude");

        if (location.Longitude is < -180 or > 180)
            throw new DomainException("E4002", "Longitude must be between -180 and 180.", 400, "longitude");

        if (capacityKwh <= 0)
            throw new DomainException("E4002", "Capacity must be greater than zero.", 400, "capacityKwh");

        if (batterySlotCount < 1)
            throw new DomainException("E4002", "Battery slot count must be at least 1.", 400, "batterySlotCount");

        if (type != "AC" && type != "DC")
            throw new DomainException("E4002", "Type must be 'AC' or 'DC'.", 400, "type");

        var parsed = new List<(ScheduleWindow Window, TimeOnly Open, TimeOnly Close)>();
        foreach (var window in schedule)
        {
            var openOk = TimeOnly.TryParseExact(
                window.OpenTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var open);
            var closeOk = TimeOnly.TryParseExact(
                window.CloseTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var close);

            if (!openOk || !closeOk)
                throw new DomainException("E4002", "Schedule times must be in strict HH:mm format.", 400, "schedule");

            if (open >= close)
                throw new DomainException("E4002", "Schedule open time must be strictly before close time.", 400, "schedule");

            parsed.Add((new ScheduleWindow
            {
                DayOfWeek = window.DayOfWeek,
                OpenTime = window.OpenTime,
                CloseTime = window.CloseTime,
            }, open, close));
        }

        foreach (var group in parsed.GroupBy(p => p.Window.DayOfWeek))
        {
            var ordered = group.OrderBy(p => p.Open).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].Open < ordered[i - 1].Close)
                    throw new DomainException("E4002", "Schedule windows on the same day must not overlap.", 400, "schedule");
            }
        }

        return [.. parsed.Select(p => p.Window)];
    }

    private static NodeLocation ToModel(NodeLocationDto location)
        => new()
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            AddressLine = location.AddressLine,
        };

    private static NodeResponse MapToResponse(Node node)
        => new(
            node.NodeId,
            node.Name,
            new NodeLocationDto(node.Location.Latitude, node.Location.Longitude, node.Location.AddressLine),
            node.CapacityKwh,
            node.BatterySlotCount,
            node.Type,
            [.. node.Schedule.Select(w => new ScheduleWindowDto(w.DayOfWeek, w.OpenTime, w.CloseTime))],
            node.IsActive,
            node.CreatedBy,
            node.CreatedAt,
            node.UpdatedAt);
}
