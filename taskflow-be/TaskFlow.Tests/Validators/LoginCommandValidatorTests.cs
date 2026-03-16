using FluentAssertions;
using FluentValidation.TestHelper;
using TaskFlow.Application.Features.Auth.Commands.Login;

namespace TaskFlow.Tests.Validators;

/// <summary>
/// Unit Tests cho LoginCommandValidator.
/// 
/// Login validator đơn giản hơn Register:
/// - Email: required + format
/// - Password: required only (không check min/max vì user đã có password rồi)
/// </summary>
public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    public LoginCommandValidatorTests()
    {
        _validator = new LoginCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        var command = new LoginCommand("huy@test.com", "AnyPassword");
        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenEmailIsEmpty(string email)
    {
        var command = new LoginCommand(email, "Password123");
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void Validate_ShouldFail_WhenEmailFormatIsInvalid(string email)
    {
        var command = new LoginCommand(email, "Password123");
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email format is invalid.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenPasswordIsEmpty(string password)
    {
        var command = new LoginCommand("huy@test.com", password);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }
}
