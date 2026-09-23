// ---------------------------------------------------------------------------
//  File        : ReservationServiceTests.cs
//  Project     : Smart Solar Microgrid Trading System - Web API tests
//  Description : Comprehensive unit tests for ReservationService and DashboardService:
//                verifying BR-05, BR-06, BR-07, BR-08, BR-15, BR-16, BR-17, BR-18,
//                RBAC ownership, state machine transitions, and dashboard metrics.
// ---------------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartSolar.Api.Configuration;
using SmartSolar.Api.DTOs.Request.Reservations;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;
using SmartSolar.Api.Services;
using Xunit;

namespace SmartSolar.Api.Tests;

// ---------------------------------------------------------------------------
//  Fakes
// ---------------------------------------------------------------------------

internal sealed class FakeReservationRepository : IReservationRepository
{
    public readonly List<EnergyReservation> Reservations = new();

    public Task<EnergyReservation?> GetByIdAsync(string reservationId)
        => Task.FromResult(Reservations.FirstOrDefault(r => r.ReservationId == reservationId));

    public Task<(List<EnergyReservation> Items, int Total)> GetMineAsync(
        string nic, string? status, DateTime? from, DateTime? to, string? search, int page, int pageSize)
    {
        var query = Reservations.Where(r => r.Nic == nic);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status.Trim(), "history", StringComparison.OrdinalIgnoreCase))
            {
                var now = DateTime.UtcNow;
                query = query.Where(r => r.Status is "Completed" or "Cancelled" or "Expired" || r.SlotEndTime < now);
            }
            else
            {
                query = query.Where(r => r.Status == status.Trim());
            }
        }

        if (from.HasValue)
        {
            query = query.Where(r => r.SlotStartTime >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.SlotStartTime <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(r => r.ReservationId.Contains(s, StringComparison.OrdinalIgnoreCase)
                                  || r.NodeId.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.OrderByDescending(r => r.CreatedAt).ToList();
        var paged = list.Skip((page - 1) * pageSize).Limit(pageSize).ToList();
        return Task.FromResult((paged, list.Count));
    }

    public Task<(List<EnergyReservation> Items, int Total)> GetAllAsync(
        string? status, string? nodeId, DateTime? from, DateTime? to, string? search, int page, int pageSize)
    {
        var query = Reservations.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);
        if (!string.IsNullOrWhiteSpace(nodeId)) query = query.Where(r => r.NodeId == nodeId);
        if (from.HasValue) query = query.Where(r => r.SlotStartTime >= from.Value);
        if (to.HasValue) query = query.Where(r => r.SlotStartTime <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.ReservationId.Contains(search, StringComparison.OrdinalIgnoreCase)
                                  || r.Nic.Contains(search, StringComparison.OrdinalIgnoreCase));

        var list = query.OrderByDescending(r => r.CreatedAt).ToList();
        var paged = list.Skip((page - 1) * pageSize).Limit(pageSize).ToList();
        return Task.FromResult((paged, list.Count));
    }

    public Task CreateAsync(EnergyReservation reservation)
    {
        Reservations.Add(reservation);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EnergyReservation reservation)
    {
        var index = Reservations.FindIndex(r => r.ReservationId == reservation.ReservationId);
        if (index >= 0) Reservations[index] = reservation;
        return Task.CompletedTask;
    }

    public Task<(int ActiveCount, int PendingCount, EnergyReservation? NextReservation, List<EnergyReservation> RecentHistory)>
        GetDashboardMetricsAsync(string nic, DateTime nowUtc)
    {
        var activeCount = Reservations.Count(r => r.Nic == nic && r.Status == "Approved" && r.SlotStartTime >= nowUtc);
        var pendingCount = Reservations.Count(r => r.Nic == nic && r.Status == "Pending");
        var next = Reservations
            .Where(r => r.Nic == nic && r.Status is "Pending" or "Approved" && r.SlotStartTime >= nowUtc)
            .OrderBy(r => r.SlotStartTime)
            .FirstOrDefault();
        var history = Reservations
            .Where(r => r.Nic == nic && (r.Status is "Completed" or "Cancelled" or "Expired" || r.SlotEndTime < nowUtc))
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToList();

        return Task.FromResult((activeCount, pendingCount, next, history));
    }

    public Task<bool> TryApproveAsync(string reservationId, string qrToken, DateTime qrIssuedAt)
    {
        lock (Reservations)
        {
            var res = Reservations.FirstOrDefault(r => r.ReservationId == reservationId && r.Status == ReservationStateMachine.Pending);
            if (res == null) return Task.FromResult(false);

            res.Status = ReservationStateMachine.Approved;
            res.QrToken = qrToken;
            res.QrIssuedAt = qrIssuedAt;
            res.UpdatedAt = qrIssuedAt;
            return Task.FromResult(true);
        }
    }

    public Task<bool> RevertApprovalAsync(string reservationId, DateTime updatedAt)
    {
        lock (Reservations)
        {
            var res = Reservations.FirstOrDefault(r => r.ReservationId == reservationId && r.Status == ReservationStateMachine.Approved);
            if (res == null) return Task.FromResult(false);

            res.Status = ReservationStateMachine.Pending;
            res.QrToken = null;
            res.QrIssuedAt = null;
            res.UpdatedAt = updatedAt;
            return Task.FromResult(true);
        }
    }

    public Task<(bool Success, string? PreviousStatus)> TryCancelAsync(
        string reservationId, string cancelledBy, DateTime cancelledAt)
    {
        lock (Reservations)
        {
            var res = Reservations.FirstOrDefault(r => r.ReservationId == reservationId &&
                (r.Status == ReservationStateMachine.Pending || r.Status == ReservationStateMachine.Approved));
            if (res == null) return Task.FromResult((false, (string?)null));

            var prevStatus = res.Status;
            res.Status = ReservationStateMachine.Cancelled;
            res.CancelledAt = cancelledAt;
            res.CancelledBy = cancelledBy;
            res.QrToken = null;
            res.UpdatedAt = cancelledAt;
            return Task.FromResult((true, (string?)prevStatus));
        }
    }

    public Task<(bool Success, string? PreviousStatus)> TryExpireAsync(string reservationId, DateTime nowUtc)
    {
        lock (Reservations)
        {
            var res = Reservations.FirstOrDefault(r => r.ReservationId == reservationId &&
                (r.Status == ReservationStateMachine.Pending || r.Status == ReservationStateMachine.Approved) &&
                r.SlotEndTime < nowUtc);
            if (res == null) return Task.FromResult((false, (string?)null));

            var prevStatus = res.Status;
            res.Status = ReservationStateMachine.Expired;
            res.QrToken = null;
            res.UpdatedAt = nowUtc;
            return Task.FromResult((true, (string?)prevStatus));
        }
    }

    public Task<(int Total, Dictionary<string, int> ByStatus)> GetStatusCountsAsync()
    {
        lock (Reservations)
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Pending"] = Reservations.Count(r => r.Status == "Pending"),
                ["Approved"] = Reservations.Count(r => r.Status == "Approved"),
                ["Completed"] = Reservations.Count(r => r.Status == "Completed"),
                ["Cancelled"] = Reservations.Count(r => r.Status == "Cancelled"),
                ["Expired"] = Reservations.Count(r => r.Status == "Expired"),
            };
            return Task.FromResult((Reservations.Count, dict));
        }
    }
}


