using MediatR;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Features.Tasks.Commands.CreateTask;

/// <summary>
/// Command tạo task mới trong 1 board.
/// BoardId: task thuộc board nào.
/// UserId: ai đang tạo (phải là owner của board).
/// </summary>
public record CreateTaskCommand(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? Deadline,
    Guid? AssignedToId,
    Guid BoardId,
    Guid UserId
) : IRequest<TaskItemDto>;
