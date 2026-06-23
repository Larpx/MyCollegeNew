using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Responses;
using Campus.Attendance.Models.Users;

namespace Campus.Attendance.Services.Users;

/// <summary>
/// 用户管理服务接口，封装学生与教师的增删改查、批量导入与密码修改
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 分页查询学生列表，支持关键字、班级、专业、院系过滤
    /// </summary>
    Task<PagedResult<StudentResponseDto>> GetStudentsAsync(
        int pageIndex, int pageSize, string? keyword = null,
        long? classId = null, long? majorId = null, long? departmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据学号查询单个学生
    /// </summary>
    Task<StudentResponseDto?> GetStudentByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建学生，密码使用 BCrypt 哈希
    /// </summary>
    Task<StudentResponseDto> CreateStudentAsync(StudentCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新学生信息
    /// </summary>
    Task<StudentResponseDto> UpdateStudentAsync(string id, StudentUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 软删除学生（IsDeleted=true）
    /// </summary>
    Task DeleteStudentAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从 CSV 流批量导入学生，默认密码为学号后 6 位
    /// </summary>
    Task<BatchImportResultDto> BatchImportStudentsAsync(Stream csvStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询教师列表，支持角色过滤
    /// </summary>
    Task<PagedResult<TeacherResponseDto>> GetTeachersAsync(
        int pageIndex, int pageSize, string? keyword = null,
        TeacherRole? role = null, long? departmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据工号查询单个教师
    /// </summary>
    Task<TeacherResponseDto?> GetTeacherByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建教师，密码使用 BCrypt 哈希
    /// </summary>
    Task<TeacherResponseDto> CreateTeacherAsync(TeacherCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新教师信息
    /// </summary>
    Task<TeacherResponseDto> UpdateTeacherAsync(string id, TeacherUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 软删除教师
    /// </summary>
    Task DeleteTeacherAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改密码，需校验旧密码
    /// </summary>
    Task ChangePasswordAsync(string userId, UserRole role, PasswordChangeDto dto, CancellationToken cancellationToken = default);
}
