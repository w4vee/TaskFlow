using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface ITaskBoardRepository : IGenericRepository<TaskBoard>
{
    Task<IEnumerable<TaskBoard>> GetBoardsByOwnerIdAsync(Guid ownerId);
    Task<TaskBoard?> GetBoardWithTasksAsync(Guid boardId);
}
