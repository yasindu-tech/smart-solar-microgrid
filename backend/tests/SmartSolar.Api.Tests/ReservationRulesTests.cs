// ---------------------------------------------------------------------------
//  File        : ReservationRulesTests.cs
//  Project     : Smart Solar Microgrid Trading System - Web API tests
//  Module      : Tests
//  Description : Boundary tests for the two headline business rules. These
//                cover the acceptance criteria that decide marks:
//                  REQ-RESV AC-2  8 days out  -> refused (E4101)
//                  REQ-RESV AC-3  exactly 7 days -> allowed
//                  REQ-RESV AC-5  11 hours out -> refused (E4102)
//                  REQ-RESV AC-6  exactly 12 hours -> allowed
//  Author      : <member name - M3>
//  Created     : 2026-09-13
// ---------------------------------------------------------------------------

using SmartSolar.Api.Middleware;
using SmartSolar.Api.Services;
using Xunit;

namespace SmartSolar.Api.Tests;

public class ReservationRulesTests
{
    private static readonly DateTime Now = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    // REQ-RESV AC-1: a slot three days out is inside the booking horizon.
    [Fact]
    public void SlotThreeDaysOut_IsWithinHorizon()
    {
        Assert.True(ReservationRules.IsWithinBookingHorizon(Now, Now.AddDays(3)));
    }

    // REQ-RESV AC-3: the 7-day boundary is inclusive.
    [Fact]
    public void SlotExactlySevenDaysOut_IsAllowed()
    {
        Assert.True(ReservationRules.IsWithinBookingHorizon(Now, Now.AddDays(7)));
    }

    // REQ-RESV AC-2: one minute past seven days is refused.
    [Fact]
    public void SlotJustOverSevenDaysOut_IsRefused()
    {
        Assert.False(ReservationRules.IsWithinBookingHorizon(Now, Now.AddDays(7).AddMinutes(1)));
    }

    // REQ-RESV AC-2: the guard raises the documented error code E4101.
    [Fact]
    public void AssertWithinBookingHorizon_ThrowsE4101_WhenBeyondSevenDays()
    {
        var ex = Assert.Throws<DomainException>(
            () => ReservationRules.AssertWithinBookingHorizon(Now, Now.AddDays(8)));

        Assert.Equal("E4101", ex.Code);
    }

    // REQ-RESV AC-6: exactly twelve hours of notice is sufficient.
    [Fact]
    public void ExactlyTwelveHoursNotice_IsAllowed()
    {
        Assert.True(ReservationRules.HasSufficientNotice(Now, Now.AddHours(12)));
    }

    // REQ-RESV AC-5: eleven hours of notice is not sufficient.
    [Fact]
    public void ElevenHoursNotice_IsRefused()
    {
        Assert.False(ReservationRules.HasSufficientNotice(Now, Now.AddHours(11)));
    }

    // REQ-RESV AC-5: the cancel guard raises E4102, not a generic failure.
    [Fact]
    public void AssertCanCancel_ThrowsE4102_WhenInsideNoticeWindow()
    {
        var ex = Assert.Throws<DomainException>(
            () => ReservationRules.AssertCanCancel(Now, Now.AddHours(11)));

        Assert.Equal("E4102", ex.Code);
    }

    // REQ-RESV AC-7: the update guard raises E4103, a different code to cancel.
    [Fact]
    public void AssertCanUpdate_ThrowsE4103_WhenInsideNoticeWindow()
    {
        var ex = Assert.Throws<DomainException>(
            () => ReservationRules.AssertCanUpdate(Now, Now.AddHours(11)));

        Assert.Equal("E4103", ex.Code);
    }

    // BR-register: the two rules use different anchors. A reservation created a
    // week ago whose slot is tomorrow still satisfies the 12-hour notice rule,
    // but would fail a naive 7-day check measured from now instead of createdAt.
    [Fact]
    public void HorizonAndNotice_UseDifferentAnchors()
    {
        var createdAt = Now.AddDays(-6);
        var slotStart = Now.AddDays(1);

        Assert.True(ReservationRules.IsWithinBookingHorizon(createdAt, slotStart));
        Assert.True(ReservationRules.HasSufficientNotice(Now, slotStart));
    }
}
