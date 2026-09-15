// ---------------------------------------------------------------------------
//  File        : ReservationRules.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Services
//  Description : The two headline time-window rules of the system, isolated as
//                pure functions so they can be unit tested at their exact
//                boundaries. See docs/business-rules/BR-register.md.
//
//                BR-05  a slot must start within 7 days of reservation CREATION
//                BR-06  updates need >= 12 hours notice before the slot start
//                BR-07  cancellations need >= 12 hours notice before slot start
//
//                BR-05 and BR-06/07 use DIFFERENT anchors. Do not merge them
//                into one helper - that is how the boundary cases go wrong.
//  Author      : <member name - M3>
//  Created     : 2026-09-13
// ---------------------------------------------------------------------------

using SmartSolar.Api.Middleware;

namespace SmartSolar.Api.Services;

public static class ReservationRules
{
    /// <summary>Booking horizon measured from the reservation's creation instant.</summary>
    public static readonly TimeSpan BookingHorizon = TimeSpan.FromDays(7);

    /// <summary>Minimum notice required before a slot starts to change or cancel it.</summary>
    public static readonly TimeSpan MinimumNotice = TimeSpan.FromHours(12);

    // BR-05: the chosen slot must begin no later than 7 days after the moment the
    // reservation was created. Boundary is inclusive - exactly 7.0 days is allowed.
    public static bool IsWithinBookingHorizon(DateTime createdAtUtc, DateTime slotStartUtc)
    {
        return slotStartUtc <= createdAtUtc + BookingHorizon;
    }

    // BR-06 / BR-07: at least 12 hours must remain between now and the slot start.
    // Boundary is inclusive - exactly 12.0 hours of notice is allowed.
    public static bool HasSufficientNotice(DateTime nowUtc, DateTime slotStartUtc)
    {
        return slotStartUtc - nowUtc >= MinimumNotice;
    }

    // Guard used by the create path. Throws E4101 when the 7-day horizon is exceeded.
    public static void AssertWithinBookingHorizon(DateTime createdAtUtc, DateTime slotStartUtc)
    {
        if (!IsWithinBookingHorizon(createdAtUtc, slotStartUtc))
        {
            throw new DomainException(
                "E4101",
                "Reservations must be scheduled within 7 days of the booking date.",
                400,
                "slotStartTime");
        }
    }

    // Guard used by the update path. Throws E4103 when less than 12 hours remain.
    public static void AssertCanUpdate(DateTime nowUtc, DateTime slotStartUtc)
    {
        if (!HasSufficientNotice(nowUtc, slotStartUtc))
        {
            throw new DomainException(
                "E4103",
                "Changes require at least 12 hours' notice before the slot start time.",
                409,
                "slotStartTime");
        }
    }

    // Guard used by the cancel path. Throws E4102 when less than 12 hours remain.
    public static void AssertCanCancel(DateTime nowUtc, DateTime slotStartUtc)
    {
        if (!HasSufficientNotice(nowUtc, slotStartUtc))
        {
            throw new DomainException(
                "E4102",
                "Cancellation requires at least 12 hours' notice before the slot start time.",
                409,
                "slotStartTime");
        }
    }
}
