using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces;

/// <summary>
/// Interface for JWT token operations.
/// Defined in Application, implemented in Infrastructure.
/// This is Dependency Inversion in action.
/// </summary>
public interface ITokenService
{
    TokenDto GenerateTokens(User user);
    string? GetUserIdFromExpiredToken(string token);
}
