using MediatR;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Features.Boards.Commands.CreateBoard;

/// <summary>
/// Command tạo board mới.
/// 
/// OwnerId: ID của user đang đăng nhập (Controller sẽ lấy từ JWT token).
/// Name, Description: dữ liệu từ request body (CreateBoardDto).
/// 
/// Tại sao không truyền CreateBoardDto trực tiếp vào Command?
/// → Vì Command cần chứa TẤT CẢ thông tin để Handler xử lý,
///   bao gồm cả OwnerId mà DTO không có (DTO chỉ chứa input từ client).
/// </summary>
public record CreateBoardCommand(
    string Name,
    string? Description,
    Guid OwnerId
) : IRequest<TaskBoardDto>;
