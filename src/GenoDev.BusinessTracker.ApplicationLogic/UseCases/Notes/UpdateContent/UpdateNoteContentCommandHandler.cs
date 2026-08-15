using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.UpdateContent;

public sealed class UpdateNoteContentCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<UpdateNoteContentCommand>
{
    public async Task Handle(
        UpdateNoteContentCommand request,
        CancellationToken cancellationToken)
    {
        var note = await dbContext.Notes
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw RequestValidationException.For(
                "Nie znaleziono notatki.",
                nameof(UpdateNoteContentCommand.Id));

        note.ContentRtf = request.ContentRtf;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
