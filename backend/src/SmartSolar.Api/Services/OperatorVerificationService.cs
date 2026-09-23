// ---------------------------------------------------------------------------
//  File        : OperatorVerificationService.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Server-side QR verification and energy transfer finalisation
//                for grid operators (DOM-04, REQ-QRV, ADR-003).
//                Enforces BR-10 (server-side verification only), BR-11 (operator
//                node assignment) and BR-18 (single-use QR token).
// ---------------------------------------------------------------------------

using SmartSolar.Api.DTOs.Request.Operator;
using SmartSolar.Api.DTOs.Response.Operator;
using SmartSolar.Api.DTOs.Response.Reservations;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Models;
using SmartSolar.Api.Repositories;

namespace SmartSolar.Api.Services;

public class OperatorVerificationService
{
    private readonly IReservationRepository _reservationRepo;
    private readonly IUserRepository _userRepo;
    private readonly ProsumerRepository _prosumerRepo;
    private readonly ISolarStationRepository _nodeRepo;
    private readonly QrTokenService _qrTokenService;
    private readonly ILogger<OperatorVerificationService> _logger;

    // Injects the repositories, the QR token verifier, and the logger.
    public OperatorVerificationService(
        IReservationRepository reservationRepo,
        IUserRepository userRepo,
        ProsumerRepository prosumerRepo,
        ISolarStationRepository nodeRepo,
        QrTokenService qrTokenService,
        ILogger<OperatorVerificationService> logger)
    {
        // Inject repository dependencies, token verifier, and logger.
        _reservationRepo = reservationRepo;
        _userRepo = userRepo;
        _prosumerRepo = prosumerRepo;
        _nodeRepo = nodeRepo;
        _qrTokenService = qrTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Verifies a scanned QR token entirely server-side and returns the booking facts
    /// the operator must confirm before committing. Writes nothing (REQ-QRV AC-9).
    /// </summary>
    public async Task<QrVerificationResponse> VerifyQrAsync(
        VerifyQrRequest request, string callerUserId, string callerRole)
    {
        // Run every verification check, then project a read-only view of the booking.
        var reservation = await RunVerificationChecksAsync(request.QrToken, callerUserId, callerRole);

        // Prosumer name and node name are fetched from the server, never decoded from the QR (AC-2).
        var prosumer = await _prosumerRepo.GetByNicAsync(reservation.Nic);
        var node = await _nodeRepo.GetByNodeIdAsync(reservation.NodeId);

        _logger.LogInformation(
            "Operator {OperatorId} verified QR for reservation {ReservationId} (read-only).",
            callerUserId, reservation.ReservationId);

        return new QrVerificationResponse(
            true,
            reservation.ReservationId,
            prosumer?.FullName ?? "Unknown prosumer",
            reservation.Nic,
            reservation.NodeId,
            node?.Name ?? reservation.NodeId,
            reservation.SlotStartTime,
            reservation.SlotEndTime,
            reservation.RequestedKwh,
            reservation.Status);
    }

    /// <summary>
    /// Re-runs every verification check and then atomically completes the transfer,
    /// stamping the operator, timestamp and delivered kWh, and killing the token (BR-18).
    /// </summary>
    public async Task<ReservationResponse> FinalizeAsync(
        FinalizeTransferRequest request, string callerUserId, string callerRole)
    {
        // Re-verify from scratch so a retry after a dropped network is safe (REQ-QRV EC-5).
        var now = DateTime.UtcNow;
        var presentedToken = request.QrToken?.Trim() ?? string.Empty;
        var reservation = await RunVerificationChecksAsync(presentedToken, callerUserId, callerRole);

        // The reservation the operator confirmed on screen must be the one the token names.
        if (!string.Equals(reservation.ReservationId, request.ReservationId?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new DomainException(
                "E4203", "This code does not belong to the reservation being finalised.", 409, "reservationId");

        // REQ-QRV EC-4: a delivered figure above the request is allowed and stored as-is; only negatives are refused.
        if (request.DeliveredKwh < 0)
            throw new DomainException("E4002", "Delivered kWh cannot be negative.", 400, "deliveredKwh");

        // Guard the state machine before the write, so an illegal move reports E4105 the same way everywhere.
        ReservationStateMachine.AssertTransition(reservation.Status, ReservationStateMachine.Completed);

        // Single atomic write matched on status AND stored token, so two operators scanning
        // the same QR cannot both succeed (REQ-QRV EC-6, BR-18).
        var completed = await _reservationRepo.TryCompleteAsync(
            reservation.ReservationId, presentedToken, callerUserId, request.DeliveredKwh, now);

        if (!completed)
        {
            // Lost the race or the token went stale between verify and finalise - re-read to pick the right code.
            var latest = await _reservationRepo.GetByIdAsync(reservation.ReservationId);
            if (latest == null)
                throw new DomainException("E4404", "Reservation not found.", 404, "reservationId");

            AssertReservationIsClaimable(latest, presentedToken);

            throw new DomainException(
                "E4203", "This booking could not be finalised. Ask the prosumer to refresh their QR code.", 409, "qrToken");
        }

        reservation.Status = ReservationStateMachine.Completed;
        reservation.CompletedAt = now;
        reservation.CompletedByOperatorId = callerUserId;
        reservation.DeliveredKwh = request.DeliveredKwh;
        reservation.QrToken = null;
        reservation.UpdatedAt = now;

        _logger.LogInformation(
            "Operator {OperatorId} finalised reservation {ReservationId} with {DeliveredKwh} kWh delivered.",
            callerUserId, reservation.ReservationId, request.DeliveredKwh);

        return MapToResponse(reservation);
    }

    // The single verification pipeline shared by verify-qr and finalize, in the order REQ-QRV §4 defines.
    // Every failure leaves the database untouched.
    private async Task<EnergyReservation> RunVerificationChecksAsync(
        string? presentedToken, string callerUserId, string callerRole)
    {
        // BR-10: recompute the HMAC signature first - a forged or hand-edited token dies here with E4201.
        var payload = _qrTokenService.ValidateToken(presentedToken);
        var token = presentedToken!.Trim();

        var reservation = await _reservationRepo.GetByIdAsync(payload.R);
        if (reservation == null)
            throw new DomainException("E4404", "Reservation not found.", 404, "reservationId");

        // BR-11: an operator may only act at a node in their own assignedNodeIds. Backoffice may verify anywhere.
        await AssertOperatorOwnsNodeAsync(callerUserId, callerRole, reservation.NodeId);

        // BR-10, BR-18: stored-token and status checks decide between E4202 and E4203.
        AssertReservationIsClaimable(reservation, token);

        return reservation;
    }

    // Distinguishes an already-used token (E4202) from one that is stale or not against an Approved booking (E4203).
    private static void AssertReservationIsClaimable(EnergyReservation reservation, string presentedToken)
    {
        // BR-18 / AC-4: a completed reservation's QR is spent, whatever the token now says.
        if (reservation.Status == ReservationStateMachine.Completed)
            throw new DomainException("E4202", "This code has already been used.", 409, "qrToken");

        // AC-8: cancelled or expired bookings are not claimable.
        if (reservation.Status != ReservationStateMachine.Approved)
            throw new DomainException("E4203", "This booking is not approved.", 409, "status");

        // AC-7 / ADR-003 §5: the reservation was modified after approval, so the old QR no longer matches.
        if (string.IsNullOrWhiteSpace(reservation.QrToken)
            || !string.Equals(reservation.QrToken, presentedToken, StringComparison.Ordinal))
            throw new DomainException(
                "E4203", "This code is out of date - ask the prosumer to refresh their QR code.", 409, "qrToken");
    }

    // BR-11: verifies the calling operator is assigned to the reservation's node. Backoffice is exempt.
    private async Task AssertOperatorOwnsNodeAsync(string callerUserId, string callerRole, string nodeId)
    {
        // Backoffice may verify a token anywhere; only the Operator role is node-scoped (api-contract §7).
        if (!string.Equals(callerRole, "Operator", StringComparison.Ordinal))
            return;

        var caller = await _userRepo.GetByUserIdAsync(callerUserId);
        if (caller == null || !caller.AssignedNodeIds.Contains(nodeId, StringComparer.OrdinalIgnoreCase))
            throw new DomainException("E4303", "You are not assigned to this node.", 403, "nodeId");
    }

    // Maps a domain EnergyReservation model to a response DTO.
    private static ReservationResponse MapToResponse(EnergyReservation r)
    {
        // Convert internal EnergyReservation entity into response DTO.
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
