namespace Campus.Attendance.Shared.Exceptions;

/// <summary>
/// 校验异常，包含所有校验失败信息
/// </summary>
public class ValidationException : BusinessException
{
    /// <summary>校验错误列表</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// 构造校验异常
    /// </summary>
    public ValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors), 400)
    {
        Errors = errors;
    }
}
