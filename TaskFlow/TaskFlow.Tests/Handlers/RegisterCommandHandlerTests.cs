using FluentAssertions;
using Moq;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Features.Auth.Commands.Register;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Tests.Handlers;

/// <summary>
/// Unit Tests cho RegisterCommandHandler.
/// 
/// === UNIT TEST LÀ GÌ? ===
/// Unit test = test 1 "unit" (đơn vị) code riêng lẻ, tách biệt.
/// Ở đây, unit = RegisterCommandHandler.
/// 
/// Ta KHÔNG test database, không test HTTP, không test JWT thật.
/// Ta chỉ test: "Với input X, Handler có XỬ LÝ ĐÚNG LOGIC không?"
/// 
/// === MOCK LÀ GÌ? ===
/// Mock = object giả, "diễn" như object thật nhưng ta KIỂM SOÁT hành vi.
/// 
/// Ví dụ thực tế:
/// - IUnitOfWork.Users.EmailExistsAsync("test@test.com") → ta bảo mock "return false"
/// - IPasswordHasher.Hash("123456") → ta bảo mock "return 'hashed_password'"
/// - ITokenService.GenerateTokens(user) → ta bảo mock "return new TokenDto(...)"
/// 
/// === ARRANGE - ACT - ASSERT (AAA Pattern) ===
/// Mỗi test method chia 3 phần:
/// 1. Arrange: Setup mock objects, tạo input data
/// 2. Act: Gọi method cần test (handler.Handle(...))
/// 3. Assert: Verify kết quả đúng không
/// 
/// === SO SÁNH VỚI SWALLET ===
/// SWallet không có unit tests. Trong thực tế, fresher biết viết test
/// là điểm CỘNG RẤT LỚN khi phỏng vấn. Nhiều company yêu cầu test coverage.
/// </summary>
public class RegisterCommandHandlerTests
{
    // Mock objects - khai báo ở class level để tái sử dụng giữa các test methods
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly RegisterCommandHandler _handler;

    /// <summary>
    /// Constructor chạy TRƯỚC MỖI test method (xUnit tạo instance mới cho mỗi test).
    /// Đây là nơi setup "sân khấu" cho tests.
    /// 
    /// Tại sao mock IUserRepository riêng?
    /// Vì IUnitOfWork.Users trả về IUserRepository.
    /// Ta cần mock cả 2: UnitOfWork (cha) và UserRepository (con).
    /// Rồi setup: unitOfWorkMock.Users → return userRepoMock
    /// </summary>
    public RegisterCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepoMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();

        // Kết nối: khi ai đó gọi _unitOfWork.Users → trả về _userRepoMock
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

