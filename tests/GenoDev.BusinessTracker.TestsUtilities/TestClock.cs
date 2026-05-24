using GenoDev.Utilities.Core.Extensions;
using GenoDev.Utilities.Core.Time;

namespace GenoDev.BusinessTracker.TestsUtilities;

/// <summary>
/// Provides utilities for mocking and managing system clock behavior for the purpose of testing.
/// </summary>
public class TestClock : IDisposable
{
    /// <summary>
    /// Represents a synchronization primitive that limits access to a resource or pool of resources
    /// to a maximum specified number of threads. Used within the <see cref="TestClock"/> class to ensure
    /// thread safety during updates to mocked clock states.
    /// </summary>
    private static readonly Semaphore Semaphore = new(1, 1);

    private TestClock()
    {
    }

    /// <summary>
    /// Mocks the current clock date and time with a single specified value.
    /// </summary>
    /// <param name="dateMock">A DateTimeOffset value to mock as the current date and time.</param>
    /// <returns>Returns an instance of <see cref="TestClock"/> to manage the mocked clock.</returns>
    public static TestClock MockClockDate(DateTimeOffset dateMock) => MockClockDates(dateMock);

    /// <summary>
    /// Sets up the clock with mocked date and time values.
    /// </summary>
    /// <param name="datesMock">An array of DateTimeOffset values to mock as the current date and time. The mocked dates will be cycled through in order.</param>
    /// <returns>Returns an instance of <see cref="TestClock"/> to manage the mocked clock.</returns>
    public static TestClock MockClockDates(params DateTimeOffset[] datesMock)
    {
        Semaphore.WaitOne();

        var testClock = new TestClock();

        Clock.SetMockedTimes(datesMock);

        return testClock;
    }

    /// <summary>
    /// Freezes the current system time by mocking it to the current UTC time value.
    /// </summary>
    /// <returns>Returns an instance of <see cref="TestClock"/> to manage the mocked clock.</returns>
    public static TestClock FreezeCurrentTime() => MockClockDate(DateTimeOffset.UtcNow.WithoutTicks());
    
    /// <summary>
    /// Updates the mocked clock with a single new date and time value.
    /// </summary>
    /// <param name="dateMock">A DateTimeOffset value to update as the current mocked date and time.</param>
    public void ChangeMockedClockDate(DateTimeOffset dateMock) => ChangeMockedClockDates(dateMock);

    /// <summary>
    /// Updates the mocked clock with new date and time values.
    /// </summary>
    /// <param name="datesMock">An array of DateTimeOffset values to update as the current mocked dates and times. The mocked dates will cycle in sequence.</param>
    public void ChangeMockedClockDates(DateTimeOffset datesMock)
    {
        Clock.SetMockedTimes(datesMock);
    }

    /// <summary>
    /// Updates the mocked clock by freezing again the new current system time to the current UTC time value.
    /// </summary>
    public void FreezeCurrentTimeAgain() => ChangeMockedClockDate(DateTimeOffset.UtcNow.WithoutTicks());

    /// <summary>
    /// Releases the specified semaphore and resets the mocked clock date and time values to null.
    /// </summary>
    /// <param name="semaphore">The semaphore instance to be released.</param>
    private static void Release(Semaphore semaphore)
    {
        Clock.SetMockedTimes(null);
        semaphore.Release();
    }

    /// <summary>
    /// Releases resources used by the TestClock instance, including resetting mocked clock date and time values
    /// and releasing any acquired semaphore.
    /// </summary>
    public void Dispose()
    {
        Release(Semaphore);
        GC.SuppressFinalize(this);
    }
}