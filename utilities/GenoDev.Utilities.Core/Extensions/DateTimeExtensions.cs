namespace GenoDev.Utilities.Core.Extensions;

/// <summary>
/// Provides extension methods for DateTime and DateTimeOffset types to remove specific time components
/// from date/time values, enabling more precise time manipulation and normalization.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Removes the least significant ticks from a DateTime value.
    /// </summary>
    /// <param name="dateTime">The DateTime value to modify.</param>
    /// <returns>A new DateTime with the least significant ticks removed.</returns>
    /// <remarks>
    /// This method removes the remainder of ticks when divided by 10, effectively truncating 
    /// the DateTime to the nearest 100-nanosecond interval.
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime value = new DateTime(2023, 1, 1, 12, 30, 45).AddTicks(6);
    /// DateTime result = value.WithoutTicks(); // Ticks ending in 6 are removed
    /// </code>
    /// </example>
    public static DateTime WithoutTicks(this DateTime dateTime)
    {
        var ticks = dateTime.TimeOfDay.Ticks % 10;
        return dateTime.AddTicks(-ticks);
    }

    /// <summary>
    /// Removes the least significant ticks from a DateTimeOffset value.
    /// </summary>
    /// <param name="dateTime">The DateTimeOffset value to modify.</param>
    /// <returns>A new DateTimeOffset with the least significant ticks removed.</returns>
    /// <remarks>
    /// This method removes the remainder of ticks when divided by 10, effectively truncating 
    /// the DateTimeOffset to the nearest 100-nanosecond interval.
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTimeOffset value = new DateTimeOffset(2023, 1, 1, 12, 30, 45, TimeSpan.Zero).AddTicks(6);
    /// DateTimeOffset result = value.WithoutTicks(); // Ticks ending in 6 are removed
    /// </code>
    /// </example>
    public static DateTimeOffset WithoutTicks(this DateTimeOffset dateTime)
    {
        var ticks = dateTime.TimeOfDay.Ticks % 10;
        return dateTime.AddTicks(-ticks);
    }

    /// <summary>
    /// Removes microseconds from a DateTime value while preserving other time components.
    /// </summary>
    /// <param name="dateTime">The DateTime value to modify.</param>
    /// <returns>A new DateTime with microseconds removed.</returns>
    /// <remarks>
    /// This method first removes the least significant ticks using <see cref="WithoutTicks(DateTime)"/>,
    /// then removes the microseconds component. The resulting DateTime will have 0 for its microsecond value.
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime value = new DateTime(2023, 1, 1, 12, 30, 45, 500).AddMicroseconds(123);
    /// DateTime result = value.WithoutMicroseconds(); // Result will have 0 microseconds
    /// </code>
    /// </example>
    public static DateTime WithoutMicroseconds(this DateTime dateTime)
    {
        var ticks = dateTime.TimeOfDay.Ticks % 10000;
        return dateTime.AddTicks(-ticks);
    }

    /// <summary>
    /// Removes microseconds from a DateTimeOffset value while preserving other time components.
    /// </summary>
    /// <param name="dateTime">The DateTimeOffset value to modify.</param>
    /// <returns>A new DateTimeOffset with microseconds removed.</returns>
    /// <remarks>
    /// This method first removes the least significant ticks using <see cref="WithoutTicks(DateTimeOffset)"/>,
    /// then removes the microseconds component. The resulting DateTimeOffset will have 0 for its microsecond value.
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTimeOffset value = new DateTimeOffset(2023, 1, 1, 12, 30, 45, 500, TimeSpan.Zero).AddMicroseconds(123);
    /// DateTimeOffset result = value.WithoutMicroseconds(); // Result will have 0 microseconds
    /// </code>
    /// </example>
    public static DateTimeOffset WithoutMicroseconds(this DateTimeOffset dateTime)
    {
        var ticks = dateTime.TimeOfDay.Ticks % 10000;
        return dateTime.AddTicks(-ticks);
    }
    
    /// <summary>
    /// Removes milliseconds and microseconds from a DateTime value while preserving other time components.
    /// </summary>
    /// <param name="dateTime">The DateTime value to modify.</param>
    /// <returns>A new DateTime with milliseconds and microseconds removed.</returns>
    /// <remarks>
    /// This method builds on <see cref="WithoutMicroseconds(DateTime)"/> by also removing the milliseconds component.
    /// The resulting DateTime will have 0 for both its millisecond and microsecond values.
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTime value = new DateTime(2023, 1, 1, 12, 30, 45, 789).AddMicroseconds(123);
    /// DateTime result = value.WithoutMilliseconds(); // Result will have 0 milliseconds and 0 microseconds
    /// </code>
    /// </example>
    public static DateTime WithoutMilliseconds(this DateTime dateTime)
    {
        var ticks = dateTime.TimeOfDay.Ticks % 10000000;
        return dateTime.AddTicks(-ticks);
    }

    /// <summary>
    /// Removes milliseconds and microseconds from a DateTimeOffset value while preserving other time components.
    /// </summary>
    /// <param name="dateTime">The DateTimeOffset value to modify.</param>
    /// <returns>A new DateTimeOffset with milliseconds and microseconds removed.</returns>
    /// <remarks>
    /// This method builds on <see cref="WithoutMicroseconds(DateTimeOffset)"/> by also removing the milliseconds component.
    /// The resulting DateTimeOffset will have 0 for both its millisecond and microsecond values.
    /// </remarks>
    /// <example>
    /// <code>
    /// DateTimeOffset value = new DateTimeOffset(2023, 1, 1, 12, 30, 45, 789, TimeSpan.Zero).AddMicroseconds(123);
    /// DateTimeOffset result = value.WithoutMilliseconds(); // Result will have 0 milliseconds and 0 microseconds
    /// </code>
    /// </example>
    public static DateTimeOffset WithoutMilliseconds(this DateTimeOffset dateTime)
    {
        var ticks = dateTime.TimeOfDay.Ticks % 10000000;
        return dateTime.AddTicks(-ticks);
    }
}