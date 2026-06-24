using Larpx.PersonalTools.MyCollegeNew.Shared.Exceptions;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;
using Msg = Larpx.PersonalTools.MyCollegeNew.Shared.Constants.MessageConstants;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Exceptions
{
    /// <summary>
    /// 全局异常处理器，实现 IExceptionHandler 接口，捕获所有未处理异常并统一返回 ApiResponse 结构
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志器</param>
        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 尝试处理异常
        /// </summary>
        /// <param name="httpContext">HTTP 上下文</param>
        /// <param name="exception">异常对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否已处理</returns>
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            switch (exception)
            {
                case Shared.Exceptions.ValidationException validationEx:
                    _logger.LogWarning("校验异常: {Errors}", string.Join("; ", validationEx.Errors));
                    await WriteResponseAsync(httpContext, HttpStatusCode.BadRequest, validationEx.Message);
                    return true;

                case BusinessException businessEx:
                    _logger.LogWarning("业务异常: {Code} {Message}", businessEx.Code, businessEx.Message);
                    var statusCode = businessEx.Code switch
                    {
                        401 => HttpStatusCode.Unauthorized,
                        403 => HttpStatusCode.Forbidden,
                        404 => HttpStatusCode.NotFound,
                        >= 500 => HttpStatusCode.InternalServerError,
                        _ => HttpStatusCode.BadRequest
                    };
                    await WriteResponseAsync(httpContext, statusCode, businessEx.Message);
                    return true;

                default:
                    // 系统异常禁止暴露 ex.Message，仅记录到日志，统一返回通用提示
                    _logger.LogError(exception, "未处理异常: {Message}", exception.Message);
                    await WriteResponseAsync(httpContext, HttpStatusCode.InternalServerError, Msg.Common.ServerError);
                    return true;
            }
        }

        /// <summary>
        /// 写入统一错误响应
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <param name="statusCode">HTTP 状态码</param>
        /// <param name="message">提示信息</param>
        private static async Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, string message)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = (int)statusCode;

            var response = ApiResponse<object>.Fail(message, (int)statusCode);
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await context.Response.WriteAsync(json);
        }
    }
}