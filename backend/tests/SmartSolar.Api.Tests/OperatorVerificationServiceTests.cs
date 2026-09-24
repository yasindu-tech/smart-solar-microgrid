// ---------------------------------------------------------------------------
//  File        : OperatorVerificationServiceTests.cs
//  Project     : Smart Solar Microgrid Trading System - Web API tests
//  Description : Unit tests for OperatorVerificationService and the operator
//                dashboard (DOM-04): BR-10 server-side verification, BR-11 node
//                assignment, BR-18 single-use tokens, and REQ-QRV AC-1..AC-9.
// ---------------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartSolar.Api.Configuration;
using SmartSolar.Api.DTOs.Request.Operator;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Services;
using Xunit;

namespace SmartSolar.Api.Tests;

public class OperatorVerificationServiceTests
{
    private const string NodeA = "ND-001";
    private const string NodeB = "ND-002";
    private const string OperatorA = "USR-0002";
    private const string OperatorB = "USR-0003";
    private const string Nic = "199512345678";

    private readonly FakeReservationRepository _reservations = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeProsumerRepository _prosumers = new();
    private readonly FakeSolarStationRepository _nodes = new();
    private readonly QrTokenService _qrService =
        new(Options.Create(new QrSettings { SigningSecret = "test-secret-key-that-is-long-enough-32-chars!" }));

    private readonly OperatorVerificationService _service;

    public OperatorVerificationServiceTests()
    {
        // Two operators at two different hubs - the fixture DOM-04's Definition of Done asks for.
        _users.Users.Add(new User { UserId = OperatorA, Role = "Operator", AssignedNodeIds = new List<string> { NodeA } });
        _users.Users.Add(new User { UserId = OperatorB, Role = "Operator", AssignedNodeIds = new List<string> { NodeB } });

        _prosumers.Prosumers[Nic] = new Prosumer { Nic = Nic, FullName = "John Doe", Status = "Active" };

        _nodes.Nodes.Add(new SolarStationInfo { NodeId = NodeA, Name = "Moratuwa Hub A", IsActive = true });
        _nodes.Nodes.Add(new SolarStationInfo { NodeId = NodeB, Name = "Panadura Hub B", IsActive = true });

        _service = new OperatorVerificationService(
            _reservations,
            _users,
            _prosumers,
            _nodes,
            _qrService,
            NullLogger<OperatorVerificationService>.Instance);
    }

    // Seeds an Approved reservation carrying a freshly issued, valid QR token.
    private EnergyReservation SeedApproved(string reservationId = "RS-0001", string nodeId = NodeA)
    {
        var now = DateTime.UtcNow;
        var reservation = new EnergyReservation
        {
            ReservationId = reservationId,
            Nic = Nic,
            NodeId = nodeId,
            SlotId = "SL-0001",
            SlotStartTime = now.AddHours(20),
            SlotEndTime = now.AddHours(22),
            RequestedKwh = 12.5,
            Status = ReservationStateMachine.Approved,
            QrToken = _qrService.GenerateToken(reservationId),
            QrIssuedAt = now,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now
        };

        _reservations.Reservations.Add(reservation);
        return reservation;
    }

    // ---------------------------------------------------------------------
    //  AC-2 / BR-10 — a valid scan returns server-held booking facts
    // ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyQrAsync_ReturnsServerHeldBookingDetails_ForValidToken()
    {
        var reservation = SeedApproved();

        var response = await _service.VerifyQrAsync(
            new VerifyQrRequest(reservation.QrToken!), OperatorA, "Operator");

        Assert.True(response.Valid);
        Assert.Equal("RS-0001", response.ReservationId);
        Assert.Equal("John Doe", response.ProsumerName);
        Assert.Equal(Nic, response.Nic);
        Assert.Equal(NodeA, response.NodeId);
        Assert.Equal("Moratuwa Hub A", response.NodeName);
        Assert.Equal(12.5, response.RequestedKwh);
        Assert.Equal(ReservationStateMachine.Approved, response.Status);
    }

