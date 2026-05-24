namespace GenoDev.BusinessTracker.TestsUtilities;

/// <summary>
/// Provides access to the current test's context, such as the output helper.
/// </summary>
internal static class TestContext
{
    private static readonly AsyncLocal<ITestOutputHelper?> _output = new();

    /// <summary>
    /// Gets or sets the <see cref="ITestOutputHelper"/> for the current test.
    /// </summary>
    public static ITestOutputHelper? Output
    {
        get => _output.Value;
        set => _output.Value = value;
    }

    public static bool AutoUseInHttpClient { get; set; }

    /// <summary>
    /// Returns the provided <paramref name="output"/> helper, or the default <see cref="Output"/> if <see cref="AutoUseInHttpClient"/> is enabled.
    /// </summary>
    /// <param name="output">The <see cref="ITestOutputHelper"/> to use.</param>
    /// <returns>The resolved <see cref="ITestOutputHelper"/>.</returns>
    public static ITestOutputHelper? ResolveOutput(ITestOutputHelper? output)
    {
        return output ?? (AutoUseInHttpClient ? Output : null);
    }
}