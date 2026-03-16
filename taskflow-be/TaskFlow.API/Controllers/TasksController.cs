using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Features.Tasks.Commands.CreateTask;
using TaskFlow.Application.Features.Tasks.Commands.DeleteTask;
using TaskFlow.Application.Features.Tasks.Commands.UpdateTask;
using TaskFlow.Application.Features.Tasks.Commands.UpdateTaskStatus;
using TaskFlow.Application.Features.Tasks.Queries.GetTaskById;
using TaskFlow.Application.Features.Tasks.Queries.GetTasksByBoard;
using TaskFlow.Domain.Enums;

namespace TaskFlow.API.Controllers;

/// <summary>
/// TasksController - CRUD operations cho TaskItem.
/// 
/// Route: api/boards/{boardId}/tasks
/// → Nested resource: tasks nằm trong boards.
/// → URL thể hiện quan hệ: "tasks thuộc về board X".
/// 
/// Ví dụ:
///   GET  /api/boards/abc-123/tasks        → Lấy tasks trong board abc-123
///   POST /api/boards/abc-123/tasks        → Tạo task trong board abc-123
///   PUT  /api/boards/abc-123/tasks/xyz-456 → Update task xyz-456
/// </summary>
[ApiController]
[Route("api/boards/{boardId}/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy tất cả tasks trong 1 board
    /// </summary>
    /// <param name="boardId">Board ID</param>
    /// <response code="200">Danh sách tasks</response>
    /// <response code="404">Board không tồn tại</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<TaskItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TaskItemDto>>> GetTasks(Guid boardId)
    {
        var query = new GetTasksByBoardQuery(boardId, GetUserId());
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết 1 task
    /// </summary>
    /// <param name="boardId">Board ID</param>
    /// <param name="id">Task ID</param>
    /// <response code="200">Task details</response>
    /// <response code="404">Task không tồn tại</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> GetTask(Guid boardId, Guid id)
    {
        var query = new GetTaskByIdQuery(id, GetUserId());
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Tạo task mới trong board
    /// </summary>
    /// <remarks>
    /// Priority: Low = 0, Medium = 1, High = 2, Critical = 3
    ///
    ///     POST /api/boards/{boardId}/tasks
    ///     {
    ///         "title": "Task name",
    ///         "description": "Optional",
    ///         "priority": "Medium",
    ///         "deadline": "2026-12-31T00:00:00Z",
    ///         "assignedToId": null
    ///     }
    ///
    /// </remarks>
    /// <param name="boardId">Board ID</param>
    /// <param name="dto">Dữ liệu task mới</param>
    /// <response code="201">Task tạo thành công</response>
    /// <response code="404">Board không tồn tại</response>
    /// <response code="422">Validation failed</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaskItemDto>> CreateTask(Guid boardId, CreateTaskDto dto)
    {
        var command = new CreateTaskCommand(
            dto.Title,
            dto.Description,
            dto.Priority,
            dto.Deadline,
            dto.AssignedToId,
            boardId,
            GetUserId());

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetTask), new { boardId, id = result.Id }, result);
    }

    /// <summary>
    /// Cập nhật toàn bộ task (PUT = full update)
    /// </summary>
    /// <param name="boardId">Board ID</param>
    /// <param name="id">Task ID</param>
    /// <param name="dto">Dữ liệu cập nhật</param>
    /// <response code="200">Task updated</response>
    /// <response code="404">Task không tồn tại</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> UpdateTask(Guid boardId, Guid id, UpdateTaskDto dto)
    {
        var command = new UpdateTaskCommand(
            id,
            dto.Title,
            dto.Description,
            dto.Status,
            dto.Priority,
            dto.Deadline,
            dto.AssignedToId,
            GetUserId());

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật status của task (PATCH = partial update, dùng cho Kanban drag-drop)
    /// </summary>
    /// <remarks>
    /// Chỉ gửi giá trị status mới trong body.
    /// Status: Todo, InProgress, Done, Cancelled
    ///
    ///     PATCH /api/boards/{boardId}/tasks/{id}/status
    ///     "InProgress"
    ///
    /// </remarks>
    /// <param name="boardId">Board ID</param>
    /// <param name="id">Task ID</param>
    /// <param name="newStatus">Status mới</param>
    /// <response code="200">Status updated</response>
    /// <response code="404">Task không tồn tại</response>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> UpdateTaskStatus(
        Guid boardId, Guid id, [FromBody] TaskItemStatus newStatus)
    {
        var command = new UpdateTaskStatusCommand(id, newStatus, GetUserId());
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Xóa task
    /// </summary>
    /// <param name="boardId">Board ID</param>
    /// <param name="id">Task ID cần xóa</param>
    /// <response code="204">Xóa thành công</response>
    /// <response code="404">Task không tồn tại</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid boardId, Guid id)
    {
        var command = new DeleteTaskCommand(id, GetUserId());
        await _mediator.Send(command);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdString!);
    }
}
