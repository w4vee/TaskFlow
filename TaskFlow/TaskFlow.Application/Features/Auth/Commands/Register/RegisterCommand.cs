using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Features.Auth.Commands.Register;

/// <summary>
/// COMMAND = "message" chứa dữ liệu input cho 1 use case.
/// 
/// IRequest&lt;TokenDto&gt; nghĩa là:
/// "Khi Command này được Send qua MediatR, kết quả trả về là TokenDto"
/// 
/// Dùng record vì Command là immutable (không thay đổi sau khi tạo).
/// Record tự generate constructor, Equals, GetHashCode, ToString.
/// 
/// Flow: Controller tạo RegisterCommand → Send qua MediatR → MediatR tìm Handler → Handler xử lý → Return TokenDto
/// </summary>
public record RegisterCommand(
    string FullName,
    string Email,
    string Password
) : IRequest<TokenDto>;
