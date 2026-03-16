using AutoMapper;
using MediatR;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Boards.Commands.CreateBoard;

public class CreateBoardCommandHandler : IRequestHandler<CreateBoardCommand, TaskBoardDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateBoardCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TaskBoardDto> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
    {
        var board = new TaskBoard
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = request.OwnerId
        };

        await _unitOfWork.TaskBoards.AddAsync(board);
        await _unitOfWork.SaveChangesAsync();

        // Map entity → DTO để trả về cho client
        // Dùng AutoMapper thay vì new TaskBoardDto(...) thủ công
        return _mapper.Map<TaskBoardDto>(board);
    }
}
