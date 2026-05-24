using System.Text.RegularExpressions;
using TimeZoneConverter;

namespace GenoDev.Utilities.Core.Time;

/// <summary>
/// A utility class that provides methods for parsing and handling time zone information.
/// </summary>
public static partial class TimeZoneReader
{
    /// <summary>
    /// Parses the provided time zone string and returns the corresponding <see cref="TimeZoneInfo"/> object.
    /// </summary>
    /// <param name="timeZone">A string representing the time zone. This can be in various formats such as IANA, Windows, Rails, or offset (e.g., +02:00, -0530, Z).</param>
    /// <returns>
    /// A <see cref="TimeZoneInfo"/> object representing the parsed time zone.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the provided time zone string cannot be parsed into a valid time zone.
    /// </exception>
    public static TimeZoneInfo ParseTimeZone(string timeZone) =>
        ParseTimeZone(timeZone, out _);

    /// <summary>
    /// Parses the provided time zone string and returns the corresponding <see cref="TimeZoneInfo"/> object.
    /// </summary>
    /// <param name="timeZone">A string representing the time zone. This can be in various formats such as IANA, Windows, Rails, or offset (e.g., +02:00, -0530, Z).</param>
    /// <param name="timeZoneFormat">An output parameter that will contain the detected format of the parsed time zone.</param>
    /// <returns>
    /// A <see cref="TimeZoneInfo"/> object representing the parsed time zone.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the provided time zone string cannot be parsed into a valid time zone.
    /// </exception>
    public static TimeZoneInfo ParseTimeZone(string timeZone, out TimeZoneFormat timeZoneFormat)
    {
        var success = TryParseTimeZone(timeZone, out var timeZoneInfo, out var format);

        if (!success) 
            throw new ArgumentException("Time zone could not be parsed", nameof(timeZone));
        
        timeZoneFormat = format!.Value;
        return timeZoneInfo!;
    }

    /// <summary>
    /// Attempts to parse the provided time zone string and returns a boolean indicating success or failure.
    /// </summary>
    /// <param name="timeZone">A string representing the time zone. This can be in various formats such as IANA, Windows, Rails, or offset (e.g., +02:00, -0530, Z).</param>
    /// <param name="timeZoneInfo">When this method returns, contains the <see cref="TimeZoneInfo"/> object if parsing was successful; otherwise, null.</param>
    /// <returns>
    /// True if the parsing is successful and a valid <see cref="TimeZoneInfo"/> is obtained; otherwise, false.
    /// </returns>
    public static bool TryParseTimeZone(string timeZone, out TimeZoneInfo? timeZoneInfo) =>
        TryParseTimeZone(timeZone, out timeZoneInfo, out _);

    /// <summary>
    /// Attempts to parse the provided time zone string and returns a boolean indicating whether the parsing was successful.
    /// It also returns the parsed <see cref="TimeZoneInfo"/> object and the detected <see cref="TimeZoneFormat"/> if successful.
    /// </summary>
    /// <param name="timeZone">A string representing the time zone. This can be in formats such as IANA, Windows, Rails, or offset (e.g., +02:00, -0530, Z).</param>
    /// <param name="timeZoneInfo">When this method returns, contains the <see cref="TimeZoneInfo"/> object if the parsing was successful; otherwise, null.</param>
    /// <param name="timeZoneFormat">When this method returns, contains the parsed <see cref="TimeZoneFormat"/> indicating the detected time zone format if successful; otherwise, null.</param>
    /// <returns>
    /// True if the parsing is successful and a valid <see cref="TimeZoneInfo"/> along with a detected <see cref="TimeZoneFormat"/> is obtained; otherwise, false.
    /// </returns>
    public static bool TryParseTimeZone(string timeZone, out TimeZoneInfo? timeZoneInfo,
        out TimeZoneFormat? timeZoneFormat)
    {
        var success = TryGetTimeZoneFormat(timeZone, out timeZoneFormat);
        
        if (!success)
        {
            timeZoneInfo = null;
            return false;
        }

        if (timeZoneFormat != TimeZoneFormat.Offset)
        {
            timeZoneInfo = TZConvert.GetTimeZoneInfo(ConvertToSupportedFormat(timeZone, timeZoneFormat!.Value));
            return true;
        }
        
        timeZoneInfo = ParseOffsetTimeZone(timeZone);
        return true;
    }

