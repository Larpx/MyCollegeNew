using System.Net;
using System.Net.Http.Json;
using Campus.Attendance.Core.Responses;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Campus.Attendance.Web.Services;

/// <summary>
/// API 客户端自定义异常，承载后端 ApiResponse 的提示信息与状态码
/// </summary>
public class ApiException : Exception
{
    /// <summary>HTTP 状态码（401/403/400/404/500 等）</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>后端 ApiResponse.Message 提示信息</summary>
    public string ApiMessage { get; }

    /// <summary>构造异常</summary>
    /// <param name="statusCode">HTTP 状态码</param>
    /// <param name="message">提示信息</param>
    public ApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
        ApiMessage = message;
    }
}

/// <summary>
/// API 客户端实现：封装 HttpClient，自动附加 JWT Token、反序列化 ApiResponse&lt;T&gt;
/// 401 跳转登录页，403 提示无权限
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<ApiClient> _logger;

    // localStorage 中存储 Token 的键名
    private const string TokenKey = "campus_token";

    /// <summary>构造函数，依赖注入 HttpClient、JSRuntime、NavigationManager、Logger</summary>
    /// <param name="httpClient">已配置 BaseAddress 的 HttpClient</param>
    /// <param name="jsRuntime">JS 运行时，用于读取 localStorage</param>
    /// <param name="navigationManager">导航管理器，用于 401 跳转</param>
    /// <param name="logger">日志记录器</param>
    public ApiClient(HttpClient httpClient, IJSRuntime jsRuntime, NavigationManager navigationManager, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        return await SendAsync<T>(HttpMethod.Get, url, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> PostAsync<T>(string url, object? body = null, CancellationToken cancellationToken = default)
    {
        return await SendAsync<T>(HttpMethod.Post, url, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> PutAsync<T>(string url, object? body = null, CancellationToken cancellationToken = default)
    {
        return await SendAsync<T>(HttpMethod.Put, url, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> DeleteAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        return await SendAsync<T>(HttpMethod.Delete, url, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PostNoContentAsync(string url, object? body = null, CancellationToken cancellationToken = default)
    {
        await SendAsync<object>(HttpMethod.Post, url, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> PostFormAsync<T>(string url, MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        return await SendFormAsync<T>(HttpMethod.Post, url, content, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadFileAsync(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // 附加 JWT Token
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载文件失败：{Url}", url);
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "网络异常，请稍后重试");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            _navigationManager.NavigateTo("/login", forceLoad: true);
            throw new ApiException(HttpStatusCode.Unauthorized, "登录已过期，请重新登录");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new ApiException(HttpStatusCode.Forbidden, "无权限执行此操作");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(response.StatusCode, "下载失败");
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// 以 multipart/form-data 方式发送请求并反序列化 ApiResponse
    /// </summary>
    /// <typeparam name="T">业务数据类型</typeparam>
    /// <param name="method">HTTP 方法</param>
    /// <param name="url">相对地址</param>
    /// <param name="content">表单内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>业务数据</returns>
    private async Task<T?> SendFormAsync<T>(HttpMethod method, string url, MultipartFormDataContent content, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);

        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        request.Content = content;

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用后端 API 失败：{Method} {Url}", method, url);
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "网络异常，请稍后重试");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            _navigationManager.NavigateTo("/login", forceLoad: true);
            throw new ApiException(HttpStatusCode.Unauthorized, "登录已过期，请重新登录");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new ApiException(HttpStatusCode.Forbidden, "无权限执行此操作");
        }

        ApiResponse<T>? apiResponse;
        try
        {
            apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "反序列化响应失败：{Url}", url);
            throw new ApiException(response.StatusCode, "响应格式错误");
        }

        if (apiResponse is null)
        {
            throw new ApiException(response.StatusCode, "响应为空");
        }

        if (apiResponse.Code < 200 || apiResponse.Code >= 300)
        {
            throw new ApiException(response.StatusCode, string.IsNullOrEmpty(apiResponse.Message) ? "请求失败" : apiResponse.Message);
        }

        return apiResponse.Data;
    }

    /// <summary>
    /// 统一发送请求：附加 Token、处理响应、反序列化 ApiResponse
    /// </summary>
    /// <typeparam name="T">业务数据类型</typeparam>
    /// <param name="method">HTTP 方法</param>
    /// <param name="url">相对地址</param>
    /// <param name="body">请求体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>业务数据</returns>
    private async Task<T?> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);

        // 自动从 localStorage 读取 Token 并附加到 Authorization 头
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用后端 API 失败：{Method} {Url}", method, url);
            throw new ApiException(HttpStatusCode.ServiceUnavailable, "网络异常，请稍后重试");
        }

        // 401：清除 Token 并跳转登录页
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            _logger.LogWarning("API 返回 401 未认证：{Url}", url);
            _navigationManager.NavigateTo("/login", forceLoad: true);
            throw new ApiException(HttpStatusCode.Unauthorized, "登录已过期，请重新登录");
        }

        // 403：提示无权限
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("API 返回 403 无权限：{Url}", url);
            throw new ApiException(HttpStatusCode.Forbidden, "无权限执行此操作");
        }

        // 反序列化 ApiResponse<T>
        ApiResponse<T>? apiResponse;
        try
        {
            apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "反序列化响应失败：{Url}", url);
            throw new ApiException(response.StatusCode, "响应格式错误");
        }

        if (apiResponse is null)
        {
            throw new ApiException(response.StatusCode, "响应为空");
        }

        // 业务失败（Code 非 2xx）
        if (apiResponse.Code < 200 || apiResponse.Code >= 300)
        {
            throw new ApiException(response.StatusCode, string.IsNullOrEmpty(apiResponse.Message) ? "请求失败" : apiResponse.Message);
        }

        return apiResponse.Data;
    }
}
