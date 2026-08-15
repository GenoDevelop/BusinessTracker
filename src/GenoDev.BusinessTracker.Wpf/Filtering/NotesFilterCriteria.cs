namespace GenoDev.BusinessTracker.Wpf.Filtering;

public sealed record NotesFilterCriteria(string? Name)
{
    public static NotesFilterCriteria Empty { get; } = new((string?)null);
}
