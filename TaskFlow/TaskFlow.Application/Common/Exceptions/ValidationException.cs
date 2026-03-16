namespace TaskFlow.Application.Common.Exceptions;

/// <summary>
/// Throw khi FluentValidation phát hiện lỗi validation.
/// Chứa dictionary các lỗi theo từng field.
/// Middleware sẽ map exception này → HTTP 422 Unprocessable Entity.
/// 
/// Ví dụ response:
/// {
///   "errors": {
///     "Email": ["Email is required", "Email format is invalid"],
///     "Password": ["Password must be at least 6 characters"]
///   }
/// }
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
