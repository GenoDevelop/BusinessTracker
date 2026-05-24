namespace GenoDev.Utilities.Core.Time;

/// <summary>
/// Provides a centralized mechanism to retrieve the current time, with support for mocking during testing.
/// </summary>
public partial class Clock
{
    /// <summary>
    /// A thread-local storage for the custom time zone used by the <see cref="Clock"/> class.
    /// </summary>
    /// <remarks>
    /// This variable holds an instance of <see cref="TimeZoneInfo"/> for the currently effective time zone
    /// specific to the current asynchronous flow. If no value is set, the local system's time zone is used as
    /// the fallback default. It facilitates flexible and isolated manipulation of time zone data for scenarios
    /// like testing or temporary overrides within a given context.
    /// </remarks>
    private static readonly AsyncLocal<TimeZoneInfo?> _timeZone = new();

    /// <summary>
    /// Gets the time zone currently in effect for the <see cref="Clock"/> class.
    /// </summary>
    /// <remarks>
    /// This property retrieves the time zone applicable to the current asynchronous context. If no custom time zone
    /// has been specified, the local system's default time zone is returned. It is crucial for ensuring that all
    /// date and time operations performed by the <see cref="Clock"/> class respect the configured effective time zone
    /// for consistent behavior, especially in scenarios involving custom or overridden time zones such as tests or localized contexts.
    /// </remarks>
    public static TimeZoneInfo EffectiveTimeZone => _timeZone.Value ?? TimeZoneInfo.Local;

    /// <summary>
    /// Gets the current UTC date and time as a <see cref="DateTimeOffset"/> with an offset of zero, representing the UTC timezone.
    /// </summary>
    /// <remarks>
    /// This property provides a consistent and standardized way to obtain the current Coordinated Universal Time in a
    /// format that includes both the date, time, and offset information. The offset is always zero since it reflects UTC.
    /// It is particularly useful in scenarios requiring a testable time source, as it supports overriding or mocking the
    /// time during testing processes.
    /// </remarks>
    public static DateTimeOffset UtcNowOffset
    {
        get
        {
            var mockedTime = GetMockedTime();
            return mockedTime is null
                ? DateTimeOffset.UtcNow
                : TimeZoneInfo.ConvertTime(mockedTime.Value, TimeZoneInfo.Utc);
        }
    }

    /// <summary>
    /// Gets the current local date and time as a <see cref="DateTimeOffset"/> adjusted to the effective time zone.
    /// </summary>
    /// <remarks>
    /// This property provides a way to retrieve the current date and time that accounts for the effective time zone,
    /// which may be manually overridden using the <see cref="ChangeGlobalTimeZone"/> or <see cref="ChangeGlobalTimeZone(System.TimeZoneInfo)"/> methods.
    /// If no override is set, the system's local time zone is used by default. This is particularly useful in scenarios
    /// where applications need to operate in different time zones or during testing when specific time zone adjustments
    /// are required.
    /// </remarks>
    public static DateTimeOffset NowOffset => TimeZoneInfo.ConvertTime(UtcNowOffset, EffectiveTimeZone);

    /// <summary>
    /// Gets the current UTC date and time as a <see cref="DateTime"/> representing the Coordinated Universal Time.
    /// </summary>
    /// <remarks>
    /// This property retrieves the current UTC time in a standard <see cref="DateTime"/> format without any offset information.
    /// It is useful in scenarios where a UTC timestamp is needed without timezone offset details. The value can be influenced
    /// by mocked times set for testing purposes, providing flexibility for time-dependent tests.
    /// </remarks>
    public static DateTime UtcNow => UtcNowOffset.UtcDateTime;

    /// <summary>
    /// Gets the current local date and time as a <see cref="DateTime"/> based on the effective time zone.
    /// </summary>
    /// <remarks>
    /// This property provides the local date and time, accounting for any effective time zone overrides
    /// set through the <see cref="Clock"/> class. By default, it reflects the machine's local time.
    /// When a mocked time or specific time zone is configured, the value returned can deviate from the
    /// system's current local time.
    /// </remarks>
    public static DateTime Now => NowOffset.DateTime;

    /// <summary>
    /// Changes the effective time zone for the <see cref="Clock"/> class and returns a disposable object
    /// to revert the change when disposed.
    /// </summary>
    /// <param name="timeZone">The string identifier of the time zone to set as the effective time zone.</param>
    /// <returns>
    /// An <see cref="IDisposable"/> that, when disposed, restores the previous effective time zone.
    /// </returns>
    public static IDisposable ChangeGlobalTimeZone(string timeZone)
    {
        var tzInfo = TimeZoneReader.ParseTimeZone(timeZone);
        
        return ChangeGlobalTimeZone(tzInfo);       
    }

    /// <summary>
    /// Changes the effective time zone for the <see cref="Clock"/> class and returns a disposable object
    /// to revert the change when disposed.
    /// </summary>
    /// <param name="timeZoneInfo">The <see cref="TimeZoneInfo"/> to set as the effective time zone.</param>
    /// <returns>
    /// An <see cref="IDisposable"/> that, when disposed, restores the previous effective time zone.
    /// </returns>
    public static IDisposable ChangeGlobalTimeZone(TimeZoneInfo timeZoneInfo)
    {
        var previousTz = _timeZone.Value;
        _timeZone.Value = timeZoneInfo;

        return new DisposeAction(() => { _timeZone.Value = previousTz; });
    }

    /// <summary>
    /// Creates a new instance of <see cref="TimeZoneClock"/> for the specified time zone.
    /// </summary>
    /// <param name="timeZone">The string identifier of the time zone for which the clock will be created.</param>
    /// <returns>
    /// A <see cref="TimeZoneClock"/> instance configured for the specified time zone.
    /// </returns>
    public static TimeZoneClock ForTimeZone(string timeZone)
    {
        var tzInfo = TimeZoneReader.ParseTimeZone(timeZone);

        return ForTimeZone(tzInfo);  
    }

    /// <summary>
    /// Creates a <see cref="TimeZoneClock"/> instance configured for a specified time zone.
    /// </summary>
    /// <param name="timeZoneInfo">The <see cref="TimeZoneInfo"/> to create a time zone clock instance.</param>
    /// <returns>
    /// A <see cref="TimeZoneClock"/> instance configured for the specified time zone.
    /// </returns>
    public static TimeZoneClock ForTimeZone(TimeZoneInfo timeZoneInfo)
    {
        return new TimeZoneClock(timeZoneInfo);   
    }
}