using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Tests.Infrastructure;

namespace Larpx.PersonalTools.MyCollegeNew.Tests.Extensions
{
/// <summary>
/// DbContextExtensions 单元测试，覆盖 SoftDeleteAsync 扩展方法
/// </summary>
public class DbContextExtensionsTests : IDisposable
{
    private readonly TestDbContext _dbContext;

    /// <summary>
    /// 构造函数，初始化测试数据库上下文
    /// </summary>
    public DbContextExtensionsTests()
    {
        _dbContext = new TestDbContext();
    }

    /// <summary>
    /// SoftDeleteAsync 应将实体的 IsDeleted 标记为 true
    /// </summary>
    [Fact]
    public async Task SoftDeleteAsync_SetsIsDeletedTrue()
    {
        // Arrange
        var department = new Department { Name = "测试院系", CreateTime = DateTime.UtcNow };
        var id = await _dbContext.Client.Insertable(department).ExecuteReturnIdentityAsync();

        // 插入后 IsDeleted 应为 false
        var inserted = await _dbContext.Client.Queryable<Department>().FirstAsync(d => d.Id == id);
        Assert.NotNull(inserted);
        Assert.False(inserted!.IsDeleted);

        // Act
        await _dbContext.SoftDeleteAsync(inserted);

        // Assert - 数据库中 IsDeleted 应为 true
        var deleted = await _dbContext.Client.Queryable<Department>().FirstAsync(d => d.Id == id);
        Assert.NotNull(deleted);
        Assert.True(deleted!.IsDeleted);
    }

    /// <summary>
    /// SoftDeleteAsync 应更新实体的 UpdateTime
    /// </summary>
    [Fact]
    public async Task SoftDeleteAsync_UpdatesUpdateTime()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow.AddSeconds(-5);
        var department = new Department { Name = "测试院系", CreateTime = DateTime.UtcNow };
        var id = await _dbContext.Client.Insertable(department).ExecuteReturnIdentityAsync();

        var inserted = await _dbContext.Client.Queryable<Department>().FirstAsync(d => d.Id == id);
        Assert.NotNull(inserted);
        // 初始 UpdateTime 应为 null
        Assert.Null(inserted!.UpdateTime);

        var beforeSoftDelete = DateTime.UtcNow;

        // Act
        await _dbContext.SoftDeleteAsync(inserted);

        // Assert - UpdateTime 应被设置为当前时间
        var deleted = await _dbContext.Client.Queryable<Department>().FirstAsync(d => d.Id == id);
        Assert.NotNull(deleted);
        Assert.NotNull(deleted!.UpdateTime);
        Assert.True(deleted.UpdateTime >= beforeSoftDelete);
    }

    /// <summary>
    /// SoftDeleteAsync 应同时设置 IsDeleted 和 UpdateTime
    /// </summary>
    [Fact]
    public async Task SoftDeleteAsync_SetsBothIsDeletedAndUpdateTime()
    {
        // Arrange
        var major = new Major { Name = "测试专业", DepartmentId = 1, CreateTime = DateTime.UtcNow };
        await _dbContext.Client.Insertable(new Department { Id = 1, Name = "所属院系", CreateTime = DateTime.UtcNow }).ExecuteCommandAsync();
        var id = await _dbContext.Client.Insertable(major).ExecuteReturnIdentityAsync();

        var inserted = await _dbContext.Client.Queryable<Major>().FirstAsync(m => m.Id == id);
        Assert.NotNull(inserted);

        var beforeSoftDelete = DateTime.UtcNow;

        // Act
        await _dbContext.SoftDeleteAsync(inserted);

        // Assert
        var deleted = await _dbContext.Client.Queryable<Major>().FirstAsync(m => m.Id == id);
        Assert.NotNull(deleted);
        Assert.True(deleted!.IsDeleted);
        Assert.NotNull(deleted.UpdateTime);
        Assert.True(deleted.UpdateTime >= beforeSoftDelete);
    }

    /// <summary>
    /// 释放测试上下文资源
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
}