namespace TaskFlow.Application.DTOs;

// ===== AUTH DTOs =====

public record RegisterDto(string FullName, string Email, string Password);

public record LoginDto(string Email, string Password);

public record TokenDto(string AccessToken, string RefreshToken);

public record RefreshTokenDto(string RefreshToken);