    // ---------------------------------------------------------------------
    //  AC-9 — verification is read-only
    // ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyQrAsync_IsReadOnly_WhenCalledTwice()
    {
        var reservation = SeedApproved();
        var tokenBefore = reservation.QrToken;
        var updatedAtBefore = reservation.UpdatedAt;

        var first = await _service.VerifyQrAsync(new VerifyQrRequest(tokenBefore!), OperatorA, "Operator");
        var second = await _service.VerifyQrAsync(new VerifyQrRequest(tokenBefore!), OperatorA, "Operator");

        // Same answer both times, and nothing about the stored document moved.
        Assert.Equal(first, second);

        var stored = _reservations.Reservations.Single(r => r.ReservationId == "RS-0001");
        Assert.Equal(ReservationStateMachine.Approved, stored.Status);
        Assert.Equal(tokenBefore, stored.QrToken);
        Assert.Equal(updatedAtBefore, stored.UpdatedAt);
        Assert.Null(stored.CompletedAt);
        Assert.Null(stored.CompletedByOperatorId);
        Assert.Null(stored.DeliveredKwh);
    }

    // ---------------------------------------------------------------------
    //  AC-5 / E4201 — tampered, forged and malformed tokens
    // ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyQrAsync_ThrowsE4201_WhenOneCharacterOfThePayloadIsEdited()
    {
        var reservation = SeedApproved();
        var token = reservation.QrToken!;

        // Flip a single character of the payload segment - the cheapest security demo in the project.
        var payload = token.Split('.')[0];
        var signature = token.Split('.')[1];
        var edited = payload[..^1] + (payload[^1] == 'A' ? 'B' : 'A') + "." + signature;

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.VerifyQrAsync(new VerifyQrRequest(edited), OperatorA, "Operator"));

        Assert.Equal("E4201", ex.Code);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task VerifyQrAsync_ThrowsE4201_WhenSignedWithADifferentSecret()
    {
        var reservation = SeedApproved();

        // A token the attacker signed themselves: right shape, wrong key.
        var foreign = new QrTokenService(Options.Create(
            new QrSettings { SigningSecret = "a-totally-different-attacker-secret-value" }));
        var forged = foreign.GenerateToken(reservation.ReservationId);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.VerifyQrAsync(new VerifyQrRequest(forged), OperatorA, "Operator"));

        Assert.Equal("E4201", ex.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-token")]
    [InlineData("no-signature.")]
    [InlineData(".no-payload")]
    [InlineData("too.many.dots")]
    [InlineData("!!!not-base64!!!.!!!also-not!!!")]
    public async Task VerifyQrAsync_ThrowsE4201_ForMalformedTokens_NeverCrashes(string token)
    {
        SeedApproved();

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.VerifyQrAsync(new VerifyQrRequest(token), OperatorA, "Operator"));

        Assert.Equal("E4201", ex.Code);
    }

    // ---------------------------------------------------------------------
    //  AC-4 / BR-18 — single use
    // ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyQrAsync_ThrowsE4202_WhenReservationIsAlreadyCompleted()
    {
        var reservation = SeedApproved();
        var token = reservation.QrToken!;

        await _service.FinalizeAsync(
            new FinalizeTransferRequest(token, reservation.ReservationId, 11.8), OperatorA, "Operator");

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.VerifyQrAsync(new VerifyQrRequest(token), OperatorA, "Operator"));

        Assert.Equal("E4202", ex.Code);
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task FinalizeAsync_ThrowsE4202_OnSecondScan_AndLeavesTheFirstResultIntact()
    {
        var reservation = SeedApproved();
        var token = reservation.QrToken!;

        await _service.FinalizeAsync(
            new FinalizeTransferRequest(token, reservation.ReservationId, 11.8), OperatorA, "Operator");

        // EC-6: a second operator scanning the same QR seconds later must lose.
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.FinalizeAsync(
                new FinalizeTransferRequest(token, reservation.ReservationId, 99.0), OperatorA, "Operator"));

        Assert.Equal("E4202", ex.Code);

