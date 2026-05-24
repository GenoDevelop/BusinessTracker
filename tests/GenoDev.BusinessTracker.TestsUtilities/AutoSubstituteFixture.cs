using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace GenoDev.BusinessTracker.TestsUtilities;

internal class AutoSubstituteFixture : Fixture
{
    public AutoSubstituteFixture()
    {
        Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
    }
}