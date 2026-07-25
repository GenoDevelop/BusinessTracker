namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record SuppliersFilterCriteria(
    string? Name,
    string? Nip,
    string? Description)
{
    public static SuppliersFilterCriteria Empty { get; } =
        new(null, null, null);
}