using AutoMapper;
using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Tasks.Queries.GetTasksByBoard;

public class GetTasksByBoardQueryHandler : IRequestHandler<GetTasksByBoardQuery, List<TaskItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetTasksByBoardQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<TaskItemDto>> Handle(GetTasksByBoardQuery request, CancellationToken cancellationToken)
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

        // 2. Lấy tất cả tasks của board
        var tasks = await _unitOfWork.TaskItems.GetTasksByBoardIdAsync(request.BoardId);

        return _mapper.Map<List<TaskItemDto>>(tasks);
    }
}
