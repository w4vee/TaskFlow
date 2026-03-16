using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>
/// GenericRepository - implement CRUD chung cho tất cả entities.
/// 
/// Tại sao dùng Generic?
/// - User, TaskBoard, TaskItem đều cần: GetById, GetAll, Add, Update, Delete
/// - Viết 1 lần, dùng cho tất cả → DRY (Don't Repeat Yourself)
/// - Specific repositories (UserRepository, etc.) kế thừa và thêm methods riêng
/// 
/// DbSet<T> trong EF Core:
/// - _dbSet = _context.Set<T>() → trả về DbSet tương ứng với entity T
/// - Nếu T = User → _dbSet = _context.Users
/// - DbSet hỗ trợ LINQ queries: .Where(), .FirstOrDefault(), .ToList(), etc.
/// - EF Core dịch LINQ → SQL tự động
/// 
/// AsNoTracking():
/// - Mặc định EF Core "track" mọi entity query được (Change Tracking)
/// - Track tốn memory, nếu chỉ READ (không UPDATE) thì dùng AsNoTracking() cho nhanh
/// - Ở đây GetAllAsync, FindAsync dùng AsNoTracking vì thường chỉ đọc
/// - GetByIdAsync KHÔNG dùng AsNoTracking vì entity có thể được update sau đó
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// Tìm entity theo Id.
    /// FindAsync() là method tối ưu nhất để tìm theo Primary Key:
    /// - Nó check trong Change Tracker trước (nếu entity đã load rồi → không query DB lại)
    /// - Nếu chưa có → query database
    /// </summary>
    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// Lấy tất cả entities.
    /// AsNoTracking() vì danh sách thường chỉ để hiển thị, không cần track.
    /// ToListAsync() thực thi query ngay (eager execution).
    /// </summary>
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// Tìm entities theo điều kiện (Expression).
    /// 
    /// Expression<Func<T, bool>> predicate -- đây là Expression Tree, không phải delegate thường.
    /// EF Core cần Expression Tree để dịch thành SQL WHERE clause.
    /// Ví dụ: FindAsync(u => u.Email == "test@test.com")
    ///   → EF Core dịch thành: SELECT * FROM Users WHERE Email = 'test@test.com'
    /// 
    /// Nếu dùng Func<T, bool> (không phải Expression) → EF Core load TOÀN BỘ bảng rồi filter trong memory → chậm!
    /// </summary>
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Thêm entity mới.
    /// AddAsync() chỉ mark entity là "Added" trong Change Tracker.
    /// SQL INSERT chưa chạy! Phải gọi SaveChangesAsync() (qua UnitOfWork) để thực sự insert.
    /// </summary>
    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// Cập nhật entity.
    /// Nếu entity đang được track (load từ FindAsync/GetByIdAsync) → chỉ cần set property, 
    /// EF Core tự detect changes.
    /// Update() dùng khi entity KHÔNG được track (vd: nhận từ API request) → attach và mark Modified.
    /// </summary>
    public Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Xóa entity.
    /// Remove() mark entity là "Deleted" trong Change Tracker.
    /// SQL DELETE chạy khi SaveChangesAsync().
    /// </summary>
    public Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Kiểm tra entity tồn tại theo Id.
    /// AnyAsync() hiệu quả hơn FindAsync() vì nó generate SQL:
    ///   SELECT CASE WHEN EXISTS (SELECT 1 FROM Users WHERE Id = @id) THEN 1 ELSE 0 END
    /// → Không load toàn bộ entity, chỉ check có hay không.
    /// </summary>
    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbSet.AnyAsync(e => e.Id == id);
    }
}
