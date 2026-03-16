using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>
/// TaskBoardRepository - CRUD + methods riêng cho TaskBoard.
/// 
/// Eager Loading với Include():
/// Mặc định EF Core dùng Lazy Loading (load navigation property khi access).
/// Nhưng trong Web API, Lazy Loading gây N+1 problem:
///   - Query 1: SELECT * FROM TaskBoards WHERE OwnerId = @id (10 boards)
///   - Query 2-11: SELECT * FROM TaskItems WHERE BoardId = @boardId (1 query per board)
///   → 11 queries thay vì 1!
/// 
/// Include() = Eager Loading = load luôn trong 1 query:
///   SELECT b.*, t.* FROM TaskBoards b LEFT JOIN TaskItems t ON b.Id = t.BoardId
///   → 1 query duy nhất!
/// </summary>
public class TaskBoardRepository : GenericRepository<TaskBoard>, ITaskBoardRepository
{
    public TaskBoardRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Lấy tất cả boards của 1 user.
    /// Include(Tasks) để tính TaskCount trong DTO mapping.
    /// AsNoTracking vì chỉ đọc.
    /// </summary>
    public async Task<IEnumerable<TaskBoard>> GetBoardsByOwnerIdAsync(Guid ownerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(b => b.Tasks)           // Eager load tasks (để đếm TaskCount)
            .Where(b => b.OwnerId == ownerId)
            .OrderByDescending(b => b.CreatedAt)  // Mới nhất lên đầu
            .ToListAsync();
    }

    /// <summary>
    /// Lấy 1 board kèm tasks (dùng cho GetBoardById query).
    /// KHÔNG dùng AsNoTracking vì board có thể được update sau đó.
    /// </summary>
    public async Task<TaskBoard?> GetBoardWithTasksAsync(Guid boardId)
    {
        return await _dbSet
            .Include(b => b.Tasks)
                .ThenInclude(t => t.AssignedTo)  // Load luôn User assigned cho mỗi task
            .FirstOrDefaultAsync(b => b.Id == boardId);
    }
}
