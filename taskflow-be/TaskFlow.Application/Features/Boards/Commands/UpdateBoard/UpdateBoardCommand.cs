using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Features.Boards.Commands.UpdateBoard;

/// <summary>
/// Command cập nhật board.
/// 
/// UserId: để verify người update có phải owner không.
/// Clean Architecture rule: authorization logic nằm trong Handler,
/// KHÔNG phải trong Controller.
/// </summary>
public record UpdateBoardCommand(
    Guid BoardId,
    string Name,
    string? Description,
    Guid UserId
) : IRequest<TaskBoardDto>;
