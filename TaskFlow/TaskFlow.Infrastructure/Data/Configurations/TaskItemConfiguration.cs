using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration cho entity TaskItem → bảng "TaskItems".
/// 
/// TaskItem có 2 Foreign Keys:
/// - BoardId (required) → TaskBoard (mỗi task thuộc 1 board)
/// - AssignedToId (nullable) → User (task có thể chưa assign cho ai)
/// 
/// Nullable FK trong EF Core:
/// - Property kiểu Guid? (nullable) + Navigation kiểu User? (nullable)
/// - EF Core tự hiểu đây là optional relationship
/// - Khi AssignedToId = null → task chưa được assign
/// </summary>
public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);   // Nullable

        // Enum → int: lưu 0 (Todo), 1 (InProgress), 2 (Done)
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<int>();

        // Enum → int: lưu 0 (Low), 1 (Medium), 2 (High), 3 (Critical)
        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.Deadline);   // Nullable DateTime

        builder.Property(t => t.IsOverdue)
            .IsRequired()
            .HasDefaultValue(false);  // DEFAULT 0 trong SQL

        builder.Property(t => t.BoardId)
            .IsRequired();

        // Indexes - tăng tốc các query thường dùng
        builder.HasIndex(t => t.BoardId);          // Lấy tasks theo board
        builder.HasIndex(t => t.AssignedToId);     // Lấy tasks theo assigned user
        builder.HasIndex(t => t.Status);           // Lọc tasks theo status (Kanban columns)
        builder.HasIndex(t => t.IsOverdue);        // Query overdue tasks (Hangfire job)

        // Note: Relationships (Board, AssignedTo) đã được config ở
        // TaskBoardConfiguration và UserConfiguration rồi.
        // EF Core chỉ cần config relationship ở 1 bên.
        // Nếu config ở cả 2 bên mà conflict → sẽ lỗi.
    }
}
