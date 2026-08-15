using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetAll;

public sealed class GetNotesQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetNotesQuery, PagedList<NoteListItemDto>>
{
    public async Task<PagedList<NoteListItemDto>> Handle(
        GetNotesQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Notes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.NameFilter))
        {
            query = query.WhereContainsAll(x => x.Name, request.NameFilter);
        }

        var orderedQuery = request.SortBy switch
        {
            NoteSortBy.Name => request.IsDescending
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };
        orderedQuery = orderedQuery.ThenByStable(x => x.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await orderedQuery
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new NoteListItemDto(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        return new PagedList<NoteListItemDto>(
            items,
            totalCount,
            request.PageIndex,
            request.PageSize);
    }
}
