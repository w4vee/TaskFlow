namespace TaskFlow.Application.Common.Exceptions;

/// <summary>
/// Throw khi không tìm thấy entity trong database.
/// Middleware sẽ map exception này → HTTP 404 Not Found.
/// 
/// Ví dụ: throw new NotFoundException("TaskBoard", boardId);
/// → Message: "Entity 'TaskBoard' with Id 'xxx' was not found."
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity '{name}' with Id '{key}' was not found.")
    {
    }
}
