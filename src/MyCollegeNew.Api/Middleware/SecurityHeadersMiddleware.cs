namespace Larpx.PersonalTools.MyCollegeNew.Api.Middleware
{
    /// <summary>
    /// 安全头中间件，统一添加安全响应头防范 XSS、点击劫持、MIME 嗅探、协议降级等攻击
    /// L-4 修复：补充 CSP、HSTS、Referrer-Policy、Permissions-Policy
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
            var headers = context.Response.Headers;

            // 防止 MIME 嗅探
            headers["X-Content-Type-Options"] = "nosniff";

            // 防止点击劫持（与 CSP frame-ancestors 互为兜底）
            headers["X-Frame-Options"] = "DENY";

            // 旧版浏览器 XSS 过滤（现代浏览器已内置，保留用于兼容）
            headers["X-XSS-Protection"] = "1";

            // L-4 修复：内容安全策略（API 仅返回 JSON，禁止任何内容渲染与嵌入）
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            // L-4 修复：HSTS 强制 HTTPS（仅 HTTPS 响应生效，HTTP 连接浏览器会忽略）
            // max-age=1 年，包含子域名，允许预加载
            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            // L-4 修复：Referrer 策略，跨域请求仅发送 origin 不含路径
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // L-4 修复：权限策略，禁用敏感设备 API
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            await _next(context);
        }
    }
}