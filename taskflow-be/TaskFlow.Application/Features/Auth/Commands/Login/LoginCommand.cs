using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command đăng nhập - nhận Email + Password, trả về TokenDto.
/// 
/// Tại sao Login là Command mà không phải Query?
/// Vì Login THAY ĐỔI dữ liệu trong DB (cập nhật RefreshToken).
/// Rule: nếu có side effect (thay đổi state) → Command, không có → Query.
/// </summary>
public record LoginCommand(
    string Email,
    string Password
) : IRequest<TokenDto>;
