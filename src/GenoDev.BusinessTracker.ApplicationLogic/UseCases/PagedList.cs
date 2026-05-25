namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases;

public record PagedList<T>(List<T> Items, int TotalCount, bool HasNextPage)
{
    public PagedList(List<T> items, int totalCount, int page, int pageSize)
        : this(items, totalCount, (page + 1) * pageSize < totalCount)
    {
    }
}
