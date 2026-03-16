using FluentValidation;
using TaskFlow.Application.Features.Auth.Commands.Register;

namespace TaskFlow.Application.Features.Auth.Commands.Register;

/// <summary>
/// Validator cho RegisterCommand.
/// 
/// AbstractValidator&lt;T&gt;: class base của FluentValidation.
/// Rules được khai báo trong constructor.
/// 
/// Validator này sẽ chạy TỰ ĐỘNG trước Handler nhờ ValidationBehavior
/// (MediatR Pipeline Behavior mà ta sẽ tạo sau).
/// 
/// Nếu có lỗi → throw ValidationException → API trả 422 kèm chi tiết lỗi.
/// Handler KHÔNG BAO GIỜ được gọi nếu validation fail.
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
            .MaximumLength(50).WithMessage("Password must not exceed 50 characters.");
    }
}
