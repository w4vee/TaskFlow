using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration cho entity TaskBoard → bảng "TaskBoards".
/// 
/// Relationships trong EF Core:
/// - One-to-Many: 1 User owns nhiều Boards → HasMany/WithOne
/// - One-to-Many: 1 Board chứa nhiều Tasks → HasMany/WithOne
/// 
/// Khi define relationship ở 1 bên (vd: UserConfiguration),
/// EF Core tự hiểu bên kia. Nhưng ta vẫn config ở cả 2 cho rõ ràng,
/// đặc biệt khi cần custom OnDelete behavior.
/// </summary>
public class TaskBoardConfiguration : IEntityTypeConfiguration<TaskBoard>
{
    public void Configure(EntityTypeBuilder<TaskBoard> builder)
    {
        builder.ToTable("TaskBoards");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Description)
            .HasMaxLength(500);    // Nullable

        builder.Property(b => b.OwnerId)
            .IsRequired();

        // Index trên OwnerId - tăng tốc query "lấy boards của user X"
        // Khi query WHERE OwnerId = @userId, database dùng index thay vì scan toàn bảng
        builder.HasIndex(b => b.OwnerId);

        // Relationship: 1 Board có nhiều Tasks
        // Cascade Delete: xóa Board → tự xóa hết tasks trong board đó
        // (hợp lý vì tasks không có ý nghĩa nếu board bị xóa)
        builder.HasMany(b => b.Tasks)
            .WithOne(t => t.Board)
            .HasForeignKey(t => t.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
