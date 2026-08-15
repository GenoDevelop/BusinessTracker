using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetDetails;

public sealed class GetNoteDetailsQueryHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<GetNoteDetailsQuery, NoteDetailsDto>
{
    public async Task<NoteDetailsDto> Handle(
        GetNoteDetailsQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.Notes
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new NoteDetailsDto(x.Id, x.Name, x.ContentRtf))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw RequestValidationException.For(
                "Nie znaleziono notatki.",
                nameof(GetNoteDetailsQuery.Id));
    }
}
