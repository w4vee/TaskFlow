using MediatR;

namespace TaskFlow.Application.Features.Tasks.Commands.DeleteTask;

public record DeleteTaskCommand(
    Guid TaskId,
    Guid UserId
) : IRequest<Unit>;
