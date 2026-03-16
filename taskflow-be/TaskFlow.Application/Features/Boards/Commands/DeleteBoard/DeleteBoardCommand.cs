using MediatR;

namespace TaskFlow.Application.Features.Boards.Commands.DeleteBoard;

/// <summary>
/// Command xóa board.
/// 
/// IRequest&lt;Unit&gt; nghĩa là "không trả về gì" (void).
/// MediatR dùng Unit thay cho void vì C# generic không hỗ trợ void.
/// Unit giống như "đã xử lý xong, không có kết quả".
/// </summary>
public record DeleteBoardCommand(
    Guid BoardId,
    Guid UserId
) : IRequest<Unit>;
