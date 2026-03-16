using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Auth.Commands.Login;

/// <summary>
/// Handler xử lý đăng nhập.
/// 
/// So sánh với RegisterCommandHandler:
/// - Register: tạo user MỚI → AddAsync
/// - Login: tìm user CŨ → verify password → UpdateAsync
/// 
/// Cùng pattern IRequestHandler, chỉ khác logic bên trong.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, TokenDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<TokenDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm user theo email
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user is null)
        {
            // Bảo mật: KHÔNG nói "email không tồn tại" vì hacker sẽ biết email nào có trong hệ thống
            // Luôn trả message chung chung
            throw new UnauthorizedException("Invalid email or password.");
        }

        // 2. Verify password
        var isValidPassword = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!isValidPassword)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        // 3. Generate JWT tokens mới
        var tokens = _tokenService.GenerateTokens(user);

        // 4. Cập nhật refresh token trong DB
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return tokens;
    }
}
