// ---------------------------------------------------------------------------
//  File        : ReservationService.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Core business service orchestrating reservation workflows,
//                scheduling rules, state transitions, and capacity accounting (DOM-03).
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Request.Reservations;
using SmartSolar.Api.DTOs.Response.Common;
using SmartSolar.Api.DTOs.Response.Reservations;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class ReservationService
{
    private readonly IReservationRepository _reservationRepo;
    private readonly IEnergyBookingSlotRepository _slotRepo;
    private readonly ISolarStationRepository _nodeRepo;
    private readonly ProsumerRepository _prosumerRepo;
    private readonly ICounterRepository _counterRepo;
    private readonly QrTokenService _qrTokenService;
    private readonly ILogger<ReservationService> _logger;

    // Injects required repositories, token generator, and logger.
    public ReservationService(
        IReservationRepository reservationRepo,
        IEnergyBookingSlotRepository slotRepo,
        ISolarStationRepository nodeRepo,
        ProsumerRepository prosumerRepo,
        ICounterRepository counterRepo,
        QrTokenService qrTokenService,
        ILogger<ReservationService> logger)
    {
        // Inject repository dependencies, token generator, and logger.
        _reservationRepo = reservationRepo;
        _slotRepo = slotRepo;
        _nodeRepo = nodeRepo;
        _prosumerRepo = prosumerRepo;
        _counterRepo = counterRepo;
        _qrTokenService = qrTokenService;
        _logger = logger;
    }

    // Creates a new energy transfer reservation enforcing BR-17, BR-16, BR-08, BR-05, and BR-15.
    public async Task<ReservationResponse> CreateAsync(string callerNic, CreateReservationRequest request)
    {
        var now = DateTime.UtcNow;

        // BR-17: Caller prosumer must exist and have status "Active".
        var prosumer = await _prosumerRepo.GetByNicAsync(callerNic);
        if (prosumer == null)
            throw new DomainException("E4404", "Prosumer account not found.", 404, "nic");

        if (prosumer.Status != "Active")
            throw new DomainException("E4302", "Your prosumer account is not active.", 403, "status");

        // BR-16, BR-20: Target microgrid node must exist and be Active.
        var node = await _nodeRepo.GetByNodeIdAsync(request.NodeId);
        if (node == null || !node.IsActive)
            throw new DomainException("E4404", "Target microgrid node not found or is currently inactive.", 404, "nodeId");

        // BR-16: Target slot must exist, be active, and belong to the specified node.
        var slot = await _slotRepo.GetBySlotIdAsync(request.SlotId);
        if (slot == null || !slot.IsActive || !string.Equals(slot.NodeId, request.NodeId, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("E4404", "Booking slot not found or inactive for this node.", 404, "slotId");

        // BR-08: Chosen slot start time must be in the future.
        ReservationRules.AssertInFuture(now, slot.StartTime);

        // BR-05: Chosen slot must begin within 7 days of the booking date.
        ReservationRules.AssertWithinBookingHorizon(now, slot.StartTime);

        // Capacity bounds check on requested energy quota.
        if (request.RequestedKwh <= 0 || request.RequestedKwh > slot.EnergyOfferedKwh)
            throw new DomainException("E4002", $"Requested kWh must be greater than 0 and at most {slot.EnergyOfferedKwh} kWh.", 400, "requestedKwh");

        // BR-15: Slot must have available reservation capacity.
        if (slot.AvailableCapacity <= 0)
            throw new DomainException("E4106", "That energy slot has reached maximum capacity.", 409, "slotId");

        // Atomically increment sequential reservation identifier, e.g. "RS-0006".
        var nextSeq = await _counterRepo.GetNextSequenceValueAsync("reservationid");
        var reservationId = $"RS-{nextSeq:D4}";

        var reservation = new EnergyReservation
        {
            ReservationId = reservationId,
            Nic = callerNic,
            NodeId = request.NodeId,
            SlotId = request.SlotId,
            SlotStartTime = slot.StartTime,
            SlotEndTime = slot.EndTime,
            RequestedKwh = request.RequestedKwh,
            Status = ReservationStateMachine.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _reservationRepo.CreateAsync(reservation);
        _logger.LogInformation("Created reservation {ReservationId} for prosumer {Nic} on slot {SlotId}.", reservationId, callerNic, slot.SlotId);

        return MapToResponse(reservation);
    }

    // Returns a paged list of reservations matching staff filters.
    public async Task<PagedResponse<ReservationResponse>> GetAllAsync(
        string? status, string? nodeId, DateTime? from, DateTime? to, string? search, int page, int pageSize)
    {
        // Query paginated reservations matching operational filter criteria.
        var (items, total) = await _reservationRepo.GetAllAsync(status, nodeId, from, to, search, page, pageSize);
        var dtos = items.Select(MapToResponse).ToList();
        return new PagedResponse<ReservationResponse>(dtos, total, page, pageSize);
    }

    // Returns a paged list of reservations owned by the calling prosumer.
    public async Task<PagedResponse<ReservationResponse>> GetMineAsync(
        string callerNic, string? status, int page, int pageSize)
    {
        // Query paginated reservations belonging to the calling prosumer.
        var (items, total) = await _reservationRepo.GetMineAsync(callerNic, status, page, pageSize);
        var dtos = items.Select(MapToResponse).ToList();
        return new PagedResponse<ReservationResponse>(dtos, total, page, pageSize);
    }

    // Retrieves a single reservation by ID enforcing ownership/role rules (BR-02).
    public async Task<ReservationResponse> GetByIdAsync(string reservationId, string callerSubject, string callerRole)
    {
        // Retrieve reservation document by identifier.
        var reservation = await _reservationRepo.GetByIdAsync(reservationId);
        if (reservation == null)
            throw new DomainException("E4404", "Reservation not found.", 404);

        // BR-02: Prosumers can only view their own reservations.
        if (callerRole == "Prosumer" && !string.Equals(reservation.Nic, callerSubject, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("E4304", "You do not have permission to view this reservation.", 403);

        return MapToResponse(reservation);
    }

    // Modifies an existing reservation enforcing BR-02, BR-06 (12h notice), BR-05, and BR-18 (QR reissue).
    public async Task<ReservationResponse> UpdateAsync(
        string reservationId, UpdateReservationRequest request, string callerSubject, string callerRole)
    {
        // Enforce 12-hour modification notice, 7-day horizon, and update reservation schedule.
        var now = DateTime.UtcNow;

        var reservation = await _reservationRepo.GetByIdAsync(reservationId);
        if (reservation == null)
            throw new DomainException("E4404", "Reservation not found.", 404);

        // BR-02: Prosumers can only modify their own reservations.
        if (callerRole == "Prosumer" && !string.Equals(reservation.Nic, callerSubject, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("E4304", "You do not have permission to modify this reservation.", 403);

        // Terminal status check: Only Pending or Approved reservations may be modified.
        if (reservation.Status != ReservationStateMachine.Pending && reservation.Status != ReservationStateMachine.Approved)
            throw new DomainException("E4105", $"Cannot modify a reservation with status '{reservation.Status}'.", 409, "status");

        // BR-06: Changes require at least 12 hours' advance notice before current slot start.
        ReservationRules.AssertCanUpdate(now, reservation.SlotStartTime);

        // Validate target slot.
        var targetSlot = await _slotRepo.GetBySlotIdAsync(request.SlotId);
        if (targetSlot == null || !targetSlot.IsActive)
            throw new DomainException("E4404", "Target booking slot not found or inactive.", 404, "slotId");

        if (!string.Equals(targetSlot.NodeId, reservation.NodeId, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("E4000", "Target slot must belong to the same microgrid node.", 400, "slotId");

        ReservationRules.AssertInFuture(now, targetSlot.StartTime);

        // BR-05: Target slot start must remain within 7 days of the ORIGINAL booking creation timestamp.
        ReservationRules.AssertWithinBookingHorizon(reservation.CreatedAt, targetSlot.StartTime);

        if (request.RequestedKwh <= 0 || request.RequestedKwh > targetSlot.EnergyOfferedKwh)
            throw new DomainException("E4002", $"Requested kWh must be greater than 0 and at most {targetSlot.EnergyOfferedKwh} kWh.", 400, "requestedKwh");

        var slotChanged = !string.Equals(reservation.SlotId, request.SlotId, StringComparison.OrdinalIgnoreCase);

        // Handle capacity handoff and QR token regeneration if already approved.
        if (slotChanged && reservation.Status == ReservationStateMachine.Approved)
        {
            // Atomically decrement capacity on the new slot.
            var capacitySecured = await _slotRepo.TryDecrementCapacityAsync(targetSlot.SlotId);
            if (!capacitySecured)
                throw new DomainException("E4106", "Target energy slot has reached maximum capacity.", 409, "slotId");

            // Restore capacity to the former slot.
            await _slotRepo.IncrementCapacityAsync(reservation.SlotId);

            // BR-18 / ADR-003: Re-issue a new QR token so the previous QR code is invalidated.
            reservation.QrToken = _qrTokenService.GenerateToken(reservation.ReservationId);
            reservation.QrIssuedAt = now;
        }

        reservation.SlotId = targetSlot.SlotId;
        reservation.SlotStartTime = targetSlot.StartTime;
        reservation.SlotEndTime = targetSlot.EndTime;
        reservation.RequestedKwh = request.RequestedKwh;
        reservation.UpdatedAt = now;

        await _reservationRepo.UpdateAsync(reservation);
        _logger.LogInformation("Updated reservation {ReservationId} (Slot: {SlotId}, kWh: {Kwh}).", reservationId, reservation.SlotId, reservation.RequestedKwh);

        return MapToResponse(reservation);
    }

    // Approves a pending reservation, decrements slot capacity (BR-15), and issues a secure QR token (ADR-003).
    public async Task<ReservationResponse> ApproveAsync(string reservationId)
    {
        // Approve pending reservation, decrement slot capacity atomically, and issue signed QR token.
        var now = DateTime.UtcNow;

        var reservation = await _reservationRepo.GetByIdAsync(reservationId);
        if (reservation == null)
            throw new DomainException("E4404", "Reservation not found.", 404);

        // Validate state transition Pending -> Approved.
        ReservationStateMachine.AssertTransition(reservation.Status, ReservationStateMachine.Approved);

        // BR-15: Atomically decrement available slot capacity.
        var decremented = await _slotRepo.TryDecrementCapacityAsync(reservation.SlotId);
        if (!decremented)
            throw new DomainException("E4106", "Cannot approve reservation: the slot has no remaining capacity.", 409, "slotId");

        reservation.Status = ReservationStateMachine.Approved;
        reservation.QrToken = _qrTokenService.GenerateToken(reservation.ReservationId);
        reservation.QrIssuedAt = now;
        reservation.UpdatedAt = now;

        await _reservationRepo.UpdateAsync(reservation);
        _logger.LogInformation("Approved reservation {ReservationId} and issued QR token.", reservationId);

        return MapToResponse(reservation);
    }

    // Cancels a reservation enforcing BR-02, BR-07 (12h notice), and capacity restoration (BR-15).
    public async Task<ReservationResponse> CancelAsync(string reservationId, string callerSubject, string callerRole)
    {
        // Cancel reservation, enforce 12-hour notice, and restore capacity if previously approved.
        var now = DateTime.UtcNow;

        var reservation = await _reservationRepo.GetByIdAsync(reservationId);
        if (reservation == null)
            throw new DomainException("E4404", "Reservation not found.", 404);

        // BR-02: Prosumers can only cancel their own reservations.
        if (callerRole == "Prosumer" && !string.Equals(reservation.Nic, callerSubject, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("E4304", "You do not have permission to cancel this reservation.", 403);

        // Assert valid state transition to Cancelled.
        ReservationStateMachine.AssertTransition(reservation.Status, ReservationStateMachine.Cancelled);

        // BR-07: Cancellation requires at least 12 hours' notice before slot start.
        ReservationRules.AssertCanCancel(now, reservation.SlotStartTime);

        // BR-15: If the reservation was previously Approved, restore slot capacity (+1).
        if (reservation.Status == ReservationStateMachine.Approved)
        {
            await _slotRepo.IncrementCapacityAsync(reservation.SlotId);
        }

        reservation.Status = ReservationStateMachine.Cancelled;
        reservation.CancelledAt = now;
        reservation.CancelledBy = callerSubject;
        reservation.QrToken = null; // Invalidate QR on cancellation
        reservation.UpdatedAt = now;

        await _reservationRepo.UpdateAsync(reservation);
        _logger.LogInformation("Cancelled reservation {ReservationId} by {CancelledBy}.", reservationId, callerSubject);

        return MapToResponse(reservation);
    }

    // Retrieves the signed QR token and expiry window for an approved reservation.
    public async Task<QrTokenResponse> GetQrAsync(string reservationId, string callerSubject, string callerRole)
    {
        // Validate caller permissions and return cryptographic QR token for approved reservation.
        var reservation = await _reservationRepo.GetByIdAsync(reservationId);
        if (reservation == null)
            throw new DomainException("E4404", "Reservation not found.", 404);

        // BR-02: Prosumers can only fetch their own QR code.
        if (callerRole == "Prosumer" && !string.Equals(reservation.Nic, callerSubject, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("E4304", "You do not have permission to view this QR code.", 403);

        if (reservation.Status != ReservationStateMachine.Approved || string.IsNullOrWhiteSpace(reservation.QrToken))
            throw new DomainException("E4201", "QR code is only available for Approved reservations.", 400, "status");

        return new QrTokenResponse(reservation.QrToken, reservation.SlotEndTime);
    }

    // Maps a domain EnergyReservation model to a response DTO.
    private static ReservationResponse MapToResponse(EnergyReservation r)
    {
        // Map EnergyReservation model to API response DTO.
        return new ReservationResponse(
            r.ReservationId,
            r.Nic,
            r.NodeId,
            r.SlotId,
            r.SlotStartTime,
            r.SlotEndTime,
            r.RequestedKwh,
            r.Status,
            r.QrToken,
            r.QrIssuedAt,
            r.CreatedAt,
            r.UpdatedAt,
            r.CancelledAt,
            r.CancelledBy,
            r.CompletedAt,
            r.CompletedByOperatorId,
            r.DeliveredKwh
        );
    }
}
