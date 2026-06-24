using Campus.Attendance.Core.Entities;

namespace Campus.Attendance.Core.Configuration;

/// <summary>
/// IDbContext 扩展方法，提供通用的实体操作快捷方式
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// 软删除实体：将 IsDeleted 标记为 true 并更新 UpdateTime，然后持久化到数据库
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="entity">待软删除的实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task SoftDeleteAsync<T>(this IDbContext dbContext, T entity, CancellationToken cancellationToken = default) where T : EntityBase
    {
        entity.IsDeleted = true;
        entity.UpdateTime = DateTime.UtcNow;
        await dbContext.Client.UpdateableByObject(entity).ExecuteCommandAsync();
    }
}
