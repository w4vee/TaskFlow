using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Repositories;

namespace TaskFlow.Infrastructure;

/// <summary>
/// UnitOfWork - quản lý tất cả repositories + đảm bảo 1 transaction duy nhất.
/// 
/// Tại sao cần UnitOfWork?
/// Giả sử RegisterCommandHandler cần:
///   1. Tạo User mới
///   2. (Tương lai) Tạo Board mặc định cho user
/// 
/// Nếu KHÔNG có UnitOfWork:
///   _userRepo.AddAsync(user);     // Mỗi repo có SaveChanges riêng
///   _boardRepo.AddAsync(board);   // Nếu cái này fail → user đã save rồi → data inconsistent!
/// 
/// Có UnitOfWork:
///   _unitOfWork.Users.AddAsync(user);      // Chưa save
///   _unitOfWork.TaskBoards.AddAsync(board); // Chưa save
///   _unitOfWork.SaveChangesAsync();         // Save TẤT CẢ cùng 1 transaction
///   // Nếu fail → rollback TẤT CẢ → data consistent!
/// 
/// Lazy Initialization:
/// Repositories được tạo lazy (khi nào access mới new).
/// Nếu Handler chỉ dùng Users → không tạo TaskBoardRepository, TaskItemRepository.
/// ?? = null-coalescing: nếu _users null thì tạo mới, không thì dùng lại.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    // Lazy initialization - repositories chỉ được tạo khi access
    private IUserRepository? _users;
    private ITaskBoardRepository? _taskBoards;
    private ITaskItemRepository? _taskItems;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    // ??= (null-coalescing assignment): nếu _users null → tạo mới, nếu có rồi → dùng lại
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ITaskBoardRepository TaskBoards => _taskBoards ??= new TaskBoardRepository(_context);
    public ITaskItemRepository TaskItems => _taskItems ??= new TaskItemRepository(_context);

    /// <summary>
    /// SaveChangesAsync - commit TẤT CẢ thay đổi trong 1 transaction.
    /// 
    /// Return int = số rows bị ảnh hưởng (affected).
    /// Vd: Add 1 User + Add 1 Board → return 2.
    /// 
    /// EF Core tự wrap trong transaction:
    /// - BEGIN TRANSACTION
    /// - INSERT INTO Users ...
    /// - INSERT INTO TaskBoards ...
    /// - COMMIT (nếu tất cả OK)
    /// - hoặc ROLLBACK (nếu bất kỳ cái nào fail)
    /// </summary>
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Dispose - giải phóng DbContext và database connection.
    /// 
    /// DI container tự gọi Dispose khi request kết thúc (Scoped lifetime).
    /// Nhưng implement IDisposable đúng cách là best practice.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }
}
