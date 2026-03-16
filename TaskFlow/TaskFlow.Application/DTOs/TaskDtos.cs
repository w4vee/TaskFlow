using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.DTOs;

// ===== TASK DTOs =====

/// <summary>
/// Output DTO cho TaskItem.
/// Dùng init properties thay vì positional record vì AssignedToName
/// là computed value từ AutoMapper .ForMember() (không có trực tiếp trên entity).
/// </summary>
public record TaskItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskItemStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public DateTime? Deadline { get; init; }
    public bool IsOverdue { get; init; }
    public Guid BoardId { get; init; }
    public Guid? AssignedToId { get; init; }
    public string? AssignedToName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateTaskDto(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? Deadline,
    Guid? AssignedToId
);

public record UpdateTaskDto(
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateTime? Deadline,
    Guid? AssignedToId
);
