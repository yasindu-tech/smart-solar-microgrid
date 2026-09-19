// ---------------------------------------------------------------------------
//  File        : ReservationRules.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Pure domain scheduling and notice rules for reservations
//                (BR-05, BR-06, BR-07, BR-08).
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

    // BR-08: the chosen slot start time must be in the future.
    public static bool IsInFuture(DateTime nowUtc, DateTime slotStartUtc)
    {
        return slotStartUtc > nowUtc;
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

    // Guard used by create and update paths. Throws E4104 when slot start time is in the past (BR-08).
    public static void AssertInFuture(DateTime nowUtc, DateTime slotStartUtc)
    {
        if (!IsInFuture(nowUtc, slotStartUtc))
        {
            throw new DomainException(
                "E4104",
                "Slot start time must be in the future.",
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
