namespace GenoDev.Utilities.Core.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="TimeSpan"/> structure to perform common time-related operations.
/// </summary>
public static class TimeSpanExtensions
{
    /// <summary>
    /// Returns a new <see cref="TimeSpan"/> with the ticks component removed (the smallest unit of time).
    /// </summary>
    /// <param name="timeSpan">The original TimeSpan value.</param>
    /// <returns>A new TimeSpan with ticks truncated.</returns>
    /// <example>
    /// <code>
    /// TimeSpan original = new TimeSpan(1, 2, 3, 4, 123) + TimeSpan.FromTicks(4567);
    /// // original = 1.02:03:04.1234567
    ///  
    /// TimeSpan result = original.WithoutTicks();
    /// // result = 1.02:03:04.1234560 (ticks truncated)
    /// </code>
    /// </example>
    public static TimeSpan WithoutTicks(this TimeSpan timeSpan)
    {
        var ticks = timeSpan.Ticks % 10;
        return timeSpan.Add(-TimeSpan.FromTicks(ticks));
    }
    
    /// <summary>
    /// Returns a new <see cref="TimeSpan"/> with the microseconds component removed.
    /// </summary>
    /// <param name="timeSpan">The original TimeSpan value.</param>
    /// <returns>A new TimeSpan with microseconds truncated.</returns>
    /// <example>
    /// <code>
    /// TimeSpan original = new TimeSpan(1, 2, 3, 4, 123) + TimeSpan.FromTicks(4567);
    /// // original = 1.02:03:04.1234567
    ///  
    /// TimeSpan result = original.WithoutMicroseconds();
    /// // result = 1.02:03:04.1230000 (microseconds truncated)
    /// </code>
    /// </example>
    public static TimeSpan WithoutMicroseconds(this TimeSpan timeSpan)
    {
        var ticks = timeSpan.Ticks % 10000;
        return timeSpan.Add(-TimeSpan.FromTicks(ticks));
    }
    
    /// <summary>
    /// Returns a new <see cref="TimeSpan"/> with the milliseconds component removed.
    /// </summary>
    /// <param name="timeSpan">The original TimeSpan value.</param>
    /// <returns>A new TimeSpan with milliseconds truncated.</returns>
    /// <example>
    /// <code>
    /// TimeSpan original = new TimeSpan(1, 2, 3, 4, 123) + TimeSpan.FromTicks(4567);
    /// // original = 1.02:03:04.1234567
    ///  
    /// TimeSpan result = original.WithoutMilliseconds();
    /// // result = 1.02:03:04.0000000 (milliseconds truncated)
    /// </code>
    /// </example>
    public static TimeSpan WithoutMilliseconds(this TimeSpan timeSpan)
    {
        var ticks = timeSpan.Ticks % 10000000;
        return timeSpan.Add(-TimeSpan.FromTicks(ticks));
    }
}