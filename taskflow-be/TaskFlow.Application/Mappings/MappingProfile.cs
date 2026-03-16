using AutoMapper;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Mappings;

/// <summary>
/// AutoMapper Profile - nơi khai báo tất cả mapping rules.
/// 
/// AutoMapper hoạt động theo convention: nếu property CÙNG TÊN và CÙNG KIỂU
/// giữa source và destination, nó tự map (ví dụ: User.FullName → UserDto.FullName).
/// 
/// Với property tên KHÁC hoặc cần TÍNH TOÁN, ta dùng ForMember() để custom.
/// 
/// Profile này được register vào DI container lúc startup,
/// AutoMapper sẽ scan tất cả Profile trong Assembly và tạo sẵn mapping config.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ===== USER MAPPING =====
        // User → UserDto: map thẳng, tất cả property trùng tên
        // PasswordHash, RefreshToken KHÔNG có trong UserDto → tự động bỏ qua (an toàn!)
        CreateMap<User, UserDto>();

        // ===== BOARD MAPPING =====
        // TaskBoard → TaskBoardDto: cần custom TaskCount vì TaskBoard không có property này
        CreateMap<TaskBoard, TaskBoardDto>()
            .ForMember(
                dest => dest.TaskCount,                    // Property đích
                opt => opt.MapFrom(src => src.Tasks != null ? src.Tasks.Count : 0)  // Null check cho trường hợp Tasks chưa load
            );

        // ===== TASK MAPPING =====
        // TaskItem → TaskItemDto: cần custom AssignedToName
        CreateMap<TaskItem, TaskItemDto>()
            .ForMember(
                dest => dest.AssignedToName,
                opt => opt.MapFrom(src => src.AssignedTo != null ? src.AssignedTo.FullName : null)
            );
    }
}