internal static class EnumerableExtensions
{
    public static IEnumerable<T> Limit<T>(this IEnumerable<T> source, int count) => source.Take(count);
}

internal sealed class FakeSlotRepository : IEnergyBookingSlotRepository
{
    public readonly List<EnergyBookingSlot> Slots = new();

    public Task<EnergyBookingSlot?> GetBySlotIdAsync(string slotId)
    {
        lock (Slots)
        {
            return Task.FromResult(Slots.FirstOrDefault(s => s.SlotId == slotId));
        }
    }

    public Task<bool> TryDecrementCapacityAsync(string slotId)
    {
        lock (Slots)
        {
            var slot = Slots.FirstOrDefault(s => s.SlotId == slotId);
            if (slot == null || slot.AvailableCapacity <= 0) return Task.FromResult(false);
            slot.AvailableCapacity--;
            return Task.FromResult(true);
        }
    }

    public Task IncrementCapacityAsync(string slotId)
    {
        lock (Slots)
        {
            var slot = Slots.FirstOrDefault(s => s.SlotId == slotId);
            if (slot != null) slot.AvailableCapacity++;
            return Task.CompletedTask;
        }
    }

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

internal sealed class FakeReservationCounterRepository : ICounterRepository
{
    private int _sequence = 5;

    public Task<int> GetNextSequenceValueAsync(string sequenceName)
    {
        _sequence++;
        return Task.FromResult(_sequence);
    }
}

internal sealed class FakeProsumerRepository : ProsumerRepository
{
    public readonly Dictionary<string, Prosumer> Prosumers = new();