        var stored = _reservations.Reservations.Single(r => r.ReservationId == "RS-0001");
        Assert.Equal(11.8, stored.DeliveredKwh);
        Assert.Equal(OperatorA, stored.CompletedByOperatorId);
    }

    // ---------------------------------------------------------------------
    //  AC-3 — successful finalisation
    // ---------------------------------------------------------------------

    [Fact]
    public async Task FinalizeAsync_CompletesTransfer_StampsOperatorAndKillsToken()
    {
        var reservation = SeedApproved();
        var token = reservation.QrToken!;
        var before = DateTime.UtcNow;

        var response = await _service.FinalizeAsync(
            new FinalizeTransferRequest(token, reservation.ReservationId, 12.0), OperatorA, "Operator");

        Assert.Equal(ReservationStateMachine.Completed, response.Status);
        Assert.Equal(OperatorA, response.CompletedByOperatorId);
        Assert.Equal(12.0, response.DeliveredKwh);
        Assert.NotNull(response.CompletedAt);
        Assert.True(response.CompletedAt >= before);
        Assert.Null(response.QrToken);

        var stored = _reservations.Reservations.Single(r => r.ReservationId == "RS-0001");
        Assert.Equal(ReservationStateMachine.Completed, stored.Status);
        Assert.Null(stored.QrToken);
    }

    // ---------------------------------------------------------------------
    //  AC-6 / BR-11 — node assignment
    // ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyQrAsync_ThrowsE4303_WhenOperatorIsNotAssignedToTheNode()
    {
        // Booking is at ND-002; the caller is only assigned to ND-001.
        var reservation = SeedApproved("RS-0002", NodeB);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.VerifyQrAsync(new VerifyQrRequest(reservation.QrToken!), OperatorA, "Operator"));

        Assert.Equal("E4303", ex.Code);
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task FinalizeAsync_ThrowsE4303_AndWritesNothing_WhenOperatorIsNotAssignedToTheNode()
    {
        var reservation = SeedApproved("RS-0002", NodeB);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.FinalizeAsync(
                new FinalizeTransferRequest(reservation.QrToken!, "RS-0002", 5.0), OperatorA, "Operator"));

        Assert.Equal("E4303", ex.Code);

        var stored = _reservations.Reservations.Single(r => r.ReservationId == "RS-0002");
        Assert.Equal(ReservationStateMachine.Approved, stored.Status);
        Assert.Null(stored.CompletedAt);
    }

    [Fact]
    public async Task VerifyQrAsync_Succeeds_ForTheOperatorAssignedToThatNode()
    {
        var reservation = SeedApproved("RS-0002", NodeB);

        var response = await _service.VerifyQrAsync(
            new VerifyQrRequest(reservation.QrToken!), OperatorB, "Operator");

        Assert.True(response.Valid);
        Assert.Equal(NodeB, response.NodeId);
    }

    // ---------------------------------------------------------------------
    //  AC-7 / AC-8 / E4203 — stale token and non-approved status
    // ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyQrAsync_ThrowsE4203_WhenTheStoredTokenWasReissued()
    {
        var reservation = SeedApproved();
        var oldToken = reservation.QrToken!;

        // ADR-003 §5: modifying an Approved reservation re-issues the token; the screenshot goes stale.
        reservation.QrToken = _qrService.GenerateToken(reservation.ReservationId);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.VerifyQrAsync(new VerifyQrRequest(oldToken), OperatorA, "Operator"));

        Assert.Equal("E4203", ex.Code);
    }

    [Theory]
    [InlineData(ReservationStateMachine.Pending)]
    [InlineData(ReservationStateMachine.Cancelled)]
    [InlineData(ReservationStateMachine.Expired)]
    public async Task VerifyQrAsync_ThrowsE4203_WhenReservationIsNotApproved(string status)
    {
        var reservation = SeedApproved();
        var token = reservation.QrToken!;
        reservation.Status = status;

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.VerifyQrAsync(new VerifyQrRequest(token), OperatorA, "Operator"));

        Assert.Equal("E4203", ex.Code);
    }

    [Fact]
    public async Task FinalizeAsync_ThrowsE4203_WhenTheConfirmedReservationIdDoesNotMatchTheToken()
    {
        var reservation = SeedApproved();
        SeedApproved("RS-0009");

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.FinalizeAsync(
                new FinalizeTransferRequest(reservation.QrToken!, "RS-0009", 5.0), OperatorA, "Operator"));

        Assert.Equal("E4203", ex.Code);
    }

    // ---------------------------------------------------------------------
    //  E4404 — token signed by us, but the reservation is gone
    // ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyQrAsync_ThrowsE4404_WhenTheReservationDoesNotExist()
    {
        var orphanToken = _qrService.GenerateToken("RS-9999");

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.VerifyQrAsync(new VerifyQrRequest(orphanToken), OperatorA, "Operator"));

        Assert.Equal("E4404", ex.Code);
        Assert.Equal(404, ex.StatusCode);
    }

    // ---------------------------------------------------------------------
    //  EC-4 / EC-5 — delivered kWh and retry safety
    // ---------------------------------------------------------------------

    [Fact]
    public async Task FinalizeAsync_AllowsDeliveredKwhAboveRequested_StoredAsIs()
    {
        var reservation = SeedApproved();

        // EC-4: over-delivery is flagged in the UI, not blocked by the API.
        var response = await _service.FinalizeAsync(
            new FinalizeTransferRequest(reservation.QrToken!, reservation.ReservationId, 20.0),
            OperatorA, "Operator");

        Assert.Equal(20.0, response.DeliveredKwh);
    }

    [Fact]
    public async Task FinalizeAsync_ThrowsE4002_WhenDeliveredKwhIsNegative()
    {
        var reservation = SeedApproved();

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.FinalizeAsync(
                new FinalizeTransferRequest(reservation.QrToken!, reservation.ReservationId, -1.0),
                OperatorA, "Operator"));

        Assert.Equal("E4002", ex.Code);

        var stored = _reservations.Reservations.Single(r => r.ReservationId == "RS-0001");
        Assert.Equal(ReservationStateMachine.Approved, stored.Status);
    }

    [Fact]
    public async Task FinalizeAsync_ReRunsEveryCheck_SoItDoesNotDependOnAPriorVerifyCall()
    {
        // EC-5: the network can drop between verify and finalize; finalize must stand alone.
        var reservation = SeedApproved();

        var response = await _service.FinalizeAsync(
            new FinalizeTransferRequest(reservation.QrToken!, reservation.ReservationId, 9.0),
            OperatorA, "Operator");

        Assert.Equal(ReservationStateMachine.Completed, response.Status);
    }

    // ---------------------------------------------------------------------
    //  Backoffice may verify anywhere (api-contract §7), but not finalise
    // ---------------------------------------------------------------------

    [Fact]
    public async Task VerifyQrAsync_AllowsBackoffice_AtAnyNode()
    {
        var reservation = SeedApproved("RS-0002", NodeB);

        var response = await _service.VerifyQrAsync(
            new VerifyQrRequest(reservation.QrToken!), "USR-0001", "Backoffice");

        Assert.True(response.Valid);
    }
}

