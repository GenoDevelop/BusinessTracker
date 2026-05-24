namespace GenoDev.Utilities.Core.Time;

/// <summary>
/// Represents a time zone-aware clock that provides the current time adjusted to a specific time zone.
/// </summary>
public class TimeZoneClock
{
    /// <summary>
    /// Gets the time zone information represented by a <see cref="TimeZoneInfo"/> associated with this instance.
    /// This property provides the time zone context used for determining localized time values.
    /// </summary>
    public TimeZoneInfo TimeZone { get; }
    
    /// <summary>
    /// Provides a centralized mechanism to retrieve the current time in given time zone, with support for mocking during testing.
    /// </summary>
    public TimeZoneClock(TimeZoneInfo timeZoneInfo)
    {
        TimeZone = timeZoneInfo;
    }

    /// <summary>
    /// Gets the current UTC date and time as a <see cref="DateTimeOffset"/> representing the system's current universal time
    /// with a zero offset. This property provides the base UTC time, which can also support mocking for testing scenarios.
    /// </summary>
    public DateTimeOffset UtcNowOffset => Clock.UtcNowOffset;

    /// <summary>
    /// Gets the current date and time as a <see cref="DateTimeOffset"/> adjusted to the specified time zone.
    /// This property reflects the localized current time based on the configured time zone and supports scenarios
    /// where the time zone is fixed or controlled, including testing environments.
    /// </summary>
    public DateTimeOffset NowOffset => TimeZoneInfo.ConvertTime(UtcNowOffset, TimeZone);

    /// <summary>
    /// Gets the current UTC date and time as a <see cref="DateTime"/> representing the exact universal time
    /// without any time zone offset applied. This property is useful for scenarios requiring non-offset
    /// UTC time values and supports mocking during testing.
    /// </summary>
    public DateTime UtcNow => UtcNowOffset.DateTime;

    /// <summary>
    /// Gets the current local date and time as a <see cref="DateTime"/> in the system's effective time zone.
    /// This property reflects the current time adjusted to the active time zone and is useful in scenarios
    /// where local time representation is required.
    /// </summary>
    public DateTime Now => NowOffset.DateTime;
}