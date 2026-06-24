using Campus.Attendance.Shared.Responses;

namespace Campus.Attendance.Web.Services;

/// <summary>
/// API 客户端接口，封装 HttpClient 调用后端 API 的统一入口
/// 自动附加 JWT Token、反序列化 ApiResponse&lt;T&gt; 并处理 401/403
/// </summary>
public interface IApiClient
{
    /// <summary>发起 GET 请求并反序列化业务数据</summary>
    /// <typeparam name="T">业务数据类型</typeparam>
    /// <param name="url">相对地址，如 api/auth/profile</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>业务数据；若响应失败则抛出 ApiException</returns>
    Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default);

    /// <summary>发起 POST 请求并反序列化业务数据</summary>
    /// <typeparam name="T">业务数据类型</typeparam>
    /// <param name="url">相对地址</param>
    /// <param name="body">请求体（将序列化为 JSON）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>业务数据；若响应失败则抛出 ApiException</returns>
    Task<T?> PostAsync<T>(string url, object? body = null, CancellationToken cancellationToken = default);

    /// <summary>发起 PUT 请求并反序列化业务数据</summary>
    /// <typeparam name="T">业务数据类型</typeparam>
    /// <param name="url">相对地址</param>
    /// <param name="body">请求体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>业务数据；若响应失败则抛出 ApiException</returns>
    Task<T?> PutAsync<T>(string url, object? body = null, CancellationToken cancellationToken = default);

    /// <summary>发起 DELETE 请求并反序列化业务数据</summary>
    /// <typeparam name="T">业务数据类型</typeparam>
    /// <param name="url">相对地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>业务数据；若响应失败则抛出 ApiException</returns>
    Task<T?> DeleteAsync<T>(string url, CancellationToken cancellationToken = default);

    /// <summary>发起 POST 请求（无返回业务数据，仅关心成功/失败）</summary>
    /// <param name="url">相对地址</param>
    /// <param name="body">请求体</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PostNoContentAsync(string url, object? body = null, CancellationToken cancellationToken = default);

    /// <summary>以 multipart/form-data 方式上传文件并反序列化业务数据</summary>
    /// <typeparam name="T">业务数据类型</typeparam>
    /// <param name="url">相对地址</param>
    /// <param name="content">MultipartFormDataContent 表单内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>业务数据；若响应失败则抛出 ApiException</returns>
    Task<T?> PostFormAsync<T>(string url, MultipartFormDataContent content, CancellationToken cancellationToken = default);

    /// <summary>下载文件（返回字节流，用于 Excel 导出等场景）</summary>
    /// <param name="url">相对地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件字节数组；若响应失败则抛出 ApiException</returns>
    Task<byte[]> DownloadFileAsync(string url, CancellationToken cancellationToken = default);
}