public class OperatorDashboardTests
{
    private const string NodeA = "ND-001";
    private const string NodeB = "ND-002";
    private const string OperatorA = "USR-0002";
    private const string OperatorB = "USR-0003";

    private readonly FakeReservationRepository _reservations = new();
    private readonly FakeUserRepository _users = new();
    private readonly DashboardService _service;

    public OperatorDashboardTests()
    {
        // Two operators, two different hubs - exactly the test DOM-04's Definition of Done asks for.
        _users.Users.Add(new User { UserId = OperatorA, Role = "Operator", AssignedNodeIds = new List<string> { NodeA } });
        _users.Users.Add(new User { UserId = OperatorB, Role = "Operator", AssignedNodeIds = new List<string> { NodeB } });

        _service = new DashboardService(_reservations, new FakeProsumerRepository(), new FakeSolarStationRepository(), _users);
    }

    // Adds one reservation to the in-memory store.
    private void Seed(string reservationId, string nodeId, string status, DateTime slotStart)
    {
        _reservations.Reservations.Add(new EnergyReservation
        {
            ReservationId = reservationId,
            Nic = "199512345678",
            NodeId = nodeId,
            SlotId = "SL-0001",
            SlotStartTime = slotStart,
            SlotEndTime = slotStart.AddHours(2),
            RequestedKwh = 10,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
    }

    [Fact]
    public async Task GetOperatorDashboardAsync_ScopesCountsToTheOperatorsOwnNodes()
    {
        var now = DateTime.UtcNow;

        Seed("RS-0001", NodeA, ReservationStateMachine.Pending, now.AddDays(2));
        Seed("RS-0002", NodeA, ReservationStateMachine.Approved, now.AddDays(2));
        Seed("RS-0003", NodeB, ReservationStateMachine.Pending, now.AddDays(2));
        Seed("RS-0004", NodeB, ReservationStateMachine.Approved, now.AddDays(2));
        Seed("RS-0005", NodeB, ReservationStateMachine.Approved, now.AddDays(3));

        var a = await _service.GetOperatorDashboardAsync(OperatorA, null);
        var b = await _service.GetOperatorDashboardAsync(OperatorB, null);

        Assert.Equal(1, a.PendingCount);
        Assert.Equal(1, a.ApprovedFutureCount);
        Assert.Equal(new[] { NodeA }, a.ScopedNodeIds);

        Assert.Equal(1, b.PendingCount);
        Assert.Equal(2, b.ApprovedFutureCount);
        Assert.Equal(new[] { NodeB }, b.ScopedNodeIds);
    }

    [Fact]
    public async Task GetOperatorDashboardAsync_ExcludesPastSlotsFromTheApprovedFutureCount()
    {
        var now = DateTime.UtcNow;

        Seed("RS-0001", NodeA, ReservationStateMachine.Approved, now.AddHours(-5));
        Seed("RS-0002", NodeA, ReservationStateMachine.Approved, now.AddHours(5));

        var response = await _service.GetOperatorDashboardAsync(OperatorA, null);

        Assert.Equal(1, response.ApprovedFutureCount);
    }

    [Fact]
    public async Task GetOperatorDashboardAsync_ReturnsOnlyTodaysReservations_SortedBySlotStart()
    {
        // Anchor mid-day UTC so "today" cannot straddle a boundary while the test runs.
        var today = DateTime.UtcNow.Date.AddHours(12);

        Seed("RS-LATE", NodeA, ReservationStateMachine.Approved, today.AddHours(4));
        Seed("RS-EARLY", NodeA, ReservationStateMachine.Approved, today.AddHours(-6));
        Seed("RS-TOMORROW", NodeA, ReservationStateMachine.Approved, today.AddDays(1));
        Seed("RS-OTHERNODE", NodeB, ReservationStateMachine.Approved, today);

        var response = await _service.GetOperatorDashboardAsync(OperatorA, null);

        Assert.Equal(new[] { "RS-EARLY", "RS-LATE" }, response.TodaysReservations.Select(r => r.ReservationId));
    }

    [Fact]
    public async Task GetOperatorDashboardAsync_ThrowsE4303_WhenNarrowingToAnUnassignedNode()
    {
        Seed("RS-0001", NodeB, ReservationStateMachine.Pending, DateTime.UtcNow.AddDays(1));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.GetOperatorDashboardAsync(OperatorA, NodeB));

        Assert.Equal("E4303", ex.Code);
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task GetOperatorDashboardAsync_ReturnsZeroes_WhenOperatorHasNoAssignedNodes()
    {
        _users.Users.Add(new User { UserId = "USR-0099", Role = "Operator", AssignedNodeIds = new List<string>() });
        Seed("RS-0001", NodeA, ReservationStateMachine.Pending, DateTime.UtcNow.AddDays(1));

        var response = await _service.GetOperatorDashboardAsync("USR-0099", null);

        Assert.Equal(0, response.PendingCount);
        Assert.Equal(0, response.ApprovedFutureCount);
        Assert.Empty(response.TodaysReservations);
    }

    [Fact]
    public async Task GetOperatorDashboardAsync_ThrowsE4404_WhenTheOperatorAccountIsMissing()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.GetOperatorDashboardAsync("USR-NOPE", null));

        Assert.Equal("E4404", ex.Code);
    }
}
