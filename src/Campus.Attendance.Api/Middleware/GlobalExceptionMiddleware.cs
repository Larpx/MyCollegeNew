using System.Net;
using System.Text.Json;
using Campus.Attendance.Core.Constants;
using Campus.Attendance.Core.Exceptions;
using Campus.Attendance.Core.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Msg = Campus.Attendance.Core.Constants.MessageConstants;

namespace Campus.Attendance.Api.Middleware;

/// <summary>
/// 全局异常处理中间件，捕获所有未处理异常并统一返回 ApiResponse 结构，禁止暴露 ex.Message
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">下一个中间件委托</param>
    /// <param name="logger">日志器</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 中间件执行入口
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            // 业务异常可对外暴露 Message
            _logger.LogWarning(ex, "业务异常: {Code} {Message}", ex.Code, ex.Message);
            await WriteResponseAsync(context, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            // 系统异常禁止暴露 ex.Message，仅记录到日志，统一返回通用提示
            _logger.LogError(ex, "未处理异常: {Message}", ex.Message);
            await WriteResponseAsync(context, (int)HttpStatusCode.InternalServerError, Msg.Common.ServerError);
        }
    }

    /// <summary>
    /// 写入统一错误响应
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="code">状态码</param>
    /// <param name="message">提示信息</param>
    private static async Task WriteResponseAsync(HttpContext context, int code, string message)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = code switch
        {
            >= 200 and < 300 => 200,
            401 => 401,
            403 => 403,
            404 => 404,
            >= 500 => 500,
            _ => 400
        };

        var response = ApiResponse<object>.Fail(message, code);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await context.Response.WriteAsync(json);
    }
}
