using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetDetails;

public sealed record NoteDetailsDto(Guid Id, string Name, string ContentRtf);

public sealed record GetNoteDetailsQuery(Guid Id) : IRequest<NoteDetailsDto>;