    public override Task<Prosumer?> GetByNicAsync(string nic)
    {
        Prosumers.TryGetValue(nic, out var prosumer);
        return Task.FromResult(prosumer);
    }
}

// ---------------------------------------------------------------------------
//  Tests
// ---------------------------------------------------------------------------

public class ReservationServiceTests
{
    private readonly FakeReservationRepository _reservationRepo = new();
    private readonly FakeSlotRepository _slotRepo = new();
    private readonly FakeSolarStationRepository _nodeRepo = new();
    private readonly FakeProsumerRepository _prosumerRepo = new();
    private readonly FakeReservationCounterRepository _counterRepo = new();
    private readonly QrTokenService _qrService = new(Options.Create(new QrSettings { SigningSecret = "test-secret-key-that-is-long-enough-32-chars!" }));

    private readonly ReservationService _service;

    public ReservationServiceTests()
    {
        // Initialize ReservationService with in-memory fakes.
        _prosumerRepo.Prosumers["199512345678"] = new Prosumer
        {
            Nic = "199512345678",
            FullName = "Default Prosumer",
            Status = "Active"
        };

        _service = new ReservationService(
            _reservationRepo,
            _slotRepo,
            _nodeRepo,
            _prosumerRepo,
            _counterRepo,
            _qrService,
            NullLogger<ReservationService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_Succeeds_AndAllocatesPendingReservation_RS0006()
    {
        var now = DateTime.UtcNow;
        var nic = "199512345678";

        _prosumerRepo.Prosumers[nic] = new Prosumer
        {
            Nic = nic,
            FullName = "John Doe",
            Status = "Active"
        };

        _nodeRepo.Nodes.Add(new SolarStationInfo
        {
            NodeId = "ND-001",
            Name = "Main Grid Station",
            IsActive = true
        });

        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-0001",
            NodeId = "ND-001",
            StartTime = now.AddDays(2),
            EndTime = now.AddDays(2).AddHours(2),
            TotalCapacity = 5,
            AvailableCapacity = 3,
            EnergyOfferedKwh = 50,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);

        var request = new CreateReservationRequest
        {
            NodeId = "ND-001",
            SlotId = "SL-0001",
            RequestedKwh = 15.5
        };

        var result = await _service.CreateAsync(nic, request);

        Assert.NotNull(result);
        Assert.Equal("RS-0006", result.ReservationId);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(15.5, result.RequestedKwh);
        Assert.Null(result.QrToken);
        Assert.Single(_reservationRepo.Reservations);
    }

    [Fact]
    public async Task CreateAsync_Rejects_WhenProsumerNotActive_BR17()
    {
        var nic = "199512345678";
        _prosumerRepo.Prosumers[nic] = new Prosumer
        {
            Nic = nic,
            Status = "PendingActivation"
        };

        var request = new CreateReservationRequest
        {
            NodeId = "ND-001",
            SlotId = "SL-0001",
            RequestedKwh = 10
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(nic, request));
        Assert.Equal("E4302", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_Rejects_WhenNodeInactive_BR16()
    {
        var now = DateTime.UtcNow;
        var nic = "199512345678";
        _prosumerRepo.Prosumers[nic] = new Prosumer { Nic = nic, Status = "Active" };
        _nodeRepo.Nodes.Add(new SolarStationInfo { NodeId = "ND-001", IsActive = false });

        var request = new CreateReservationRequest
        {
            NodeId = "ND-001",
            SlotId = "SL-0001",
            RequestedKwh = 10
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(nic, request));
        Assert.Equal("E4404", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_Rejects_WhenSlotInPast_BR08()
    {
        var now = DateTime.UtcNow;
        var nic = "199512345678";
        _prosumerRepo.Prosumers[nic] = new Prosumer { Nic = nic, Status = "Active" };
        _nodeRepo.Nodes.Add(new SolarStationInfo { NodeId = "ND-001", IsActive = true });
        _slotRepo.Slots.Add(new EnergyBookingSlot
        {
            SlotId = "SL-0001",
            NodeId = "ND-001",
            StartTime = now.AddHours(-1),
            EndTime = now.AddHours(1),
            TotalCapacity = 5,
            AvailableCapacity = 3,
            EnergyOfferedKwh = 20,
            IsActive = true
        });

        var request = new CreateReservationRequest
        {
            NodeId = "ND-001",
            SlotId = "SL-0001",
            RequestedKwh = 10
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(nic, request));
        Assert.Equal("E4104", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_Rejects_WhenSlotBeyond7Days_BR05()
    {
        var now = DateTime.UtcNow;
        var nic = "199512345678";
        _prosumerRepo.Prosumers[nic] = new Prosumer { Nic = nic, Status = "Active" };
        _nodeRepo.Nodes.Add(new SolarStationInfo { NodeId = "ND-001", IsActive = true });
        _slotRepo.Slots.Add(new EnergyBookingSlot
        {
            SlotId = "SL-0001",
            NodeId = "ND-001",
            StartTime = now.AddDays(8),
            EndTime = now.AddDays(8).AddHours(2),
            TotalCapacity = 5,
            AvailableCapacity = 3,
            EnergyOfferedKwh = 20,
            IsActive = true
        });

        var request = new CreateReservationRequest
        {
            NodeId = "ND-001",
            SlotId = "SL-0001",
            RequestedKwh = 10
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(nic, request));
        Assert.Equal("E4101", ex.Code);
    }

    [Fact]
    public async Task CreateAsync_Rejects_WhenSlotFull_BR15()
    {
        var now = DateTime.UtcNow;
        var nic = "199512345678";
        _prosumerRepo.Prosumers[nic] = new Prosumer { Nic = nic, Status = "Active" };
        _nodeRepo.Nodes.Add(new SolarStationInfo { NodeId = "ND-001", IsActive = true });
        _slotRepo.Slots.Add(new EnergyBookingSlot
        {
            SlotId = "SL-0001",
            NodeId = "ND-001",
            StartTime = now.AddDays(2),
            EndTime = now.AddDays(2).AddHours(2),
            TotalCapacity = 5,
            AvailableCapacity = 0,
            EnergyOfferedKwh = 20,
            IsActive = true
        });

        var request = new CreateReservationRequest
        {
            NodeId = "ND-001",
            SlotId = "SL-0001",
            RequestedKwh = 10
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(nic, request));
        Assert.Equal("E4106", ex.Code);
    }

    [Fact]
    public void QrTokenService_Generates_ValidSignedToken()
    {
        var token = _qrService.GenerateToken("RS-0042");
        Assert.NotNull(token);
        Assert.Contains(".", token);
        var parts = token.Split('.');
        Assert.Equal(2, parts.Length);
        Assert.NotEmpty(parts[0]);
        Assert.NotEmpty(parts[1]);
    }

    [Fact]
    public async Task SlotRepository_AtomicCapacity_DecrementsAndRestores()
    {
        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-0001",
            NodeId = "ND-001",
            TotalCapacity = 2,
            AvailableCapacity = 1,
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            EnergyOfferedKwh = 20.0,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);

        // First decrement should succeed
        var dec1 = await _slotRepo.TryDecrementCapacityAsync("SL-0001");
        Assert.True(dec1);
        Assert.Equal(0, slot.AvailableCapacity);

        // Second decrement should fail because availableCapacity == 0
        var dec2 = await _slotRepo.TryDecrementCapacityAsync("SL-0001");
        Assert.False(dec2);

        // Restore should increment back to 1
        await _slotRepo.IncrementCapacityAsync("SL-0001");
        Assert.Equal(1, slot.AvailableCapacity);
    }

    [Fact]
    public async Task UpdateAsync_Rejects_WhenNoticeLessThan12Hours_BR06()
    {
        var now = DateTime.UtcNow;
        var reservation = new EnergyReservation
        {
            ReservationId = "RS-0010",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-0001",
            SlotStartTime = now.AddHours(6), // Starts in 6 hours!
            SlotEndTime = now.AddHours(8),
            RequestedKwh = 10,
            Status = "Pending",
            CreatedAt = now.AddDays(-1)
        };
        _reservationRepo.Reservations.Add(reservation);

        var request = new UpdateReservationRequest
        {
            SlotId = "SL-0002",
            RequestedKwh = 15
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.UpdateAsync("RS-0010", request, "199512345678", "Prosumer"));

        Assert.Equal("E4103", ex.Code);
    }

    [Fact]
    public async Task CancelAsync_Rejects_WhenNoticeLessThan12Hours_BR07()
    {
        var now = DateTime.UtcNow;
        var reservation = new EnergyReservation
        {
            ReservationId = "RS-0011",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-0001",
            SlotStartTime = now.AddHours(11), // 11 hours out!
            SlotEndTime = now.AddHours(13),
            RequestedKwh = 10,
            Status = "Pending",
            CreatedAt = now.AddDays(-1)
        };
        _reservationRepo.Reservations.Add(reservation);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.CancelAsync("RS-0011", "199512345678", "Prosumer"));

        Assert.Equal("E4102", ex.Code);
    }

    [Fact]
    public async Task CancelAsync_RestoresCapacity_WhenReservationWasApproved_BR15()
    {
        var now = DateTime.UtcNow;
        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-0001",
            NodeId = "ND-001",
            TotalCapacity = 4,
            AvailableCapacity = 2,
            StartTime = now.AddDays(2),
            EndTime = now.AddDays(2).AddHours(2),
            EnergyOfferedKwh = 20,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);

        var reservation = new EnergyReservation
        {
            ReservationId = "RS-0012",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-0001",
            SlotStartTime = slot.StartTime,
            SlotEndTime = slot.EndTime,
            RequestedKwh = 10,
            Status = "Approved",
            QrToken = "some-token",
            CreatedAt = now.AddDays(-1)
        };
        _reservationRepo.Reservations.Add(reservation);

        var cancelled = await _service.CancelAsync("RS-0012", "199512345678", "Prosumer");

        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Null(cancelled.QrToken);
        // Available capacity must have restored from 2 to 3!
        Assert.Equal(3, slot.AvailableCapacity);
    }

    [Fact]
    public async Task ApproveAsync_DecrementsCapacity_AndIssuesQr_BR15_BR18()
    {
        var now = DateTime.UtcNow;
        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-0001",
            NodeId = "ND-001",
            TotalCapacity = 4,
            AvailableCapacity = 3,
            StartTime = now.AddDays(2),
            EndTime = now.AddDays(2).AddHours(2),
            EnergyOfferedKwh = 20,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);

        var reservation = new EnergyReservation
        {
            ReservationId = "RS-0013",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-0001",
            SlotStartTime = slot.StartTime,
            SlotEndTime = slot.EndTime,
            RequestedKwh = 10,
            Status = "Pending",
            CreatedAt = now
        };
        _reservationRepo.Reservations.Add(reservation);

        var approved = await _service.ApproveAsync("RS-0013");

        Assert.Equal("Approved", approved.Status);
        Assert.NotNull(approved.QrToken);
        Assert.Contains(".", approved.QrToken);
        Assert.NotNull(approved.QrIssuedAt);
        // Slot capacity decremented from 3 to 2!
        Assert.Equal(2, slot.AvailableCapacity);
    }

    [Fact]
    public async Task ApproveAsync_ConcurrentRequests_OnlyOneSucceeds_AndCapacityDecrementsOnce_EC9()
    {
        var now = DateTime.UtcNow;
        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-CONC-01",
            NodeId = "ND-001",
            TotalCapacity = 5,
            AvailableCapacity = 5,
            StartTime = now.AddDays(2),
            EndTime = now.AddDays(2).AddHours(2),
            EnergyOfferedKwh = 20,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);

        var reservation = new EnergyReservation
        {
            ReservationId = "RS-CONC-01",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-CONC-01",
            SlotStartTime = slot.StartTime,
            SlotEndTime = slot.EndTime,
            RequestedKwh = 10,
            Status = "Pending",
            CreatedAt = now
        };
        _reservationRepo.Reservations.Add(reservation);

        // Run 10 concurrent approval tasks on the same reservation
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            try
            {
                await _service.ApproveAsync("RS-CONC-01");
                return (Success: true, ErrorCode: (string?)null);
            }
            catch (DomainException ex)
            {
                return (Success: false, ErrorCode: (string?)ex.Code);
            }
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r.Success);
        var conflictCount = results.Count(r => !r.Success && r.ErrorCode == "E4105");

        Assert.Equal(1, successCount);
        Assert.Equal(9, conflictCount);
        // Slot capacity MUST decrement exactly once (from 5 down to 4), never double-decremented!
        Assert.Equal(4, slot.AvailableCapacity);
    }

    [Fact]
    public async Task ApproveAsync_WhenSlotCapacityExhausted_ThrowsE4106()
    {
        var now = DateTime.UtcNow;
        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-CONC-02",
            NodeId = "ND-001",
            TotalCapacity = 5,
            AvailableCapacity = 0, // No capacity remaining
            StartTime = now.AddDays(2),
            EndTime = now.AddDays(2).AddHours(2),
            EnergyOfferedKwh = 20,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);

        var reservation = new EnergyReservation
        {
            ReservationId = "RS-CONC-02",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-CONC-02",
            SlotStartTime = slot.StartTime,
            SlotEndTime = slot.EndTime,
            RequestedKwh = 10,
            Status = "Pending",
            CreatedAt = now
        };
        _reservationRepo.Reservations.Add(reservation);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.ApproveAsync("RS-CONC-02"));

