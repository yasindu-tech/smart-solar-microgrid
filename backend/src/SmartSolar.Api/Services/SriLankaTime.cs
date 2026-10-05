// ---------------------------------------------------------------------------
//  File        : SriLankaTime.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Description : The single definition of the project's local time zone.
//                Instants are stored and compared in UTC everywhere; a node's
//                opening hours are wall-clock Sri Lanka times, so any rule
//                that compares the two has to convert first. Sri Lanka is a
//                fixed UTC+05:30 with no daylight saving.
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.Services;

public static class SriLankaTime
{
    // The Windows id is what the Azure VM resolves; the IANA id is what Linux
    // CI agents and macOS dev machines resolve. .NET 8 maps between the two on
    // most hosts, but not on a host with no ICU data, so try both before
    // falling back to the fixed +05:30 offset the zone has always had.
    public static readonly TimeZoneInfo Zone = ResolveZone();

    /// <summary>Converts a UTC instant to the Sri Lanka wall-clock time at that instant.</summary>
    public static DateTime FromUtc(DateTime utc)
        // Guard the Kind so a DateTime that arrived as Unspecified (Mongo and
        // System.Text.Json both hand those back) is not rejected by the BCL.
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    /// <summary>Converts a Sri Lanka wall-clock time to the UTC instant it names.</summary>
    public static DateTime ToUtc(DateTime local)
        // Unspecified is required here: ConvertTimeToUtc rejects a value whose
        // Kind contradicts the source zone it is being read against.
        => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    private static TimeZoneInfo ResolveZone()
    {
        // Probe the platform-specific ids in turn, then synthesise the zone so a
        // missing time-zone database degrades to a correct fixed offset rather
        // than throwing at first use and taking every slot endpoint down.
        foreach (var id in new[] { "Sri Lanka Standard Time", "Asia/Colombo" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Asia/Colombo", TimeSpan.FromMinutes(330), "Sri Lanka", "Sri Lanka");
    }
}
