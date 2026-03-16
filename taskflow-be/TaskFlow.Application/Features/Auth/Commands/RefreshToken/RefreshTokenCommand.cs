using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Command refresh token - gửi access token cũ (đã hết hạn) + refresh token
/// để nhận cặp token mới.
/// </summary>
public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken
) : IRequest<TokenDto>;
