namespace TaskFlow.Application.Common.Exceptions;

/// <summary>
/// Throw khi request không hợp lệ (logic error, không phải validation error).
/// Middleware sẽ map exception này → HTTP 400 Bad Request.
/// 
/// Ví dụ: throw new BadRequestException("Email already exists");
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message)
        : base(message)
    {
    }
}