        // Tạo handler với các mock dependencies
        _handler = new RegisterCommandHandler(
            _unitOfWorkMock.Object,       // .Object = lấy ra "object giả" từ Mock wrapper
            _passwordHasherMock.Object,
            _tokenServiceMock.Object
        );
    }

    /// <summary>
    /// Happy path test: Đăng ký thành công.
    /// 
    /// [Fact] = attribute đánh dấu đây là 1 test case (xUnit).
    /// Tương tự [TestMethod] trong MSTest hoặc [Test] trong NUnit.
    /// 
    /// Naming: Handle_ShouldReturnTokenDto_WhenEmailIsNew
    /// - Handle: method đang test
    /// - ShouldReturnTokenDto: kết quả mong đợi
    /// - WhenEmailIsNew: điều kiện test
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnTokenDto_WhenEmailIsNew()
    {
        // === ARRANGE ===
        var command = new RegisterCommand("Huy Le", "huy@test.com", "Password123");
        var expectedTokens = new TokenDto("access_token_123", "refresh_token_456");

        // Setup mock behaviors: "Khi ai gọi method X với param Y → return Z"
        _userRepoMock
            .Setup(r => r.EmailExistsAsync("huy@test.com"))
            .ReturnsAsync(false);  // Email chưa tồn tại → cho phép đăng ký

        _passwordHasherMock
            .Setup(h => h.Hash("Password123"))
            .Returns("hashed_password_xyz");

        // It.IsAny<User>() = "bất kỳ User nào" - vì ta chưa biết chính xác User object
        // Handler sẽ tạo User mới bên trong, ta không control được Id, CreatedAt...
        _tokenServiceMock
            .Setup(t => t.GenerateTokens(It.IsAny<User>()))
            .Returns(expectedTokens);

        // === ACT ===
        var result = await _handler.Handle(command, CancellationToken.None);

        // === ASSERT ===
        // FluentAssertions: đọc tự nhiên như tiếng Anh
        // "result should not be null"
        // "result.AccessToken should be expectedTokens.AccessToken"
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(expectedTokens.AccessToken);
        result.RefreshToken.Should().Be(expectedTokens.RefreshToken);

        // Verify mock methods ĐƯỢC GỌI đúng cách
        // Times.Once() = phải được gọi ĐÚNG 1 LẦN
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// Test case: Email đã tồn tại → throw BadRequestException.
    /// 
    /// Đây là negative test (test trường hợp LỖI).
    /// Quan trọng không kém happy path vì:
    /// - Đảm bảo handler KHÔNG tạo user trùng email
    /// - Đảm bảo throw đúng exception type
    /// - Đảm bảo message đúng
    /// </summary>
    [Fact]
    public async Task Handle_ShouldThrowBadRequestException_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new RegisterCommand("Huy Le", "existing@test.com", "Password123");

        _userRepoMock
            .Setup(r => r.EmailExistsAsync("existing@test.com"))
            .ReturnsAsync(true);  // Email ĐÃ tồn tại!

        // Act & Assert
        // FluentAssertions: "Awaiting this action should throw BadRequestException"
        // Invoking/Awaiting = wrapper để FluentAssertions bắt exception
        await FluentActions
            .Invoking(() => _handler.Handle(command, CancellationToken.None))
            .Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("Email already exists.");

        // Verify: KHÔNG được gọi AddAsync (không tạo user trùng email)
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    /// <summary>
    /// Test: User entity được tạo đúng với dữ liệu từ command.
    /// 
    /// Dùng Callback() của Moq để "bắt" (capture) User object mà Handler tạo ra.
    /// Rồi verify các properties của User đó.
    /// 
    /// Tại sao test này quan trọng?
    /// Đảm bảo Handler map đúng data từ Command → Entity:
    /// - FullName đúng
    /// - Email đúng  
    /// - PasswordHash = hashed version (KHÔNG phải plain text!)
    /// - Role = User (default)
    /// - RefreshToken được set
    /// </summary>
    [Fact]
    public async Task Handle_ShouldCreateUserWithCorrectData_WhenEmailIsNew()
    {
        // Arrange
        var command = new RegisterCommand("Huy Le", "huy@test.com", "Password123");
        var expectedTokens = new TokenDto("access_token", "refresh_token");
        User? capturedUser = null;  // Biến để "bắt" User được tạo

        _userRepoMock
            .Setup(r => r.EmailExistsAsync("huy@test.com"))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.Hash("Password123"))
            .Returns("bcrypt_hashed_value");

        _tokenServiceMock
            .Setup(t => t.GenerateTokens(It.IsAny<User>()))
            .Returns(expectedTokens);

        // Callback: khi AddAsync được gọi, "bắt" User argument lại
        _userRepoMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => capturedUser = user)
            .ReturnsAsync((User user) => user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: verify User entity đúng
        capturedUser.Should().NotBeNull();
        capturedUser!.FullName.Should().Be("Huy Le");
        capturedUser.Email.Should().Be("huy@test.com");
        capturedUser.PasswordHash.Should().Be("bcrypt_hashed_value");  // Hashed, KHÔNG phải "Password123"
        capturedUser.Role.Should().Be(UserRole.User);
        capturedUser.RefreshToken.Should().Be("refresh_token");
        capturedUser.RefreshTokenExpiryTime.Should().NotBeNull();
    }

    /// <summary>
    /// Test: Password PHẢI được hash trước khi lưu.
    /// 
    /// Đây là security test - cực kỳ quan trọng.
    /// Nếu ai đó vô tình xóa dòng _passwordHasher.Hash() trong Handler,
    /// test này sẽ FAIL → catch bug ngay.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldHashPassword_WhenRegistering()
    {
        // Arrange
        var command = new RegisterCommand("Huy Le", "huy@test.com", "MySecretPass");

        _userRepoMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(h => h.Hash("MySecretPass"))
            .Returns("hashed_value");

        _tokenServiceMock
            .Setup(t => t.GenerateTokens(It.IsAny<User>()))
            .Returns(new TokenDto("at", "rt"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: Hash PHẢI được gọi với password gốc
        _passwordHasherMock.Verify(h => h.Hash("MySecretPass"), Times.Once);
    }
}
