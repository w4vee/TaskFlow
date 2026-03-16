using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration cho entity User → bảng "Users" trong database.
/// 
/// IEntityTypeConfiguration<T> cho phép tách config ra file riêng.
/// Mỗi entity có 1 file config → dễ quản lý, dễ đọc.
/// 
/// So sánh Fluent API vs Data Annotations:
/// 
/// Data Annotations (KHÔNG dùng trong Clean Architecture):
///   [Required]
///   [MaxLength(100)]
///   public string FullName { get; set; }
///   → Pha trộn database concerns vào Domain entity
/// 
/// Fluent API (dùng trong Clean Architecture):
///   builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
///   → Domain entity sạch sẽ, database config nằm ở Infrastructure
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Tên bảng trong database
        builder.ToTable("Users");

        // Primary Key - EF Core tự detect property "Id" là PK,
        // nhưng explicit config cho rõ ràng
        builder.HasKey(u => u.Id);

        // Property configurations
        builder.Property(u => u.FullName)
            .IsRequired()          // NOT NULL trong SQL
            .HasMaxLength(100);    // NVARCHAR(100)

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);    // BCrypt hash dài ~60 chars, để 500 cho an toàn

        // Enum → int trong database
        // HasConversion<int>() lưu enum dưới dạng số (0, 1)
        // Nếu muốn lưu string "User", "Admin" thì dùng HasConversion<string>()
        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500);    // Nullable by default (string?)

        builder.Property(u => u.RefreshTokenExpiryTime);  // Nullable DateTime

        // Index - tạo UNIQUE INDEX trên Email
        // Đảm bảo không có 2 user cùng email ở database level
        // (ngoài việc check trong code, database cũng enforce)
        builder.HasIndex(u => u.Email)
            .IsUnique();

        // Relationships - 1 User có nhiều OwnedBoards
        // HasMany: User có nhiều TaskBoard
        // WithOne: Mỗi TaskBoard có 1 Owner (User)
        // HasForeignKey: FK là OwnerId trong TaskBoard
        // OnDelete Restrict: Không cho xóa User nếu còn boards
        //   (Restrict vs Cascade: Cascade = xóa User → tự xóa hết boards.
        //    Restrict = phải xóa boards trước rồi mới xóa được User → an toàn hơn)
        builder.HasMany(u => u.OwnedBoards)
            .WithOne(b => b.Owner)
            .HasForeignKey(b => b.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1 User có nhiều AssignedTasks (nullable FK)
        builder.HasMany(u => u.AssignedTasks)
            .WithOne(t => t.AssignedTo)
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);  // Xóa User → task.AssignedToId = NULL (unassign)
    }
}
