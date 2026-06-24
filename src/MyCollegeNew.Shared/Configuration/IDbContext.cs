using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Configuration
{
/// <summary>
/// 数据库上下文接口
/// </summary>
public interface IDbContext
{
    /// <summary>获取 SqlSugar 数据库客户端实例</summary>
    ISqlSugarClient Client { get; }
}
}