using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Campus.Attendance.Shared.Constants;
using Campus.Attendance.Shared.Responses;
using Microsoft.AspNetCore.Components;

using Msg = Campus.Attendance.Shared.Constants.MessageConstants;

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
/// API 客户端实现：通过 HttpOnly Cookie 读取 JWT 并附加到 API 请求头
/// 401 跳转登录页，403 提示无权限
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly TokenService _tokenService;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<ApiClient> _logger;

    /// <summary>构造函数，依赖注入 HttpClient、TokenService、NavigationManager、Logger</summary>
    /// <param name="httpClient">已配置 BaseAddress 的 HttpClient</param>
    /// <param name="tokenService">Cookie-based Token 服务</param>
    /// <param name="navigationManager">导航管理器，用于 401 跳转</param>
    /// <param name="logger">日志记录器</param>
    public ApiClient(HttpClient httpClient, TokenService tokenService, NavigationManager navigationManager, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
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
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
        var response = await SendRequestAsync(request, url, cancellationToken);
        HandleAuthErrors(response, url);
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
        using var request = CreateAuthenticatedRequest(method, url);
        request.Content = content;
        var response = await SendRequestAsync(request, url, cancellationToken);
        HandleAuthErrors(response, url);
        return await DeserializeApiResponseAsync<T>(response, url, cancellationToken);
    }

    /// <summary>
    /// 统一发送请求：附加 Token、处理响应、反序列化 ApiResponse
    /// </summary>
    private async Task<T?> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var request = CreateAuthenticatedRequest(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        var response = await SendRequestAsync(request, url, cancellationToken);
        HandleAuthErrors(response, url);
        return await DeserializeApiResponseAsync<T>(response, url, cancellationToken);
    }

    /// <summary>
    /// 创建已附加 JWT Token 的 HttpRequestMessage（从 HttpOnly Cookie 读取）
    /// </summary>
    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        var token = _tokenService.GetToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
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
    private void HandleAuthErrors(HttpResponseMessage response, string url)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _tokenService.RemoveToken();
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
