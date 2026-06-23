namespace Campus.Attendance.Core.Exceptions;

/// <summary>
/// 业务异常，用于在业务流程中显式抛出可预期的错误，由全局异常中间件统一捕获并转换为友好响应
/// </summary>
public class BusinessException : Exception
{
    /// <summary>业务错误码，默认 400</summary>
    public int Code { get; }

    /// <summary>
    /// 构造业务异常
    /// </summary>
    /// <param name="message">错误信息（可对外暴露）</param>
    /// <param name="code">业务错误码，默认 400</param>
    public BusinessException(string message, int code = 400)
        : base(message)
    {
        Code = code;
    }
}
