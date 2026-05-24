namespace GenoDev.Utilities.Core.Time;

public partial class Clock
{
    /// <summary>
    /// A queue used to store mocked DateTimeOffset values for testing purposes.
    /// This is utilized to provide predetermined datetime values in scenarios
    /// where controlling time behavior is necessary, such as unit tests.
    /// </summary>
    private static Queue<DateTimeOffset>? _mockedDateTimeOffsets;

    /// <summary>
    /// Sets mocked DateTimeOffset values for testing purposes. This method allows
    /// predetermined datetime values to be supplied to enable controlled testing
    /// of temporal logic by overriding standard clock behavior.
    /// </summary>
    /// <param name="mockedDateTimeOffsets">
    /// An optional array of DateTimeOffset values to set as the mocked sequence
    /// of dates. Leaves the mocking inactive if null or empty.
    /// </param>
    internal static void SetMockedTimes(params DateTimeOffset[]? mockedDateTimeOffsets)
    {
        if (mockedDateTimeOffsets is { Length: > 0 })
        {
            _mockedDateTimeOffsets = new Queue<DateTimeOffset>(mockedDateTimeOffsets);
            return;
        }

        _mockedDateTimeOffsets = null;
    }

    /// <summary>
    /// Retrieves the next mocked DateTimeOffset value from the internal queue for testing purposes.
    /// If no mocked values are set, returns null. This method is used to simulate
    /// deterministic datetime scenarios in controlled test environments by cycling through
    /// the sequence of mock values provided.
    /// </summary>
    /// <returns>
    /// The next mocked DateTimeOffset from the internal queue, or null if no mocked values are available.
    /// </returns>
    private static DateTimeOffset? GetMockedTime()
    {
        if (_mockedDateTimeOffsets is null)
            return null;

        var mock = _mockedDateTimeOffsets.Dequeue();
        _mockedDateTimeOffsets.Enqueue(mock);

        return mock;
    }
}