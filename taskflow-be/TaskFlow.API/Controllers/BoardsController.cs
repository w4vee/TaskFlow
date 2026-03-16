using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Features.Boards.Commands.CreateBoard;
using TaskFlow.Application.Features.Boards.Commands.DeleteBoard;
using TaskFlow.Application.Features.Boards.Commands.UpdateBoard;
using TaskFlow.Application.Features.Boards.Queries.GetBoardById;
using TaskFlow.Application.Features.Boards.Queries.GetBoards;

namespace TaskFlow.API.Controllers;

/// <summary>
/// BoardsController - CRUD operations cho TaskBoard.
/// 
/// [Authorize]: BẮT BUỘC đăng nhập (có JWT token hợp lệ) mới access được.
///   Không có token → Authentication middleware trả 401 Unauthorized.
///   Token hết hạn → 401.
///   Token sai → 401.
/// 
/// RESTful API conventions:
///   GET    /api/boards       → Lấy danh sách (GetAll)
///   GET    /api/boards/{id}  → Lấy chi tiết (GetById)
///   POST   /api/boards       → Tạo mới (Create)
///   PUT    /api/boards/{id}  → Cập nhật (Update)
///   DELETE /api/boards/{id}  → Xóa (Delete)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Tất cả endpoints trong controller này yêu cầu đăng nhập
public class BoardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BoardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// GET /api/boards
    /// Lấy tất cả boards của user đang đăng nhập.
    /// </summary>
    /// <summary>
    /// Lấy tất cả boards của user hiện tại
    /// </summary>
    /// <response code="200">Danh sách boards</response>
    /// <response code="401">Chưa đăng nhập</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<TaskBoardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TaskBoardDto>>> GetBoards()
    {
        var query = new GetBoardsQuery(GetUserId());
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/boards/{id}
    /// Lấy chi tiết 1 board.
    /// 
    /// {id} trong route → parameter Guid id.
    /// ASP.NET Core tự bind từ URL path vào parameter.
    /// </summary>
    /// <summary>
    /// Lấy chi tiết 1 board theo ID
    /// </summary>
    /// <param name="id">Board ID</param>
    /// <response code="200">Board details</response>
    /// <response code="404">Board không tồn tại</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskBoardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskBoardDto>> GetBoard(Guid id)
    {
        var query = new GetBoardByIdQuery(id, GetUserId());
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/boards
    /// Tạo board mới.
    /// 
    /// [FromBody] CreateBoardDto: ASP.NET Core tự deserialize JSON body → DTO.
    /// OwnerId lấy từ JWT token (GetUserId), KHÔNG từ client.
    /// → Client không thể giả mạo OwnerId.
    /// 
    /// Return 201 Created + Location header.
    /// CreatedAtAction: trả header "Location: /api/boards/{id}" 
    /// → Client biết URL để GET board vừa tạo.
    /// </summary>
    /// <summary>
    /// Tạo board mới
    /// </summary>
    /// <remarks>
    /// Owner tự động lấy từ JWT token, không cần truyền.
    ///
    ///     POST /api/boards
    ///     {
    ///         "name": "My Board",
    ///         "description": "Optional description"
    ///     }
    ///
    /// </remarks>
    /// <response code="201">Board tạo thành công</response>
    /// <response code="422">Validation failed</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskBoardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaskBoardDto>> CreateBoard(CreateBoardDto dto)
    {
        var command = new CreateBoardCommand(dto.Name, dto.Description, GetUserId());
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetBoard), new { id = result.Id }, result);
    }

    /// <summary>
    /// PUT /api/boards/{id}
    /// Cập nhật board.
    /// 
    /// UserId truyền vào Command để Handler verify ownership.
    /// </summary>
    /// <summary>
    /// Cập nhật board
    /// </summary>
    /// <param name="id">Board ID cần update</param>
    /// <param name="dto">Dữ liệu cập nhật</param>
    /// <response code="200">Board updated</response>
    /// <response code="404">Board không tồn tại</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskBoardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskBoardDto>> UpdateBoard(Guid id, UpdateBoardDto dto)
    {
        var command = new UpdateBoardCommand(id, dto.Name, dto.Description, GetUserId());
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// DELETE /api/boards/{id}
    /// Xóa board.
    /// 
    /// Return 204 No Content (xóa thành công, không trả data).
    /// </summary>
    /// <summary>
    /// Xóa board (và tất cả tasks trong board)
    /// </summary>
    /// <param name="id">Board ID cần xóa</param>
    /// <response code="204">Xóa thành công</response>
    /// <response code="404">Board không tồn tại</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBoard(Guid id)
    {
        var command = new DeleteBoardCommand(id, GetUserId());
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Helper: lấy UserId từ JWT claims.
    /// </summary>
    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdString!);
    }
}
