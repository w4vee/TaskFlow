using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Services;

/// <summary>
/// TokenService - generate và validate JWT tokens.
/// 
/// Flow hoạt động:
/// 1. User Login → GenerateTokens() → trả về { AccessToken, RefreshToken }
/// 2. Client gửi AccessToken trong header: Authorization: Bearer eyJhbGci...
/// 3. API middleware validate AccessToken → cho phép access
/// 4. AccessToken hết hạn (15 phút) → Client gửi { AccessToken cũ, RefreshToken }
/// 5. GetUserIdFromExpiredToken() → extract userId từ token cũ (dù đã expired)
/// 6. Verify RefreshToken match trong DB → GenerateTokens() mới → Token Rotation
/// 
/// JWT Structure:
/// eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U
/// |      Header       |       Payload              |              Signature                              |
/// 
/// Claims trong Payload:
/// - Sub (Subject): UserId → để biết "ai" đang request
/// - Email: email user
/// - Role: "User" hoặc "Admin" → cho authorization
/// - Jti (JWT ID): unique ID cho mỗi token → có thể dùng để blacklist
/// - Exp (Expiration): thời điểm hết hạn
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generate cả AccessToken (JWT) và RefreshToken (random string).
    /// </summary>
    public TokenDto GenerateTokens(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        return new TokenDto(accessToken, refreshToken);
    }

    /// <summary>
    /// Extract UserId từ AccessToken ĐÃ HẾT HẠN.
    /// 
    /// Tại sao cần validate expired token?
    /// Khi user gọi /refresh-token, AccessToken đã expired.
    /// Ta vẫn cần biết token đó thuộc user nào → extract UserId.
    /// 
    /// ValidateLifetime = false: cho phép token hết hạn vẫn validate được.
    /// Các validate khác (issuer, audience, signature) vẫn check bình thường.
    /// </summary>
    public string? GetUserIdFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,  // QUAN TRỌNG: không check expiry
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!))
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            // Verify token dùng đúng algorithm HmacSha256
            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            // Extract UserId từ claim "sub" (Subject)
            return principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        catch
        {
            return null;  // Token invalid → return null
        }
    }

    /// <summary>
    /// Generate JWT Access Token.
    /// 
    /// Claims = thông tin được encode trong token:
    /// - NameIdentifier: UserId (Guid) → dùng để identify user trong mỗi request
    /// - Email: email user
    /// - Role: cho role-based authorization ([Authorize(Roles = "Admin")])
    /// - Jti: unique token ID
    /// 
    /// SigningCredentials: dùng Secret Key + HMAC-SHA256 để ký token.
    /// Ai có Secret Key mới tạo/verify được token → Key PHẢI giữ bí mật!
    /// </summary>
    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Đọc expiration từ config (default 15 phút)
        var expirationMinutes = int.Parse(
            _configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generate Refresh Token - random string, KHÔNG phải JWT.
    /// 
    /// Dùng RandomNumberGenerator (cryptographically secure) thay vì Random.
    /// Random không đủ random cho security purposes.
    /// Convert sang Base64 string để lưu trong database.
    /// </summary>
    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
