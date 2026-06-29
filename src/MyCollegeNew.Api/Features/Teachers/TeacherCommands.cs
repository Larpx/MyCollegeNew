using Larpx.PersonalTools.MyCollegeNew.Shared.Contracts;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Users;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using MediatR;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Features.Teachers
{
    /// <summary>分页查询教师列表</summary>
    public record GetTeachersQuery : PagedQuery, IRequest<ApiResponse<PagedResult<TeacherResponseDto>>>
    {
        /// <summary>搜索关键字</summary>
        public string? Keyword { get; init; }

        /// <summary>教师角色</summary>
        public TeacherRole? Role { get; init; }

        /// <summary>院系ID</summary>
        public long? DepartmentId { get; init; }
    }

    /// <summary>根据工号查询教师</summary>
    public record GetTeacherByIdQuery(string Id) : IRequest<ApiResponse<TeacherResponseDto>>;

    /// <summary>
    /// 查询当前登录教师：教师本人可调用以获取自身信息（含 IsDepartmentHead 标记位），
    /// 供前端 NavMenu 与仪表盘动态渲染系主任专属功能
    /// </summary>
    /// <param name="TeacherId">当前登录教师工号</param>
    public record GetCurrentTeacherQuery(string TeacherId) : IRequest<ApiResponse<TeacherResponseDto>>;

    /// <summary>创建教师</summary>
    public record CreateTeacherCommand(TeacherCreateDto Dto) : IRequest<ApiResponse<TeacherResponseDto>>;

    /// <summary>更新教师</summary>
    public record UpdateTeacherCommand(string Id, TeacherUpdateDto Dto) : IRequest<ApiResponse<TeacherResponseDto>>;

    /// <summary>删除教师</summary>
    public record DeleteTeacherCommand(string Id) : IRequest<ApiResponse<object>>;
}