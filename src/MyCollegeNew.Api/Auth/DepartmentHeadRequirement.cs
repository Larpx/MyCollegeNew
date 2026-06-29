using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Security.Claims;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Auth
{
    /// <summary>
    /// 系主任授权要求：标识当前接口需要系主任身份
    /// </summary>
    public class DepartmentHeadRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// 系主任授权处理器：根据 JWT 中的用户工号查询 Teacher 表，
    /// 校验 IsDepartmentHead=true 且未被软删除
    /// </summary>
    public class DepartmentHeadHandler : AuthorizationHandler<DepartmentHeadRequirement>
    {
        private readonly IDbContext _dbContext;
        private readonly ILogger<DepartmentHeadHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="logger">日志器</param>
        public DepartmentHeadHandler(IDbContext dbContext, ILogger<DepartmentHeadHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// 校验当前用户是否为系主任
        /// </summary>
        /// <param name="context">授权上下文</param>
        /// <param name="requirement">授权要求</param>
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, DepartmentHeadRequirement requirement)
        {
            // 优先使用自定义 user_id 声明，回退到标准 NameIdentifier
            var userId = context.User?.FindFirst("user_id")?.Value
                         ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var teacher = await _dbContext.Client.Queryable<Teacher>()
                .FirstAsync(t => t.Id == userId && t.IsDepartmentHead && !t.IsDeleted);
            if (teacher is not null)
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("用户 {UserId} 不具备系主任身份，授权被拒绝", userId);
            }
        }
    }
}
