using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class TaskBoard : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Foreign key
    public Guid OwnerId { get; set; }

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
