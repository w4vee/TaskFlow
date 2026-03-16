using AutoMapper;
using MediatR;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Boards.Queries.GetBoards;

public class GetBoardsQueryHandler : IRequestHandler<GetBoardsQuery, List<TaskBoardDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBoardsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<TaskBoardDto>> Handle(GetBoardsQuery request, CancellationToken cancellationToken)
    {
        // Lấy tất cả boards của user
        var boards = await _unitOfWork.TaskBoards.GetBoardsByOwnerIdAsync(request.UserId);

        // Map List<TaskBoard> → List<TaskBoardDto>
        // AutoMapper hỗ trợ map collection tự động
        return _mapper.Map<List<TaskBoardDto>>(boards);
    }
}
