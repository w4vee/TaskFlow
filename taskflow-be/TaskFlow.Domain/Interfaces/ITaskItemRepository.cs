using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Interfaces;

public interface ITaskItemRepository : IGenericRepository<TaskItem>
{
    Task<IEnumerable<TaskItem>> GetTasksByBoardIdAsync(Guid boardId);
    Task<IEnumerable<TaskItem>> GetTasksByStatusAsync(TaskItemStatus status);
    Task<IEnumerable<TaskItem>> GetOverdueTasksAsync();
    Task<IEnumerable<TaskItem>> GetTasksByAssignedUserAsync(Guid userId);
}
