using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Features.Tasks.Queries.GetTaskById;

public record GetTaskByIdQuery(
    Guid TaskId,
    Guid UserId
) : IRequest<TaskItemDto>;
