using FluentAssertions;
using FluentValidation.TestHelper;
using TaskFlow.Application.Features.Boards.Commands.CreateBoard;

namespace TaskFlow.Tests.Validators;

/// <summary>
/// Unit Tests cho CreateBoardCommandValidator.
/// 
/// Rules:
/// - Name: required, max 200 chars
/// - Description: optional, max 500 chars
/// - OwnerId: required (not empty Guid)
/// </summary>
public class CreateBoardCommandValidatorTests
{
    private readonly CreateBoardCommandValidator _validator;

    public CreateBoardCommandValidatorTests()
    {
        _validator = new CreateBoardCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        var command = new CreateBoardCommand("Sprint 1", "First sprint", Guid.NewGuid());
        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldPass_WhenDescriptionIsNull()
    {
        // Description is optional
        var command = new CreateBoardCommand("Sprint 1", null, Guid.NewGuid());
        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== NAME VALIDATION =====

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenNameIsEmpty(string name)
    {
        var command = new CreateBoardCommand(name, "Desc", Guid.NewGuid());
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Board name is required.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameExceeds200Characters()
    {
        var longName = new string('a', 201);
        var command = new CreateBoardCommand(longName, "Desc", Guid.NewGuid());
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Board name must not exceed 200 characters.");
    }

    // ===== DESCRIPTION VALIDATION =====

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionExceeds500Characters()
    {
        var longDesc = new string('a', 501);
        var command = new CreateBoardCommand("Sprint 1", longDesc, Guid.NewGuid());
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 500 characters.");
    }

    // ===== OWNERID VALIDATION =====

    /// <summary>
    /// Guid.Empty = "00000000-0000-0000-0000-000000000000"
    /// Đây là "zero value" của Guid, tương tự 0 cho int, null cho object.
    /// FluentValidation .NotEmpty() reject Guid.Empty.
    /// </summary>
    [Fact]
    public void Validate_ShouldFail_WhenOwnerIdIsEmpty()
    {
        var command = new CreateBoardCommand("Sprint 1", "Desc", Guid.Empty);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.OwnerId)
            .WithErrorMessage("Owner ID is required.");
    }
}
