using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Handler xử lý refresh token.
/// 
/// Flow phức tạp hơn Register/Login:
/// 1. Đọc UserId từ access token ĐÃ HẾT HẠN (GetUserIdFromExpiredToken)
/// 2. Tìm user trong DB
/// 3. Verify: refresh token client gửi lên có KHỚP với refresh token trong DB không?
/// 4. Verify: refresh token đã hết hạn chưa?
/// 5. Generate cặp token mới (cả access + refresh)
/// 6. Cập nhật refresh token mới vào DB
/// 
/// Đây gọi là "Token Rotation" - mỗi lần refresh, cả 2 token đều đổi mới.
/// Nếu ai đó đánh cắp refresh token cũ và dùng lại → sẽ fail vì DB đã lưu token mới.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<TokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Đọc UserId từ access token đã hết hạn
        // TokenService sẽ parse token mà KHÔNG validate expiry time
        var userId = _tokenService.GetUserIdFromExpiredToken(request.AccessToken);
        if (userId is null)
        {
            throw new UnauthorizedException("Invalid access token.");
        }

        // 2. Tìm user trong DB
        var user = await _unitOfWork.Users.GetByIdAsync(Guid.Parse(userId));
        if (user is null)
        {
            throw new UnauthorizedException("User not found.");
        }

        // 3. Verify refresh token khớp với DB
        if (user.RefreshToken != request.RefreshToken)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        // 4. Check refresh token hết hạn chưa
        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token has expired. Please login again.");
        }

        // 5. Generate cặp token mới (Token Rotation)
        var newTokens = _tokenService.GenerateTokens(user);

        // 6. Cập nhật refresh token mới vào DB
        user.RefreshToken = newTokens.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return newTokens;
    }
}
