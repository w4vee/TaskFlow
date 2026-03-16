using AutoMapper;
using FluentAssertions;
using Moq;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Features.Boards.Commands.CreateBoard;
using TaskFlow.Application.Mappings;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Tests.Handlers;

/// <summary>
/// Unit Tests cho CreateBoardCommandHandler.
/// 
/// === ĐIỂM KHÁC SO VỚI AUTH HANDLER TESTS ===
/// 
/// Auth handlers dùng mock cho IPasswordHasher, ITokenService.
/// CreateBoardHandler dùng IMapper (AutoMapper).
/// 
/// Best practice: Dùng REAL AutoMapper thay vì mock.
/// Tại sao?
/// - Mock IMapper → ta tự define "Map(X) return Y" → KHÔNG test được mapping logic
/// - Real IMapper → test cả MappingProfile → nếu mapping sai sẽ catch bug
/// 
/// Cách tạo real IMapper trong test:
///   var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
///   var mapper = config.CreateMapper();
/// </summary>
public class CreateBoardCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITaskBoardRepository> _boardRepoMock;
    private readonly IMapper _mapper;  // REAL mapper, không phải Mock!
    private readonly CreateBoardCommandHandler _handler;

    public CreateBoardCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _boardRepoMock = new Mock<ITaskBoardRepository>();

        // Tạo real AutoMapper với MappingProfile thật
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = config.CreateMapper();

        _unitOfWorkMock.Setup(u => u.TaskBoards).Returns(_boardRepoMock.Object);

        _handler = new CreateBoardCommandHandler(_unitOfWorkMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnTaskBoardDto_WhenBoardCreated()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var command = new CreateBoardCommand("Sprint 1", "First sprint board", ownerId);

        // Setup: khi AddAsync được gọi, return entity đó lại (giống EF Core behavior)
        _boardRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TaskBoard>()))
            .ReturnsAsync((TaskBoard board) => board);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TaskBoardDto>();
        result.Name.Should().Be("Sprint 1");
        result.Description.Should().Be("First sprint board");
        result.OwnerId.Should().Be(ownerId);
        result.TaskCount.Should().Be(0);  // Board mới tạo → chưa có task

        // Verify
        _boardRepoMock.Verify(r => r.AddAsync(It.IsAny<TaskBoard>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreateBoardWithCorrectData_WhenCommandIsValid()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var command = new CreateBoardCommand("My Board", "Description here", ownerId);
        TaskBoard? capturedBoard = null;

        _boardRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TaskBoard>()))
            .Callback<TaskBoard>(board => capturedBoard = board)
            .ReturnsAsync((TaskBoard board) => board);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert: verify entity được tạo đúng
        capturedBoard.Should().NotBeNull();
        capturedBoard!.Name.Should().Be("My Board");
        capturedBoard.Description.Should().Be("Description here");
        capturedBoard.OwnerId.Should().Be(ownerId);
        capturedBoard.Id.Should().NotBeEmpty();  // BaseEntity auto-generate Guid
    }

    /// <summary>
    /// Test: Board có thể tạo KHÔNG có description (nullable).
    /// </summary>
    [Fact]
    public async Task Handle_ShouldAllowNullDescription_WhenDescriptionNotProvided()
    {
        // Arrange
        var command = new CreateBoardCommand("Board No Desc", null, Guid.NewGuid());

        _boardRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TaskBoard>()))
            .ReturnsAsync((TaskBoard board) => board);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Description.Should().BeNull();
        result.Name.Should().Be("Board No Desc");
    }

    /// <summary>
    /// Test: AutoMapper config đúng, không throw lỗi khi map.
    /// 
    /// Đây là test cho mapping configuration.
    /// Nếu MappingProfile có lỗi (property name sai, type mismatch),
    /// AutoMapper sẽ throw exception → test này fail → catch bug sớm.
    /// </summary>
    [Fact]
    public void AutoMapper_ShouldHaveValidConfiguration()
    {
        // Arrange
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        // Assert: validate toàn bộ mapping config
        config.AssertConfigurationIsValid();
    }
}
