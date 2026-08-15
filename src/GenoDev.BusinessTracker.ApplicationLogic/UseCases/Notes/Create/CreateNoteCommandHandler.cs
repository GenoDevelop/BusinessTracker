using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.Create;

public sealed class CreateNoteCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<CreateNoteCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateNoteCommand request,
        CancellationToken cancellationToken)
    {
        var note = new Note
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ContentRtf = string.Empty
        };

        dbContext.Notes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);

        return note.Id;
    }
}
