using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Services;

namespace TaskFlow.Tests.Services;

/// <summary>
/// Unit Tests cho TokenService (JWT implementation).
/// 
/// === SETUP ĐẶC BIỆT: Fake IConfiguration ===
/// 
/// TokenService đọc JWT settings từ IConfiguration (appsettings.json).
/// Trong test, ta KHÔNG đọc file thật → tạo fake config bằng:
///   new ConfigurationBuilder().AddInMemoryCollection(...)
/// 
/// Đây là pattern phổ biến khi test service cần IConfiguration.
/// InMemoryCollection = Dictionary giả làm appsettings.json.
/// 
/// === INTEGRATION TEST vs UNIT TEST ===
/// 
/// TokenService test này hơi nghiêng về Integration test vì:
/// - Dùng real JWT library (System.IdentityModel.Tokens.Jwt)
/// - Dùng real crypto (HMAC-SHA256)
/// 
/// Nhưng vẫn là unit test vì:
/// - Không cần database, HTTP, external service
/// - Chạy nhanh, isolated, deterministic
/// 
/// Ranh giới unit/integration test đôi khi mờ. Quan trọng là test CHẠY NHANH
/// và KHÔNG phụ thuộc external systems → vẫn OK.
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;

    public TokenServiceTests()
    {
        // Tạo fake IConfiguration với JWT settings
        // Key phải >= 32 chars cho HMAC-SHA256 (256 bits = 32 bytes)
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "ThisIsASuperSecretKeyForTesting1234567890!@#$",  // >= 32 chars
            ["Jwt:Issuer"] = "TaskFlowTest",
            ["Jwt:Audience"] = "TaskFlowTestApp",
            ["Jwt:AccessTokenExpirationMinutes"] = "15"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    /// <summary>
    /// Helper tạo fake User cho tests.
    /// </summary>
    private static User CreateTestUser() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        FullName = "Test User",
        Email = "test@example.com",
        PasswordHash = "hashed",
        Role = UserRole.User
    };

    [Fact]
    public void GenerateTokens_ShouldReturnBothTokens()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = _tokenService.GenerateTokens(user);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test: Access Token phải là valid JWT format.
    /// JWT có 3 phần cách nhau bởi dấu chấm: header.payload.signature
    /// </summary>
    [Fact]
    public void GenerateTokens_ShouldReturnValidJwtAccessToken()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = _tokenService.GenerateTokens(user);

        // Assert: JWT format = xxx.yyy.zzz (3 parts separated by dots)
        var parts = result.AccessToken.Split('.');
        parts.Should().HaveCount(3);

        // Verify ta có thể decode JWT
        var handler = new JwtSecurityTokenHandler();
        var canRead = handler.CanReadToken(result.AccessToken);
        canRead.Should().BeTrue();
    }

    /// <summary>
    /// Test: JWT chứa đúng Claims (UserId, Email, Role).
    /// 
    /// Claims là thông tin được encode trong token.
    /// Nếu claims sai → API không biết user là ai, role gì → authorization lỗi.
    /// </summary>
    [Fact]
    public void GenerateTokens_ShouldContainCorrectClaims()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = _tokenService.GenerateTokens(user);

        // Decode JWT để đọc claims
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.AccessToken);

        // Assert
        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier &&
            c.Value == user.Id.ToString());

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Email &&
            c.Value == "test@example.com");

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role &&
            c.Value == "User");
    }

    /// <summary>
    /// Test: JWT expiration đúng config (15 phút).
    /// </summary>
    [Fact]
    public void GenerateTokens_ShouldSetCorrectExpiration()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = _tokenService.GenerateTokens(user);

        // Decode JWT
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.AccessToken);

        // Assert: token hết hạn trong khoảng 14-16 phút từ bây giờ
        // (cho phép sai lệch vài giây do thời gian chạy code)
        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        jwt.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Test: Refresh Token là random string (Base64), KHÔNG phải JWT.
    /// Mỗi lần generate phải ra giá trị KHÁC NHAU.
    /// </summary>
    [Fact]
    public void GenerateTokens_ShouldReturnUniqueRefreshTokens()
    {
        var user = CreateTestUser();

        var result1 = _tokenService.GenerateTokens(user);
        var result2 = _tokenService.GenerateTokens(user);

        result1.RefreshToken.Should().NotBe(result2.RefreshToken);
    }

    /// <summary>
    /// Test: GetUserIdFromExpiredToken trả về đúng UserId.
    /// 
    /// Flow: GenerateTokens → lấy AccessToken → GetUserIdFromExpiredToken
    /// → verify trả về đúng UserId.
    /// </summary>
    [Fact]
    public void GetUserIdFromExpiredToken_ShouldReturnUserId_WhenTokenIsValid()
    {
        // Arrange
        var user = CreateTestUser();
        var tokens = _tokenService.GenerateTokens(user);

        // Act
        var userId = _tokenService.GetUserIdFromExpiredToken(tokens.AccessToken);

        // Assert
        userId.Should().Be(user.Id.ToString());
    }

    /// <summary>
    /// Test: GetUserIdFromExpiredToken trả về null khi token invalid.
    /// </summary>
    [Fact]
    public void GetUserIdFromExpiredToken_ShouldReturnNull_WhenTokenIsInvalid()
    {
        // Act
        var userId = _tokenService.GetUserIdFromExpiredToken("totally.invalid.token");

        // Assert
        userId.Should().BeNull();
    }

    /// <summary>
    /// Test: Hai user khác nhau phải ra access token KHÁC nhau.
    /// (Vì claims khác nhau: userId, email, role).
    /// </summary>
    [Fact]
    public void GenerateTokens_ShouldReturnDifferentTokens_ForDifferentUsers()
    {
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user1@test.com",
            FullName = "User 1",
            PasswordHash = "hash1",
            Role = UserRole.User
        };

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user2@test.com",
            FullName = "User 2",
            PasswordHash = "hash2",
            Role = UserRole.Admin
        };

        var tokens1 = _tokenService.GenerateTokens(user1);
        var tokens2 = _tokenService.GenerateTokens(user2);

        tokens1.AccessToken.Should().NotBe(tokens2.AccessToken);
    }
}
