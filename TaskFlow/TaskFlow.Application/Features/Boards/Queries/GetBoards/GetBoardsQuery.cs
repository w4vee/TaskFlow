using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Features.Boards.Queries.GetBoards;

/// <summary>
/// QUERY = "Tôi muốn ĐỌC dữ liệu, không thay đổi gì".
/// 
/// Trả về List&lt;TaskBoardDto&gt; - danh sách boards của user đang đăng nhập.
/// Query KHÔNG thay đổi database - chỉ đọc.
/// </summary>
public record GetBoardsQuery(
    Guid UserId
) : IRequest<List<TaskBoardDto>>;
