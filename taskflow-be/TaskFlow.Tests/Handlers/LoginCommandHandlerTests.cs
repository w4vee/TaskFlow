using FluentAssertions;
using Moq;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Features.Auth.Commands.Login;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Tests.Handlers;

/// <summary>
/// Unit Tests cho LoginCommandHandler.
/// 
/// So sánh với RegisterCommandHandlerTests:
/// - Register: test tạo user MỚI → focus vào AddAsync, Hash
/// - Login: test tìm user CŨ → focus vào GetByEmailAsync, Verify password
/// 
/// Login có 3 kịch bản chính:
/// 1. Happy path: email đúng + password đúng → return tokens
/// 2. Email không tồn tại → throw UnauthorizedException
/// 3. Password sai → throw UnauthorizedException
/// 
/// Lưu ý bảo mật: cả 2 trường hợp lỗi (2 + 3) đều throw CÙNG message
/// "Invalid email or password" → hacker không biết email có tồn tại không.
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

        _handler = new LoginCommandHandler(
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object
        );
    }

    /// <summary>
    /// Helper method tạo fake User object.
    /// 
    /// Tại sao dùng helper? Vì nhiều test cần User giống nhau,
    /// tránh copy-paste code. DRY principle (Don't Repeat Yourself).
    /// </summary>
    private static User CreateFakeUser() => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Huy Le",
        Email = "huy@test.com",
        PasswordHash = "existing_hashed_password",
        Role = UserRole.User
    };

    [Fact]
    public async Task Handle_ShouldReturnTokenDto_WhenCredentialsAreValid()
    {
        // Arrange
        var command = new LoginCommand("huy@test.com", "CorrectPassword");
        var existingUser = CreateFakeUser();
        var expectedTokens = new TokenDto("new_access_token", "new_refresh_token");

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("huy@test.com"))
            .ReturnsAsync(existingUser);

        _passwordHasherMock
            .Setup(h => h.Verify("CorrectPassword", "existing_hashed_password"))
            .Returns(true);  // Password đúng!

        _tokenServiceMock
            .Setup(t => t.GenerateTokens(existingUser))
            .Returns(expectedTokens);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");

        // Verify: UpdateAsync được gọi để lưu refresh token mới
        _userRepoMock.Verify(r => r.UpdateAsync(existingUser), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenEmailNotFound()
    {
        // Arrange
        var command = new LoginCommand("notexist@test.com", "AnyPassword");

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("notexist@test.com"))
            .ReturnsAsync((User?)null);  // Email không tồn tại → return null

        // Act & Assert
        await FluentActions
            .Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");

        // Verify: KHÔNG được gọi Verify password (vì user null)
        _passwordHasherMock.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenPasswordIsWrong()
    {
        // Arrange
        var command = new LoginCommand("huy@test.com", "WrongPassword");
        var existingUser = CreateFakeUser();

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("huy@test.com"))
            .ReturnsAsync(existingUser);

        _passwordHasherMock
            .Setup(h => h.Verify("WrongPassword", "existing_hashed_password"))
            .Returns(false);  // Password SAI!

        // Act & Assert
        await FluentActions
            .Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");

        // Verify: KHÔNG được gọi GenerateTokens (password sai thì không cấp token)
        _tokenServiceMock.Verify(t => t.GenerateTokens(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    /// <summary>
    /// Test: Refresh token trong DB phải được cập nhật sau login.
    /// 
    /// Tại sao quan trọng?
    /// Token Rotation: mỗi lần login → refresh token MỚI.
    /// Nếu hacker có refresh token cũ → không dùng được nữa.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldUpdateRefreshToken_WhenLoginSuccessful()
    {
        // Arrange
        var command = new LoginCommand("huy@test.com", "CorrectPassword");
        var existingUser = CreateFakeUser();
        existingUser.RefreshToken = "old_refresh_token";  // Token cũ

        var newTokens = new TokenDto("at", "brand_new_refresh_token");

        _userRepoMock
            .Setup(r => r.GetByEmailAsync("huy@test.com"))
            .ReturnsAsync(existingUser);

        _passwordHasherMock
            .Setup(h => h.Verify("CorrectPassword", existingUser.PasswordHash))
            .Returns(true);

        _tokenServiceMock
            .Setup(t => t.GenerateTokens(existingUser))
            .Returns(newTokens);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: User entity phải có refresh token MỚI
        existingUser.RefreshToken.Should().Be("brand_new_refresh_token");
        existingUser.RefreshTokenExpiryTime.Should().NotBeNull();
        existingUser.RefreshTokenExpiryTime.Should().BeAfter(DateTime.UtcNow);
    }
}
