using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Middleware
{
    /// <summary>
    /// 数据库健康检查：验证 SqlSugar 能否成功连接并执行简单查询
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IDbContext _dbContext;

        /// <summary>
        /// 构造函数，注入数据库上下文
        /// </summary>
        /// <param name="dbContext">SqlSugar 数据库上下文</param>
        public DatabaseHealthCheck(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 执行健康检查：尝试执行简单 SQL 查询验证数据库连接
        /// </summary>
        /// <param name="context">健康检查上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>健康检查结果</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dbContext.Client.Ado.GetIntAsync("SELECT 1");
                return result == 1
                    ? HealthCheckResult.Healthy("数据库连接正常")
                    : HealthCheckResult.Degraded("数据库查询返回异常值");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("数据库连接失败", ex);
            }
        }
    }
}
