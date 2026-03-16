using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Features.Boards.Queries.GetBoardById;

/// <summary>
/// Query lấy chi tiết 1 board theo ID.
/// </summary>
public record GetBoardByIdQuery(
    Guid BoardId,
    Guid UserId
) : IRequest<TaskBoardDto>;
