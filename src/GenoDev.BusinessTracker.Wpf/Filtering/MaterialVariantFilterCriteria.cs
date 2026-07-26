namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record MaterialVariantFilterCriteria(
    string? Name,
    string? Ean,
    string? ManufacturerCode,
    string? Description)
{
    public static MaterialVariantFilterCriteria Empty { get; } =
        new(null, null, null, null);
}