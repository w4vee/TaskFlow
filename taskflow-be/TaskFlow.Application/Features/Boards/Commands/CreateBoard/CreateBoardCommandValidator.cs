using FluentValidation;

namespace TaskFlow.Application.Features.Boards.Commands.CreateBoard;

public class CreateBoardCommandValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Board name is required.")
            .MaximumLength(200).WithMessage("Board name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required.");
    }
}
