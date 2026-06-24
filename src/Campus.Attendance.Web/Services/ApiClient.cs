using System.Net;
using System.Net.Http.Json;
using Campus.Attendance.Core.Constants;
using Campus.Attendance.Core.Responses;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Msg = Campus.Attendance.Core.Constants.MessageConstants;

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

    /// <summary>localStorage 中存储 Token 的键名</summary>
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
        using var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, url, cancellationToken);
        var response = await SendRequestAsync(request, url, cancellationToken);
        await HandleAuthErrors(response, url);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(response.StatusCode, Msg.Common.DownloadFailed);
        }
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// 以 multipart/form-data 方式发送请求并反序列化 ApiResponse
    /// </summary>
    private async Task<T?> SendFormAsync<T>(HttpMethod method, string url, MultipartFormDataContent content, CancellationToken cancellationToken)
    {
        using var request = await CreateAuthenticatedRequestAsync(method, url, cancellationToken);
        request.Content = content;
        var response = await SendRequestAsync(request, url, cancellationToken);
        await HandleAuthErrors(response, url);
        return await DeserializeApiResponseAsync<T>(response, url, cancellationToken);
    }

    /// <summary>
    /// 统一发送请求：附加 Token、处理响应、反序列化 ApiResponse
    /// </summary>
    private async Task<T?> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var request = await CreateAuthenticatedRequestAsync(method, url, cancellationToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        var response = await SendRequestAsync(request, url, cancellationToken);
        await HandleAuthErrors(response, url);
        return await DeserializeApiResponseAsync<T>(response, url, cancellationToken);
    }

    /// <summary>
    /// 创建已附加 JWT Token 的 HttpRequestMessage
    /// </summary>
    private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", cancellationToken, TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        return request;
    }

    /// <summary>
    /// 发送 HTTP 请求并处理网络异常
    /// </summary>
    private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, string url, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用后端 API 失败：{Method} {Url}", request.Method, url);
            throw new ApiException(HttpStatusCode.ServiceUnavailable, Msg.Common.NetworkError);
        }
    }

    /// <summary>
    /// 处理 401/403 认证与授权错误
    /// </summary>
    private async Task HandleAuthErrors(HttpResponseMessage response, string url)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            _logger.LogWarning("API 返回 401 未认证：{Url}", url);
            _navigationManager.NavigateTo("/login", forceLoad: true);
            throw new ApiException(HttpStatusCode.Unauthorized, Msg.Common.TokenExpired);
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("API 返回 403 无权限：{Url}", url);
            throw new ApiException(HttpStatusCode.Forbidden, Msg.Common.NoPermission);
        }
    }

    /// <summary>
    /// 反序列化 ApiResponse 并校验业务状态码
    /// </summary>
    private async Task<T?> DeserializeApiResponseAsync<T>(HttpResponseMessage response, string url, CancellationToken cancellationToken)
    {
        ApiResponse<T>? apiResponse;
        try
        {
            apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "反序列化响应失败：{Url}", url);
            throw new ApiException(response.StatusCode, Msg.Common.ResponseFormatError);
        }

        if (apiResponse is null)
        {
            throw new ApiException(response.StatusCode, Msg.Common.ResponseEmpty);
        }

        if (apiResponse.Code < 200 || apiResponse.Code >= 300)
        {
            throw new ApiException(response.StatusCode, string.IsNullOrEmpty(apiResponse.Message) ? Msg.Common.RequestFailed : apiResponse.Message);
        }

        return apiResponse.Data;
    }
}
