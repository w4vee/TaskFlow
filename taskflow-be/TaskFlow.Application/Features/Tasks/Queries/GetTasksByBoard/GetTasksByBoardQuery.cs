using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Features.Tasks.Queries.GetTasksByBoard;

/// <summary>
/// Query lấy tất cả tasks trong 1 board.
/// Đây là query chính cho Kanban board view.
/// </summary>
public record GetTasksByBoardQuery(
    Guid BoardId,
    Guid UserId
) : IRequest<List<TaskItemDto>>;
