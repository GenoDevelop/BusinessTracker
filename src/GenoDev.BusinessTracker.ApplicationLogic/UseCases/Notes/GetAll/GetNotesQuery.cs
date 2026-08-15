using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetAll;

public sealed record NoteListItemDto(Guid Id, string Name);

public sealed record GetNotesQuery(
    int PageIndex,
    int PageSize,
    NoteSortBy SortBy = NoteSortBy.Name,
    bool IsDescending = false,
    string? NameFilter = null) : IRequest<PagedList<NoteListItemDto>>;
