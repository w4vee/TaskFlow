using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Auth.Commands.Register;

/// <summary>
/// HANDLER = nơi chứa business logic thực sự.
/// 
/// IRequestHandler&lt;RegisterCommand, TokenDto&gt; nghĩa là:
/// "Tôi xử lý RegisterCommand và trả về TokenDto"
/// 
/// MediatR tự động tìm Handler này khi ai đó gọi:
///   await _mediator.Send(new RegisterCommand(...));
/// 
/// Handler nhận dependencies qua constructor injection (DI):
/// - IUnitOfWork: truy cập database
/// - IPasswordHasher: hash password  
/// - ITokenService: tạo JWT tokens
/// 
/// Lưu ý: Handler KHÔNG biết gì về HTTP, Controller, hay Request/Response.
/// Nó chỉ biết domain logic. Đây là sức mạnh của Clean Architecture.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, TokenDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public RegisterCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Handle = method chính mà MediatR gọi.
    /// 
    /// Flow:
    /// 1. Check email đã tồn tại chưa → throw BadRequestException nếu có
    /// 2. Hash password (KHÔNG BAO GIỜ lưu plain text password!)
    /// 3. Tạo User entity
    /// 4. Generate JWT tokens
    /// 5. Lưu refresh token vào User (để verify khi user refresh token sau này)
    /// 6. Save vào database
    /// 7. Return TokenDto chứa access token + refresh token
    /// </summary>
    public async Task<TokenDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Check email trùng
        var emailExists = await _unitOfWork.Users.EmailExistsAsync(request.Email);
        if (emailExists)
        {
            throw new BadRequestException("Email already exists.");
        }

        // 2. Hash password
        var passwordHash = _passwordHasher.Hash(request.Password);

        // 3. Tạo User entity
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = UserRole.User  // Mặc định role User, Admin tạo riêng
        };

        // 4. Generate JWT tokens (access token + refresh token)
        var tokens = _tokenService.GenerateTokens(user);

        // 5. Lưu refresh token vào user
        // Tại sao? Khi user gửi refresh token lên để lấy access token mới,
        // ta cần verify refresh token này có khớp với DB không.
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Refresh token sống 7 ngày

        // 6. Lưu user vào DB
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // 7. Return tokens cho client
        return tokens;
    }
}
