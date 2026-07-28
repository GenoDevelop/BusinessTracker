using GenoDev.BusinessTracker.Domain.Enums;

namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record RecipeMaterialsFilterCriteria(
    string? MaterialName,
    string? Description)
{
    public static RecipeMaterialsFilterCriteria Empty { get; } = new(null, null);
}
