using Microsoft.AspNetCore.Http;

namespace Larpx.PersonalTools.MyCollegeNew.Api.Middleware;

/// <summary>
/// 安全头中间件，统一添加 X-Content-Type-Options、X-Frame-Options、X-XSS-Protection 等安全响应头
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">下一个中间件委托</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// 中间件执行入口，添加安全响应头后转发请求
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1";

        await _next(context);
    }
}
