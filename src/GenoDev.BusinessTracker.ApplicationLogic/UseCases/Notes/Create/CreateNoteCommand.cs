using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.Create;

public sealed record CreateNoteCommand(string Name) : IRequest<Guid>;
