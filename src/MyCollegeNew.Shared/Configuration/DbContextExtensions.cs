using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;

/// <summary>
/// IDbContext 扩展方法，提供通用的实体操作快捷方式
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// 软删除实体
    /// </summary>
    public static async Task SoftDeleteAsync<T>(this IDbContext dbContext, T entity, CancellationToken cancellationToken = default) where T : EntityBase
    {
        entity.IsDeleted = true;
        entity.UpdateTime = DateTime.UtcNow;
        await dbContext.Client.UpdateableByObject(entity).ExecuteCommandAsync();
    }
}
