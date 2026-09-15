// ---------------------------------------------------------------------------
//  File        : ReservationStateMachineTests.cs
//  Project     : Smart Solar Microgrid Trading System - Web API tests
//  Module      : Tests
//  Description : Confirms terminal statuses are terminal, per
//                docs/architecture/state-machines.md section 1.
//  Author      : <member name - M3>
//  Created     : 2026-09-13
// ---------------------------------------------------------------------------

using SmartSolar.Api.Middleware;
using SmartSolar.Api.Services;
using Xunit;

namespace SmartSolar.Api.Tests;

public class ReservationStateMachineTests
{
    // A pending reservation may be approved by an operator.
    [Fact]
    public void PendingToApproved_IsAllowed()
    {
        Assert.True(ReservationStateMachine.CanTransition("Pending", "Approved"));
    }

    // REQ-RESV AC-10: a cancelled reservation is terminal.
    [Theory]
    [InlineData("Cancelled", "Approved")]
    [InlineData("Cancelled", "Pending")]
    [InlineData("Completed", "Cancelled")]
    [InlineData("Expired", "Approved")]
    public void TerminalStatuses_CannotTransition(string from, string to)
    {
        Assert.False(ReservationStateMachine.CanTransition(from, to));
    }

    // REQ-RESV AC-10: the guard raises the documented code E4105.
    [Fact]
    public void AssertTransition_ThrowsE4105_OnTerminalStatus()
    {
        var ex = Assert.Throws<DomainException>(
            () => ReservationStateMachine.AssertTransition("Cancelled", "Approved"));

        Assert.Equal("E4105", ex.Code);
    }

    // An approved reservation is completed only after QR verification (REQ-QRV).
    [Fact]
    public void ApprovedToCompleted_IsAllowed()
    {
        Assert.True(ReservationStateMachine.CanTransition("Approved", "Completed"));
    }
}
