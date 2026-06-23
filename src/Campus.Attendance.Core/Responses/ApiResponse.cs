namespace Campus.Attendance.Core.Responses;

/// <summary>
/// 统一 API 响应包装类，所有控制器返回结果统一使用此结构
/// </summary>
/// <typeparam name="T">业务数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>状态码：200成功，400参数错误，401未认证，403无权限，404未找到，500服务器错误</summary>
    public int Code { get; set; }

    /// <summary>提示信息</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>业务数据</summary>
    public T? Data { get; set; }

    /// <summary>
    /// 构造成功响应
    /// </summary>
    /// <param name="data">业务数据</param>
    /// <param name="message">提示信息</param>
    /// <returns>成功响应对象</returns>
    public static ApiResponse<T> Success(T data, string message = "操作成功")
        => new() { Code = 200, Message = message, Data = data };

    /// <summary>
    /// 构造失败响应
    /// </summary>
    /// <param name="message">错误提示</param>
    /// <param name="code">状态码，默认 400</param>
    /// <returns>失败响应对象</returns>
    public static ApiResponse<T> Fail(string message, int code = 400)
        => new() { Code = code, Message = message, Data = default };
}

/// <summary>
/// 分页查询结果包装类
/// </summary>
/// <typeparam name="T">列表项类型</typeparam>
public class PagedResult<T>
{
    /// <summary>总记录数</summary>
    public long Total { get; set; }

    /// <summary>当前页数据列表</summary>
    public List<T> Items { get; set; } = new();

    /// <summary>当前页码（从 1 开始）</summary>
    public int PageIndex { get; set; }

    /// <summary>每页大小</summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 构造分页结果
    /// </summary>
    /// <param name="items">当前页数据</param>
    /// <param name="total">总记录数</param>
    /// <param name="pageIndex">当前页码</param>
    /// <param name="pageSize">每页大小</param>
    public static PagedResult<T> Create(List<T> items, long total, int pageIndex, int pageSize)
        => new() { Items = items, Total = total, PageIndex = pageIndex, PageSize = pageSize };
}
