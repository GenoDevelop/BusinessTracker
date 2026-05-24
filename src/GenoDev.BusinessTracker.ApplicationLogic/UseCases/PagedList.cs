namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases;

public record PagedList<T>(List<T> Items, int TotalCount, bool HasNextPage);
