namespace TaskFlow.Application.DTOs;

// ===== BOARD DTOs =====

/// <summary>
/// Output DTO cho TaskBoard.
/// 
/// Tại sao dùng { get; init; } thay vì positional record TaskBoardDto(...)?
/// 
/// Positional record tạo constructor BẮT BUỘC tất cả params:
///   new TaskBoardDto(id, name, desc, ownerId, createdAt, taskCount)
/// 
/// Vấn đề: AutoMapper khi map sẽ cố dùng constructor đó.
/// Nhưng TaskCount là computed value (từ .ForMember), không phải property 
/// trực tiếp của TaskBoard → AutoMapper không truyền được → Exception!
/// 
/// Giải pháp: Dùng init properties → AutoMapper dùng parameterless constructor
/// rồi gán từng property riêng biệt (kể cả computed values).
/// init = chỉ gán được lúc khởi tạo → vẫn immutable như record gốc.
/// </summary>
public record TaskBoardDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid OwnerId { get; init; }
    public DateTime CreatedAt { get; init; }
    public int TaskCount { get; init; }
}

public record CreateBoardDto(string Name, string? Description);

public record UpdateBoardDto(string Name, string? Description);
