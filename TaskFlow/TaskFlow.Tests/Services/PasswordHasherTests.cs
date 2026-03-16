using FluentAssertions;
using TaskFlow.Infrastructure.Services;

namespace TaskFlow.Tests.Services;

/// <summary>
/// Unit Tests cho PasswordHasher (BCrypt implementation).
/// 
/// === KHÁC BIỆT LỚN: KHÔNG dùng Mock! ===
/// 
/// Handler tests: mock dependencies → test business logic riêng
/// Service tests: dùng REAL implementation → test xem service hoạt động đúng chưa
/// 
/// PasswordHasher là "leaf" service (không phụ thuộc service khác),
/// nên test trực tiếp luôn, không cần mock.
/// 
/// === TẠI SAO TEST PASSWORD HASHER QUAN TRỌNG? ===
/// 
/// Nếu Hash/Verify bị lỗi:
/// - Hash lỗi → user không register được
/// - Verify lỗi → user không login được
/// - Security lỗi → password bị lưu plain text → data breach
/// 
/// Test này đảm bảo BCrypt wrapper hoạt động đúng.
/// </summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher;

    public PasswordHasherTests()
    {
        _hasher = new PasswordHasher();
    }

    [Fact]
    public void Hash_ShouldReturnHashedString_NotPlainText()
    {
        // Act
        var hash = _hasher.Hash("MyPassword123");

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("MyPassword123");  // Hash PHẢI khác plain text!
        hash.Should().StartWith("$2");          // BCrypt hash bắt đầu bằng $2a$ hoặc $2b$
    }

    /// <summary>
    /// BCrypt salt random → cùng password, 2 lần hash ra KẾT QUẢ KHÁC NHAU.
    /// 
    /// Tại sao quan trọng?
    /// Nếu hash giống nhau → hacker dùng rainbow table (bảng hash có sẵn) để dò.
    /// Salt random → mỗi user có hash khác nhau dù cùng password → rainbow table vô dụng.
    /// </summary>
    [Fact]
    public void Hash_ShouldProduceDifferentHashes_ForSamePassword()
    {
        // Act
        var hash1 = _hasher.Hash("SamePassword");
        var hash2 = _hasher.Hash("SamePassword");

        // Assert: 2 hash KHÁC nhau (do salt khác)
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatchesHash()
    {
        // Arrange
        var password = "CorrectPassword";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatchHash()
    {
        // Arrange
        var hash = _hasher.Hash("OriginalPassword");

        // Act
        var result = _hasher.Verify("WrongPassword", hash);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Test case sensitivity: "password" ≠ "Password" ≠ "PASSWORD".
    /// BCrypt phải phân biệt hoa thường.
    /// </summary>
    [Fact]
    public void Verify_ShouldBeCaseSensitive()
    {
        var hash = _hasher.Hash("Password123");

        _hasher.Verify("password123", hash).Should().BeFalse();
        _hasher.Verify("PASSWORD123", hash).Should().BeFalse();
        _hasher.Verify("Password123", hash).Should().BeTrue();
    }
}