        Assert.Equal("E4106", ex.Code);
        Assert.Equal("Pending", reservation.Status);
        Assert.Equal(0, slot.AvailableCapacity);
    }

    [Fact]
    public async Task CancelAsync_ConcurrentRequests_OnlyOneSucceeds_AndCapacityRestoresOnce()
    {
        var now = DateTime.UtcNow;
        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-CONC-03",
            NodeId = "ND-001",
            TotalCapacity = 5,
            AvailableCapacity = 2,
            StartTime = now.AddDays(2),
            EndTime = now.AddDays(2).AddHours(2),
            EnergyOfferedKwh = 20,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);

        var reservation = new EnergyReservation
        {
            ReservationId = "RS-CONC-03",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-CONC-03",
            SlotStartTime = slot.StartTime,
            SlotEndTime = slot.EndTime,
            RequestedKwh = 10,
            Status = "Approved",
            QrToken = "token-123",
            CreatedAt = now.AddDays(-1)
        };
        _reservationRepo.Reservations.Add(reservation);

        // Run 10 concurrent cancellation requests on the same approved reservation
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            try
            {
                await _service.CancelAsync("RS-CONC-03", "199512345678", "Prosumer");
                return (Success: true, ErrorCode: (string?)null);
            }
            catch (DomainException ex)
            {
                return (Success: false, ErrorCode: (string?)ex.Code);
            }
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r.Success);
        var conflictCount = results.Count(r => !r.Success && r.ErrorCode == "E4105");

        Assert.Equal(1, successCount);
        Assert.Equal(9, conflictCount);
        // Slot capacity MUST restore exactly once (from 2 up to 3), never over-inflated!
        Assert.Equal(3, slot.AvailableCapacity);
    }

    [Fact]
    public async Task GetQrAsync_ThrowsE4201_WhenReservationNotApproved()
    {
        var now = DateTime.UtcNow;
        var reservation = new EnergyReservation
        {
            ReservationId = "RS-0014",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-0001",
            SlotStartTime = now.AddDays(2),
            SlotEndTime = now.AddDays(2).AddHours(2),
            RequestedKwh = 10,
            Status = "Pending",
            CreatedAt = now
        };
        _reservationRepo.Reservations.Add(reservation);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.GetQrAsync("RS-0014", "199512345678", "Prosumer"));

        Assert.Equal("E4201", ex.Code);
    }

    [Fact]
    public async Task DashboardService_Aggregates_Active_Pending_Next_And_History()
    {
        var now = DateTime.UtcNow;
        var nic = "199512345678";

        _reservationRepo.Reservations.AddRange(new[]
        {
            new EnergyReservation
            {
                ReservationId = "RS-0001",
                Nic = nic,
                NodeId = "ND-001",
                SlotId = "SL-0001",
                SlotStartTime = now.AddDays(1),
                SlotEndTime = now.AddDays(1).AddHours(2),
                Status = "Approved",
                CreatedAt = now.AddDays(-1)
            },
            new EnergyReservation
            {
                ReservationId = "RS-0002",
                Nic = nic,
                NodeId = "ND-001",
                SlotId = "SL-0002",
                SlotStartTime = now.AddDays(3),
                SlotEndTime = now.AddDays(3).AddHours(2),
                Status = "Pending",
                CreatedAt = now
            },
            new EnergyReservation
            {
                ReservationId = "RS-0003",
                Nic = nic,
                NodeId = "ND-001",
                SlotId = "SL-0003",
                SlotStartTime = now.AddDays(-2),
                SlotEndTime = now.AddDays(-2).AddHours(2),
                Status = "Completed",
                CreatedAt = now.AddDays(-3)
            }
        });

        var dashboardService = new DashboardService(_reservationRepo);
        var response = await dashboardService.GetProsumerDashboardAsync(nic);

        Assert.Equal(1, response.ActiveCount);
        Assert.Equal(1, response.PendingCount);
        Assert.NotNull(response.NextReservation);
        Assert.Equal("RS-0001", response.NextReservation!.ReservationId);
        Assert.Single(response.RecentHistory);
        Assert.Equal("RS-0003", response.RecentHistory[0].ReservationId);
    }

    [Fact]
    public async Task GetMineAsync_HistoryFilter_LazilyExpiresPastReservations_AndRestoresCapacityForApproved_ADR001()
    {
        var now = DateTime.UtcNow;
        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-EXP-01",
            NodeId = "ND-001",
            TotalCapacity = 4,
            AvailableCapacity = 2,
            StartTime = now.AddHours(-4),
            EndTime = now.AddHours(-2),
            EnergyOfferedKwh = 20,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);

        var reservation = new EnergyReservation
        {
            ReservationId = "RS-EXP-01",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-EXP-01",
            SlotStartTime = slot.StartTime,
            SlotEndTime = slot.EndTime,
            RequestedKwh = 10,
            Status = "Approved",
            QrToken = "qr-token-123",
            CreatedAt = now.AddDays(-2)
        };
        _reservationRepo.Reservations.Add(reservation);

        // Fetch prosumer history
        var response = await _service.GetMineAsync("199512345678", "history", 1, 10);

        Assert.Single(response.Items);
        var item = response.Items[0];
        // Status must be lazily transitioned to Expired before returning
        Assert.Equal("Expired", item.Status);
        Assert.Null(item.QrToken);

        // The persisted document must also be Expired
        Assert.Equal("Expired", reservation.Status);
        Assert.Null(reservation.QrToken);

        // Approved reservation capacity must be restored (+1) on expiry per BR-15
        Assert.Equal(3, slot.AvailableCapacity);
    }

    [Fact]
    public async Task GetByIdAsync_LazilyExpiresPastReservation_BeforeReturning()
    {
        var now = DateTime.UtcNow;
        var reservation = new EnergyReservation
        {
            ReservationId = "RS-EXP-02",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-0001",
            SlotStartTime = now.AddHours(-3),
            SlotEndTime = now.AddHours(-1),
            RequestedKwh = 5,
            Status = "Pending",
            CreatedAt = now.AddDays(-1)
        };
        _reservationRepo.Reservations.Add(reservation);

        var response = await _service.GetByIdAsync("RS-EXP-02", "199512345678", "Prosumer");

        Assert.Equal("Expired", response.Status);
        Assert.Equal("Expired", reservation.Status);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsE4302_WhenProsumerIsDeactivated_BR17_EC8()
    {
        var now = DateTime.UtcNow;
        var nic = "199512345678";

        // Prosumer is deactivated
        _prosumerRepo.Prosumers[nic] = new Prosumer
        {
            Nic = nic,
            FullName = "John Doe",
            Status = "Deactivated"
        };

        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-0001",
            NodeId = "ND-001",
            TotalCapacity = 4,
            AvailableCapacity = 4,
            StartTime = now.AddDays(2),
            EndTime = now.AddDays(2).AddHours(2),
            EnergyOfferedKwh = 20,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);

        var reservation = new EnergyReservation
        {
            ReservationId = "RS-UPD-01",
            Nic = nic,
            NodeId = "ND-001",
            SlotId = "SL-0001",
            SlotStartTime = slot.StartTime,
            SlotEndTime = slot.EndTime,
            RequestedKwh = 5,
            Status = "Pending",
            CreatedAt = now.AddDays(-1)
        };
        _reservationRepo.Reservations.Add(reservation);

        var updateRequest = new UpdateReservationRequest
        {
            SlotId = "SL-0001",
            RequestedKwh = 8
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.UpdateAsync("RS-UPD-01", updateRequest, nic, "Prosumer"));

        Assert.Equal("E4302", ex.Code);
    }

    [Fact]
    public async Task UpdateAsync_Rejects_WhenTargetSlotNoticeLessThan12Hours_BR06_EC2()
    {
        var now = DateTime.UtcNow;
        var currentSlot = new EnergyBookingSlot
        {
            SlotId = "SL-CURR",
            NodeId = "ND-001",
            StartTime = now.AddDays(2),
            EndTime = now.AddDays(2).AddHours(2),
            EnergyOfferedKwh = 20,
            IsActive = true
        };
        _slotRepo.Slots.Add(currentSlot);

        var targetSlotNear = new EnergyBookingSlot
        {
            SlotId = "SL-TARGET-NEAR",
            NodeId = "ND-001",
            StartTime = now.AddHours(6), // Less than 12h notice!
            EndTime = now.AddHours(8),
            EnergyOfferedKwh = 20,
            IsActive = true
        };
        _slotRepo.Slots.Add(targetSlotNear);

        var reservation = new EnergyReservation
        {
            ReservationId = "RS-0020",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-CURR",
            SlotStartTime = currentSlot.StartTime, // Current slot has 48h notice
            SlotEndTime = currentSlot.EndTime,
            RequestedKwh = 10,
            Status = "Pending",
            CreatedAt = now.AddDays(-1)
        };
        _reservationRepo.Reservations.Add(reservation);

        var request = new UpdateReservationRequest
        {
            SlotId = "SL-TARGET-NEAR",
            RequestedKwh = 10
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.UpdateAsync("RS-0020", request, "199512345678", "Prosumer"));

        Assert.Equal("E4103", ex.Code);
    }

    [Fact]
    public async Task GetMineAsync_FiltersBySearchAndDateRange()
    {
        var now = DateTime.UtcNow;
        var nic = "199512345678";

        _reservationRepo.Reservations.Add(new EnergyReservation
        {
            ReservationId = "RS-AAA",
            Nic = nic,
            NodeId = "ND-001",
            SlotStartTime = now.AddDays(1),
            CreatedAt = now.AddDays(-2)
        });
        _reservationRepo.Reservations.Add(new EnergyReservation
        {
            ReservationId = "RS-BBB",
            Nic = nic,
            NodeId = "ND-002",
            SlotStartTime = now.AddDays(3),
            CreatedAt = now.AddDays(-1)
        });

        // Search by NodeId
        var resNode = await _service.GetMineAsync(nic, null, search: "ND-002");
        Assert.Single(resNode.Items);
        Assert.Equal("RS-BBB", resNode.Items[0].ReservationId);

        // Filter by Date Range (only RS-AAA within now to now.AddDays(2))
        var resDate = await _service.GetMineAsync(nic, null, from: now, to: now.AddDays(2));
        Assert.Single(resDate.Items);
        Assert.Equal("RS-AAA", resDate.Items[0].ReservationId);
    }

    [Fact]
    public async Task UpdateAsync_AllowsBackofficeToUpdateAnyReservation()
    {
        var now = DateTime.UtcNow;
        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-0001",
            NodeId = "ND-001",
            TotalCapacity = 4,
            AvailableCapacity = 4,
            StartTime = now.AddDays(2),
            EndTime = now.AddDays(2).AddHours(2),
            EnergyOfferedKwh = 20,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);

        var reservation = new EnergyReservation
        {
            ReservationId = "RS-BO-01",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-0001",
            SlotStartTime = slot.StartTime,
            SlotEndTime = slot.EndTime,
            RequestedKwh = 5,
            Status = "Pending",
            CreatedAt = now.AddDays(-1)
        };
        _reservationRepo.Reservations.Add(reservation);

        var request = new UpdateReservationRequest
        {
            SlotId = "SL-0001",
            RequestedKwh = 12
        };

        var response = await _service.UpdateAsync("RS-BO-01", request, "USR-BACKOFFICE-01", "Backoffice");
        Assert.Equal(12, response.RequestedKwh);
    }

    [Fact]
    public async Task CancelAsync_AllowsBackofficeToCancelAnyReservation()
    {
        var now = DateTime.UtcNow;
        var reservation = new EnergyReservation
        {
            ReservationId = "RS-BO-02",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-0001",
            SlotStartTime = now.AddDays(2),
            SlotEndTime = now.AddDays(2).AddHours(2),
            RequestedKwh = 5,
            Status = "Pending",
            CreatedAt = now.AddDays(-1)
        };
        _reservationRepo.Reservations.Add(reservation);

        var response = await _service.CancelAsync("RS-BO-02", "USR-BACKOFFICE-01", "Backoffice");
        Assert.Equal("Cancelled", response.Status);
        Assert.Equal("USR-BACKOFFICE-01", response.CancelledBy);
    }

    [Fact]
    public async Task CreateOnBehalf_SucceedsForActiveProsumer()
    {
        var now = DateTime.UtcNow;
        _nodeRepo.Nodes.Add(new SolarStationInfo
        {
            NodeId = "ND-001",
            Name = "Main Grid Station",
            IsActive = true
        });

        var slot = new EnergyBookingSlot
        {
            SlotId = "SL-BEHALF-01",
            NodeId = "ND-001",
            StartTime = now.AddDays(1),
            EndTime = now.AddDays(1).AddHours(2),
            TotalCapacity = 5,
            AvailableCapacity = 3,
            EnergyOfferedKwh = 30,
            IsActive = true
        };
        _slotRepo.Slots.Add(slot);


        var request = new CreateReservationOnBehalfRequest
        {
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-BEHALF-01",
            RequestedKwh = 15.0
        };

        var response = await _service.CreateAsync(request.Nic, request);

        Assert.NotNull(response);
        Assert.Equal("199512345678", response.Nic);
        Assert.Equal("ND-001", response.NodeId);
        Assert.Equal("SL-BEHALF-01", response.SlotId);
        Assert.Equal(15.0, response.RequestedKwh);
        Assert.Equal("Pending", response.Status);
    }

    [Fact]
    public async Task GetBackofficeDashboardAsync_ReturnsAggregatedMetrics()
    {
        var now = DateTime.UtcNow;
        _reservationRepo.Reservations.Add(new EnergyReservation
        {
            ReservationId = "RS-TEST-01",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-0001",
            SlotStartTime = now.AddDays(1),
            SlotEndTime = now.AddDays(1).AddHours(2),
            RequestedKwh = 10,
            Status = "Pending"
        });
        _reservationRepo.Reservations.Add(new EnergyReservation
        {
            ReservationId = "RS-TEST-02",
            Nic = "199512345678",
            NodeId = "ND-001",
            SlotId = "SL-0002",
            SlotStartTime = now.AddDays(2),
            SlotEndTime = now.AddDays(2).AddHours(2),
            RequestedKwh = 20,
            Status = "Approved"
        });

        var dashboardService = new DashboardService(_reservationRepo);
        var result = await dashboardService.GetBackofficeDashboardAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalReservations);
        Assert.Equal(1, result.ReservationsByStatus["Pending"]);
        Assert.Equal(1, result.ReservationsByStatus["Approved"]);
    }
}


