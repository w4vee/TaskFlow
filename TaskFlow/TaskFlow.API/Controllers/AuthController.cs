using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Features.Auth.Commands.Login;
using TaskFlow.Application.Features.Auth.Commands.RefreshToken;
using TaskFlow.Application.Features.Auth.Commands.Register;

namespace TaskFlow.API.Controllers;

/// <summary>
/// AuthController - xử lý Authentication (Register, Login, Refresh Token).
/// 
/// [ApiController]: attribute giúp ASP.NET Core tự động:
///   1. Model binding từ request body (JSON → DTO) 
///   2. Trả 400 nếu model invalid (trước khi vào action)
///   3. Không cần [FromBody] cho complex types
/// 
/// [Route("api/[controller]")]: URL = api/auth (tên controller bỏ "Controller")
/// 
/// ControllerBase vs Controller:
///   - Controller: có View support (cho MVC với Razor pages)
///   - ControllerBase: chỉ API, không có View → dùng cho Web API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    // IMediator được inject từ DI (đã đăng ký trong AddApplication)
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Đăng ký tài khoản mới
    /// </summary>
    /// <remarks>
    /// Tạo user mới và trả về JWT access token + refresh token.
    /// 
    /// Sample request:
    ///
    ///     POST /api/auth/register
    ///     {
    ///         "fullName": "Nguyen Van A",
    ///         "email": "user@example.com",
    ///         "password": "MyPassword123"
    ///     }
    ///
    /// </remarks>
    /// <response code="201">Đăng ký thành công, trả về tokens</response>
    /// <response code="400">Email đã tồn tại</response>
    /// <response code="422">Validation failed (email/password không hợp lệ)</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TokenDto>> Register(RegisterDto dto)
    {
        var command = new RegisterCommand(dto.FullName, dto.Email, dto.Password);
        var result = await _mediator.Send(command);
        return StatusCode(201, result);
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    /// <remarks>
    /// Xác thực email + password, trả về JWT tokens.
    ///
    ///     POST /api/auth/login
    ///     {
    ///         "email": "user@example.com",
    ///         "password": "MyPassword123"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Đăng nhập thành công</response>
    /// <response code="401">Sai email hoặc password</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenDto>> Login(LoginDto dto)
    {
        var command = new LoginCommand(dto.Email, dto.Password);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Refresh token - lấy cặp token mới
    /// </summary>
    /// <remarks>
    /// Gửi refresh token trong body + access token (có thể đã hết hạn) trong Authorization header.
    /// Server sẽ verify và trả về cặp token mới (Token Rotation).
    ///
    ///     POST /api/auth/refresh-token
    ///     Header: Authorization: Bearer {expired-access-token}
    ///     Body: { "refreshToken": "your-refresh-token" }
    ///
    /// </remarks>
    /// <response code="200">Token mới được tạo thành công</response>
    /// <response code="401">Refresh token không hợp lệ hoặc đã hết hạn</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenDto>> RefreshToken(RefreshTokenDto dto)
    {
        // Lấy access token từ header Authorization: Bearer <token>
        var accessToken = Request.Headers["Authorization"]
            .ToString().Replace("Bearer ", "");

        var command = new RefreshTokenCommand(accessToken, dto.RefreshToken);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Helper method: lấy UserId từ JWT token.
    /// </summary>
    protected Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdString!);
    }
}
