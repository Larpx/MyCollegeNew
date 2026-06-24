namespace Larpx.PersonalTools.MyCollegeNew.Shared.Responses;

/// <summary>
/// 统一 API 响应包装类
/// </summary>
public class ApiResponse<T>
{
    /// <summary>状态码</summary>
    public int Code { get; set; }

    /// <summary>提示信息</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>业务数据</summary>
    public T? Data { get; set; }

    /// <summary>构造成功响应</summary>
    public static ApiResponse<T> Success(T data, string message = "操作成功")
        => new() { Code = 200, Message = message, Data = data };

    /// <summary>构造无数据的成功响应</summary>
    public static ApiResponse<T> Success(string message = "操作成功")
        => new() { Code = 200, Message = message, Data = default };

    /// <summary>构造失败响应</summary>
    public static ApiResponse<T> Fail(string message, int code = 400)
        => new() { Code = code, Message = message, Data = default };
}

/// <summary>
/// 分页查询结果包装类
/// </summary>
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

    /// <summary>构造分页结果</summary>
    public static PagedResult<T> Create(List<T> items, long total, int pageIndex, int pageSize)
        => new() { Items = items, Total = total, PageIndex = pageIndex, PageSize = pageSize };
}
