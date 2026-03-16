using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.DTOs;

// ===== USER DTOs =====

public record UserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public DateTime CreatedAt { get; init; }
}
