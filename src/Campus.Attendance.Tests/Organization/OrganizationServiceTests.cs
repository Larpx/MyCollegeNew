using Campus.Attendance.Core.Entities;
using Campus.Attendance.Core.Enums;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Models.Organization;
using Campus.Attendance.Services.Organization;
using Campus.Attendance.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Campus.Attendance.Tests.Organization;

/// <summary>
/// OrganizationService 单元测试，使用 SQLite 内存数据库隔离测试
/// </summary>
public class OrganizationServiceTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly OrganizationService _organizationService;

    /// <summary>
    /// 构造函数，初始化测试上下文与 OrganizationService 实例
    /// </summary>
    public OrganizationServiceTests()
    {
        _dbContext = new TestDbContext();
        _organizationService = new OrganizationService(_dbContext, NullLogger<OrganizationService>.Instance);
    }

    /// <summary>
    /// 创建院系使用合法 DTO 应返回创建后的院系信息
    /// </summary>
    [Fact]
    public async Task CreateDepartmentAsync_ValidDto_ReturnsCreatedDepartment()
    {
        // Arrange
        var dto = new DepartmentCreateDto { Name = "计算机学院" };

        // Act
        var result = await _organizationService.CreateDepartmentAsync(dto);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("计算机学院", result.Name);
        Assert.Equal(0, result.MajorCount);
        Assert.Equal(0, result.StudentCount);
    }

    /// <summary>
    /// 删除存在关联专业的院系应抛出 BusinessException
    /// </summary>
    [Fact]
    public async Task DeleteDepartmentAsync_WithMajors_ThrowsBusinessException()
    {
        // Arrange
        var departmentId = await CreateDepartmentAsync("计算机学院");
        await CreateMajorAsync("软件工程", departmentId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _organizationService.DeleteDepartmentAsync(departmentId));
        Assert.Contains("专业", ex.Message);
    }

    /// <summary>
    /// 查询组织架构树应返回院系→专业→班级的层级数据
    /// </summary>
    [Fact]
    public async Task GetOrganizationTreeAsync_ReturnsHierarchicalData()
    {
        // Arrange
        var departmentId = await CreateDepartmentAsync("计算机学院");
        var majorId = await CreateMajorAsync("软件工程", departmentId);
        await CreateClassAsync("软工2201", majorId);

        // Act
        var tree = await _organizationService.GetOrganizationTreeAsync();

        // Assert
        Assert.Single(tree);
        var node = tree[0];
        Assert.Equal("计算机学院", node.Department.Name);
        Assert.Equal(1, node.Department.MajorCount);
        Assert.Single(node.Majors);
        Assert.Equal("软件工程", node.Majors[0].Major.Name);
        Assert.Single(node.Majors[0].Classes);
        Assert.Equal("软工2201", node.Majors[0].Classes[0].Name);
    }

    /// <summary>
    /// 创建专业使用合法 DTO 应返回创建后的专业信息
    /// </summary>
    [Fact]
    public async Task CreateMajorAsync_ValidDto_ReturnsCreatedMajor()
    {
        // Arrange
        var departmentId = await CreateDepartmentAsync("计算机学院");
        var dto = new MajorCreateDto { Name = "软件工程", DepartmentId = departmentId };

        // Act
        var result = await _organizationService.CreateMajorAsync(dto);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("软件工程", result.Name);
        Assert.Equal(departmentId, result.DepartmentId);
        Assert.Equal("计算机学院", result.DepartmentName);
    }

    /// <summary>
    /// 删除存在关联班级的专业应抛出 BusinessException
    /// </summary>
    [Fact]
    public async Task DeleteMajorAsync_WithClasses_ThrowsBusinessException()
    {
        // Arrange
        var departmentId = await CreateDepartmentAsync("计算机学院");
        var majorId = await CreateMajorAsync("软件工程", departmentId);
        await CreateClassAsync("软工2201", majorId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _organizationService.DeleteMajorAsync(majorId));
        Assert.Contains("班级", ex.Message);
    }

    /// <summary>
    /// 释放测试上下文资源
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 播种院系并返回 Id
    /// </summary>
    private async Task<long> CreateDepartmentAsync(string name)
    {
        var department = new Department { Name = name, CreateTime = DateTime.UtcNow };
        return await _dbContext.Client.Insertable(department).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 播种专业并返回 Id
    /// </summary>
    private async Task<long> CreateMajorAsync(string name, long departmentId)
    {
        var major = new Major { Name = name, DepartmentId = departmentId, CreateTime = DateTime.UtcNow };
        return await _dbContext.Client.Insertable(major).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 播种班级并返回 Id，辅导员使用 T002
    /// </summary>
    private async Task<long> CreateClassAsync(string name, long majorId)
    {
        // 先播种辅导员教师
        var counselor = new Teacher
        {
            Id = "T002",
            Name = "李老师",
            Password = BCrypt.Net.BCrypt.HashPassword("123456"),
            Gender = "女",
            DepartmentId = 1,
            MajorId = null,
            Role = TeacherRole.Counselor,
            CreateTime = DateTime.UtcNow
        };
        if (!await _dbContext.Client.Queryable<Teacher>().AnyAsync(t => t.Id == "T002"))
        {
            await _dbContext.Client.Insertable(counselor).ExecuteCommandAsync();
        }

        var cls = new Class
        {
            Name = name,
            MajorId = majorId,
            Grade = 2022,
            CounselorId = "T002",
            CreateTime = DateTime.UtcNow
        };
        return await _dbContext.Client.Insertable(cls).ExecuteReturnIdentityAsync();
    }
}
