// ---------------------------------------------------------------------------
//  File        : SriLankaTimeTests.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : Guards the project's local time zone resolving to UTC+05:30
//                on every host the solution is built on - a Linux CI agent, a
//                macOS dev machine and the Windows deployment VM each resolve
//                the id by a different route.
// ---------------------------------------------------------------------------

using SmartSolar.Api.Services;
using Xunit;

namespace SmartSolar.Api.Tests;

public class SriLankaTimeTests
{
    [Fact] // Sri Lanka is a fixed +05:30 with no daylight saving
    public void Zone_ResolvesToUtcPlusFiveThirty()
    {
        // Asserted against a concrete instant rather than BaseUtcOffset so the
        // synthesised fallback zone and a real tzdata zone are both covered.
        var offset = SriLankaTime.Zone.GetUtcOffset(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(TimeSpan.FromMinutes(330), offset);
    }

    [Fact] // the conversion the schedule check depends on, stated as a worked example
    public void FromUtc_ShiftsAUtcInstantForwardByFiveAndAHalfHours()
    {
        // 03:30 UTC is the 09:00 local slot that AC-7 used to refuse.
        var local = SriLankaTime.FromUtc(new DateTime(2026, 10, 5, 3, 30, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 10, 5, 9, 0, 0), local);
    }

    [Fact] // a local wall-clock time before 05:30 belongs to the previous UTC day
    public void ToUtc_RollsAnEarlyMorningLocalTimeBackADay()
    {
        var utc = SriLankaTime.ToUtc(new DateTime(2026, 10, 5, 1, 0, 0));

        Assert.Equal(new DateTime(2026, 10, 4, 19, 30, 0, DateTimeKind.Utc), utc);
        Assert.Equal(DayOfWeek.Sunday, utc.DayOfWeek);
    }

    [Fact] // round-tripping must not drift, since both directions are used
    public void FromUtc_AndToUtc_RoundTrip()
    {
        var original = new DateTime(2026, 10, 5, 13, 30, 0, DateTimeKind.Utc);

        var roundTripped = SriLankaTime.ToUtc(SriLankaTime.FromUtc(original));

        Assert.Equal(original, roundTripped);
    }
}
