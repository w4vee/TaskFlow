using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>
/// TaskItemRepository - CRUD + filtering methods cho TaskItem.
/// 
/// Các method filter này phục vụ:
/// - GetTasksByBoardIdAsync: hiển thị Kanban board
/// - GetTasksByStatusAsync: filter theo column (Todo/InProgress/Done)
/// - GetOverdueTasksAsync: Hangfire job kiểm tra task quá hạn
/// - GetTasksByAssignedUserAsync: xem tasks được assign cho mình
/// 
/// Include(t => t.AssignedTo): load luôn User để mapping AssignedToName trong DTO.
/// Nếu không Include → t.AssignedTo sẽ null → AssignedToName = null.
/// </summary>
public class TaskItemRepository : GenericRepository<TaskItem>, ITaskItemRepository
{
    public TaskItemRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Lấy tất cả tasks trong 1 board.
    /// Include AssignedTo để có AssignedToName cho DTO.
    /// OrderBy Priority descending → Critical tasks lên đầu.
    /// </summary>
    public async Task<IEnumerable<TaskItem>> GetTasksByBoardIdAsync(Guid boardId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.AssignedTo)
            .Where(t => t.BoardId == boardId)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy tasks theo status (vd: lọc tất cả task "InProgress" across boards).
    /// </summary>
    public async Task<IEnumerable<TaskItem>> GetTasksByStatusAsync(TaskItemStatus status)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.AssignedTo)
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.Priority)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy tasks quá hạn - dùng cho Hangfire recurring job.
    /// Điều kiện: Deadline đã qua + Status chưa Done + chưa mark IsOverdue
    ///   (hoặc đã mark rồi thì vẫn lấy để hiển thị).
    /// 
    /// Ở đây lấy tất cả task đã quá hạn mà chưa Done.
    /// Hangfire job sẽ gọi method này → update IsOverdue = true → notify user.
    /// </summary>
    public async Task<IEnumerable<TaskItem>> GetOverdueTasksAsync()
    {
        return await _dbSet
            .Include(t => t.AssignedTo)
            .Include(t => t.Board)
            .Where(t => t.Deadline != null
                     && t.Deadline < DateTime.UtcNow
                     && t.Status != TaskItemStatus.Done)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy tasks assigned cho 1 user cụ thể.
    /// Dùng cho "My Tasks" view.
    /// </summary>
    public async Task<IEnumerable<TaskItem>> GetTasksByAssignedUserAsync(Guid userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(t => t.Board)
            .Where(t => t.AssignedToId == userId)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Deadline)
            .ToListAsync();
    }
}
