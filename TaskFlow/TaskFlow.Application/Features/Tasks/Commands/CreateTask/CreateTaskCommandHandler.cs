using AutoMapper;
using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskItemDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateTaskCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TaskItemDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify board tồn tại và user là owner
        var board = await _unitOfWork.TaskBoards.GetByIdAsync(request.BoardId);
        if (board is null)
        {
            throw new NotFoundException("TaskBoard", request.BoardId);
        }

        if (board.OwnerId != request.UserId)
        {
            throw new BadRequestException("You are not the owner of this board.");
        }

        // 2. Nếu có AssignedToId, verify user đó tồn tại
        if (request.AssignedToId.HasValue)
        {
            var assignedUser = await _unitOfWork.Users.GetByIdAsync(request.AssignedToId.Value);
            if (assignedUser is null)
            {
                throw new NotFoundException("User", request.AssignedToId.Value);
            }
        }

        // 3. Tạo TaskItem entity
        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Deadline = request.Deadline,
            AssignedToId = request.AssignedToId,
            BoardId = request.BoardId
            // Status mặc định = Todo (set trong Entity)
            // IsOverdue mặc định = false (Hangfire job sẽ cập nhật sau)
        };

        await _unitOfWork.TaskItems.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<TaskItemDto>(task);
    }
}
