using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data;

/// <summary>
/// AppDbContext - "cầu nối" giữa C# code và SQL Server database.
/// 
/// DbContext làm gì?
/// 1. Quản lý connection tới database
/// 2. Map entity classes → database tables (via DbSet)
/// 3. Track changes trên entities (Change Tracking)
/// 4. Generate SQL queries (LINQ → SQL)
/// 5. Handle migrations (tạo/update schema database)
/// 
/// Mỗi DbSet<T> = 1 bảng trong database.
/// Khi ta gọi _context.Users.Add(user) → EF Core track entity đó.
/// Khi gọi SaveChangesAsync() → EF Core generate INSERT SQL và execute.
/// </summary>
public class AppDbContext : DbContext
{
    // Constructor nhận DbContextOptions - được inject từ DI container
    // Options chứa connection string, database provider (SQL Server), etc.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSet = Table trong database
    // EF Core sẽ tạo bảng "Users", "TaskBoards", "TaskItems" tương ứng
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskBoard> TaskBoards => Set<TaskBoard>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    /// <summary>
    /// OnModelCreating - nơi cấu hình database schema bằng Fluent API.
    /// 
    /// ApplyConfigurationsFromAssembly() sẽ tự động scan assembly này,
    /// tìm tất cả class implement IEntityTypeConfiguration<T>,
    /// và apply config cho từng entity.
    /// 
    /// Giống như MediatR scan Assembly để tìm Handlers,
    /// EF Core scan Assembly để tìm Entity Configurations.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tự động apply tất cả IEntityTypeConfiguration trong assembly này
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// Override SaveChangesAsync để tự động set CreatedAt/UpdatedAt.
    /// 
    /// Change Tracker: EF Core theo dõi state của mỗi entity:
    /// - Added: entity mới được Add() → sẽ INSERT
    /// - Modified: property bị thay đổi → sẽ UPDATE
    /// - Deleted: entity bị Remove() → sẽ DELETE
    /// - Unchanged: không thay đổi gì → bỏ qua
    /// 
    /// Trước khi SaveChanges, ta duyệt qua tất cả entity đang tracked,
    /// tự động gán CreatedAt (khi Added) và UpdatedAt (khi Modified).
    /// → Không cần set thủ công trong mỗi Handler/Repository.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
