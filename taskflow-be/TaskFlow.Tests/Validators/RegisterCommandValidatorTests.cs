using FluentAssertions;
using FluentValidation.TestHelper;
using TaskFlow.Application.Features.Auth.Commands.Register;

namespace TaskFlow.Tests.Validators;

/// <summary>
/// Unit Tests cho RegisterCommandValidator.
/// 
/// === VALIDATOR TEST KHÁC GÌ HANDLER TEST? ===
/// 
/// Handler test: mock dependencies → test business logic
/// Validator test: KHÔNG mock gì → test validation rules trực tiếp
/// 
/// FluentValidation cung cấp TestValidate() helper:
/// - validator.TestValidate(command) → trả về TestValidationResult
/// - result.ShouldNotHaveAnyValidationErrors() → OK
/// - result.ShouldHaveValidationErrorFor(x => x.Email) → Có lỗi ở Email
/// 
/// === [THEORY] vs [FACT] ===
/// 
/// [Fact]: 1 test case cố định (không có parameter)
/// [Theory]: NHIỀU test cases với DATA KHÁC NHAU (parameterized test)
/// 
/// [Theory] + [InlineData]: giống test cùng 1 logic nhưng nhiều input
/// Ví dụ: test email invalid → thử "", "notanemail", "missing@", "@nodomain"
/// Thay vì viết 4 [Fact] methods giống nhau, dùng 1 [Theory] + 4 [InlineData].
/// </summary>
public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator;

    public RegisterCommandValidatorTests()
    {
        _validator = new RegisterCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var command = new RegisterCommand("Huy Le", "huy@test.com", "Password123");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== FULLNAME VALIDATION =====

    [Theory]
    [InlineData("")]       // Empty string
    [InlineData("   ")]    // Whitespace only
    public void Validate_ShouldFail_WhenFullNameIsEmpty(string fullName)
    {
        var command = new RegisterCommand(fullName, "huy@test.com", "Password123");
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name is required.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenFullNameExceeds100Characters()
    {
        // string('a', 101) = "aaa...a" (101 chars)
        var longName = new string('a', 101);
        var command = new RegisterCommand(longName, "huy@test.com", "Password123");
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name must not exceed 100 characters.");
    }

    // ===== EMAIL VALIDATION =====

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenEmailIsEmpty(string email)
    {
        var command = new RegisterCommand("Huy Le", email, "Password123");
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    /// <summary>
    /// [Theory] + [InlineData] = parameterized test.
    /// 
    /// Mỗi [InlineData] = 1 test case riêng.
    /// xUnit sẽ chạy method này 3 LẦN, mỗi lần với email khác nhau.
    /// 
    /// Trong Test Explorer (Visual Studio), mỗi InlineData hiện là 1 dòng riêng:
    ///   ✅ Validate_ShouldFail_WhenEmailFormatIsInvalid(email: "notanemail")
    ///   ✅ Validate_ShouldFail_WhenEmailFormatIsInvalid(email: "missing@")
    ///   ✅ Validate_ShouldFail_WhenEmailFormatIsInvalid(email: "@nodomain.com")
    /// </summary>
    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void Validate_ShouldFail_WhenEmailFormatIsInvalid(string email)
    {
        var command = new RegisterCommand("Huy Le", email, "Password123");
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email format is invalid.");
    }

    // ===== PASSWORD VALIDATION =====

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenPasswordIsEmpty(string password)
    {
        var command = new RegisterCommand("Huy Le", "huy@test.com", password);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsTooShort()
    {
        var command = new RegisterCommand("Huy Le", "huy@test.com", "12345");  // 5 chars < 6
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 6 characters.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsTooLong()
    {
        var longPassword = new string('a', 51);  // 51 chars > 50
        var command = new RegisterCommand("Huy Le", "huy@test.com", longPassword);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must not exceed 50 characters.");
    }

    /// <summary>
    /// Boundary test: password đúng 6 chars (minimum) → PASS.
    /// 
    /// Boundary testing = test ở ranh giới: 5 (fail), 6 (pass), 50 (pass), 51 (fail).
    /// Đây là kỹ thuật test quan trọng, hay gặp trong phỏng vấn.
    /// </summary>
    [Fact]
    public void Validate_ShouldPass_WhenPasswordIsExactly6Characters()
    {
        var command = new RegisterCommand("Huy Le", "huy@test.com", "123456");
        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
