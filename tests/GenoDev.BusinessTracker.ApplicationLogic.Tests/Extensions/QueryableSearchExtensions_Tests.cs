using FluentAssertions;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.Extensions;

public class QueryableSearchExtensions_Tests
{
    [Fact]
    public void ThenByStable_ShouldOrderEqualPrimaryValuesByTieBreaker()
    {
        // Arrange
        var firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var items = new[]
        {
            new TestItem(secondId, "Same"),
            new TestItem(firstId, "Same")
        }.AsQueryable().OrderBy(x => x.Name);

        // Act
        var result = items
            .ThenByStable(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        // Assert
        result.Should().ContainInOrder(firstId, secondId);
    }

    private sealed record TestItem(Guid Id, string Name);
}
