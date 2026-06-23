using SqlSugar;

namespace Campus.Attendance.Core.Configuration;

/// <summary>
/// 数据库上下文接口，封装 SqlSugar 客户端，供业务服务层统一访问数据库
/// </summary>
public interface IDbContext
{
    /// <summary>获取 SqlSugar 数据库客户端实例</summary>
    ISqlSugarClient Client { get; }
}