    /// <summary>
    /// Parses a string representation of an offset-based time zone and returns the corresponding <see cref="TimeZoneInfo"/> object.
    /// </summary>
    /// <param name="offsetTimeZone">A string representing the offset-based time zone in formats such as "+02:00", "-0530" or "Z" (for UTC).</param>
    /// <returns>
    /// A <see cref="TimeZoneInfo"/> object representing the parsed offset-based time zone. If the offset is zero or corresponds to "Z", the method returns <see cref="TimeZoneInfo.Utc"/>.
    /// </returns>
    private static TimeZoneInfo ParseOffsetTimeZone(string offsetTimeZone)
    {
        if (IsExplicitUtcOffsetTimeZone(offsetTimeZone))
            return TimeZoneInfo.Utc;

        var match = OffsetTimeZoneRegex().Match(offsetTimeZone);
        
        var hours = match.Groups["hours"].Success ? int.Parse(match.Groups["hours"].Value) : 14;
        var minutes = match.Groups["minutes"].Success ? int.Parse(match.Groups["minutes"].Value) : 0;
        var sign = match.Groups["sign"].Value == "-" ? -1 : 1;

        var offset = new TimeSpan(sign * hours, sign * minutes, 0);
        
        if (offset == TimeSpan.Zero)
            return TimeZoneInfo.Utc;

        var offsetString = $"{(sign == -1 ? "-" : "+")}{hours:00}:{minutes:00}";
        
        return TimeZoneInfo.CreateCustomTimeZone(
            $"UTC{offsetString}",
            offset,
            $"UTC{offsetString}",
            $"UTC{offsetString}"
        );
    }

    /// <summary>
    /// Attempts to determine the format of a given time zone string and provides the corresponding <see cref="TimeZoneFormat"/>.
    /// </summary>
    /// <param name="timeZone">A string representation of the time zone. This can be in formats such as IANA, Windows, Rails, or an offset format (e.g., +02:00, -0530, Z).</param>
    /// <param name="timeZoneFormat">When this method returns, contains the identified <see cref="TimeZoneFormat"/> if the operation was successful; otherwise, <c>null</c>.</param>
    /// <returns>
    /// <c>true</c> if the time zone string could be successfully mapped to a specific format; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetTimeZoneFormat(string timeZone, out TimeZoneFormat? timeZoneFormat)
    {
        if (TZConvert.TryIanaToWindows(timeZone, out _))
        {
            timeZoneFormat = TimeZoneFormat.IANA;
            return true;
        }

        if (TZConvert.TryWindowsToIana(timeZone, out _))
        {
            timeZoneFormat = TimeZoneFormat.Windows;
            return true;
        }

        if (TZConvert.TryRailsToIana(timeZone, out _))
        {
            timeZoneFormat = TimeZoneFormat.Rails;
            return true;
        }

        // Handle "Z" and offset formats
        if (IsExplicitUtcOffsetTimeZone(timeZone) || OffsetTimeZoneRegex().IsMatch(timeZone))
        {
            timeZoneFormat = TimeZoneFormat.Offset;
            return true;
        }

        timeZoneFormat = null;
        return false;
    }

    /// <summary>
    /// Determines the format of the specified time zone string and returns the corresponding <see cref="TimeZoneFormat"/>.
    /// </summary>
    /// <param name="timeZone">A string representation of the time zone. Supported formats include IANA, Windows, Rails, or an offset format (e.g., +02:00, -0530, Z).</param>
    /// <returns>
    /// A <see cref="TimeZoneFormat"/> indicating the format of the provided time zone string.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the time zone string cannot be resolved to a valid <see cref="TimeZoneFormat"/>.
    /// </exception>
    public static TimeZoneFormat GetTimeZoneFormat(string timeZone)
    {
        var success = TryGetTimeZoneFormat(timeZone, out var timeZoneFormat);

        return success
            ? timeZoneFormat!.Value
            : throw new ArgumentException("Time zone format could not be recognized", nameof(timeZone));
    }

    /// <summary>
    /// Converts the provided time zone string to a format supported by the <see cref="TimeZoneConverter.TZConvert"/> library.
    /// </summary>
    /// <param name="timeZone">A string representing the time zone in the format specified by <paramref name="timeZoneFormat"/>.</param>
    /// <param name="timeZoneFormat">The format of the input time zone string. It can be IANA, Windows, or Rails.</param>
    /// <returns>
    /// A string representing the time zone in a format compatible with IANA or Windows time zones.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="timeZoneFormat"/> is an unsupported format.
    /// </exception>
    private static string ConvertToSupportedFormat(string timeZone, TimeZoneFormat timeZoneFormat)
    {
        return timeZoneFormat switch
        {
            TimeZoneFormat.IANA or TimeZoneFormat.Windows => timeZone,
            TimeZoneFormat.Rails => TZConvert.RailsToIana(timeZone),
            _ => throw new ArgumentOutOfRangeException(nameof(timeZoneFormat), timeZoneFormat, null)
        };
    }

    [GeneratedRegex(@"^(?<sign>[+-])(?:(?<hours>(0\d|1[0-3])):?(?<minutes>[0-5]\d)?|14:?00)$", RegexOptions.Compiled)]
    private static partial Regex OffsetTimeZoneRegex();

    /// <summary>
    /// Determines whether the specified time zone string explicitly represents a UTC offset time zone.
    /// </summary>
    /// <param name="timeZone">A string representing the time zone. It can explicitly indicate UTC or Z, which are standard representations of a UTC time zone.</param>
    /// <returns>
    /// <c>true</c> if the specified time zone string explicitly represents a UTC offset time zone (e.g., "UTC" or "Z"); otherwise, <c>false</c>.
    /// </returns>
    private static bool IsExplicitUtcOffsetTimeZone(string timeZone)
    {
        return string.Equals(timeZone, "Z", StringComparison.OrdinalIgnoreCase);
    }
}