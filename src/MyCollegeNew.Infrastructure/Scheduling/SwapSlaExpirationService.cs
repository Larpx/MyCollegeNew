using Larpx.PersonalTools.MyCollegeNew.Shared.Configuration;
using Larpx.PersonalTools.MyCollegeNew.Shared.Constants;
using Larpx.PersonalTools.MyCollegeNew.Shared.Entities;
using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Infrastructure.Scheduling
{
    /// <summary>
    /// 调换课申请 SLA 过期处理后台服务
    /// 按 <see cref="CourseSwapSlaConstants.ExpirationScanIntervalMinutes"/> 周期扫描 Pending 状态的调换课申请，
    /// 超过 <see cref="CourseSwapSlaConstants.SlaHours"/> 小时未确认的自动撤销（Status=Cancelled），
    /// 避免代课教师长时间不确认导致原任课教师排课状态不确定
    /// </summary>
    public class SwapSlaExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SwapSlaExpirationService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">服务定位器，用于在每次扫描中创建独立作用域解析 Scoped 服务</param>
        /// <param name="logger">日志器</param>
        public SwapSlaExpirationService(IServiceProvider serviceProvider, ILogger<SwapSlaExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// 后台执行入口：循环扫描过期申请，捕获并记录循环内异常以保证服务持续运行不闪退
        /// </summary>
        /// <param name="stoppingToken">停止令牌</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scanInterval = TimeSpan.FromMinutes(CourseSwapSlaConstants.ExpirationScanIntervalMinutes);
            _logger.LogInformation(
                "调换课 SLA 过期处理服务已启动，扫描间隔 {Interval} 分钟",
                scanInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredSwapsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // 应用关闭时正常退出，无需记录错误
                    break;
                }
                catch (Exception ex)
                {
                    // 单轮扫描异常不能让服务退出，记录错误后继续等待下一轮
                    _logger.LogError(ex, "调换课 SLA 过期处理任务异常，将在下一轮扫描继续");
                }

                try
                {
                    await Task.Delay(scanInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation("调换课 SLA 过期处理服务已停止");
        }

        /// <summary>
        /// 扫描并撤销所有超过 SLA 时长仍未确认的 Pending 调换课申请
        /// </summary>
        /// <param name="ct">取消令牌</param>
        private async Task ProcessExpiredSwapsAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
            var db = dbContext.Client;

            // 计算逾期临界时间：CreateTime 早于该时间且仍为 Pending 即视为超过 SLA
            var deadline = DateTime.UtcNow.AddHours(-CourseSwapSlaConstants.SlaHours);
            var expiredSwaps = await db.Queryable<CourseSwapRequest>()
                .Where(s => s.Status == SwapStatus.Pending && !s.IsDeleted && s.CreateTime <= deadline)
                .ToListAsync(ct);

            if (expiredSwaps.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var swap in expiredSwaps)
            {
                swap.Status = SwapStatus.Cancelled;
                swap.UpdateTime = now;
                await db.Updateable(swap)
                    .UpdateColumns(s => new { s.Status, s.UpdateTime })
                    .ExecuteCommandAsync(ct);
                _logger.LogWarning(
                    "调换课申请 {SwapId} 因超过 {SlaHours} 小时未确认已自动撤销",
                    swap.Id,
                    CourseSwapSlaConstants.SlaHours);
            }

            _logger.LogInformation(
                "本次扫描共撤销 {Count} 笔逾期调换课申请",
                expiredSwaps.Count);
        }
    }
}
