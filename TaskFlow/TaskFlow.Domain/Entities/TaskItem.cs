using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? Deadline { get; set; }
    public bool IsOverdue { get; set; } = false;

    // Foreign keys
    public Guid BoardId { get; set; }
    public Guid? AssignedToId { get; set; }

    // Navigation properties
    public TaskBoard Board { get; set; } = null!;
    public User? AssignedTo { get; set; }
}
