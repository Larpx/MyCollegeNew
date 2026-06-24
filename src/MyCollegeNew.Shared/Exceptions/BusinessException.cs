namespace Larpx.PersonalTools.MyCollegeNew.Shared.Exceptions;

/// <summary>
/// 业务异常，用于在业务流程中显式抛出可预期的错误
/// </summary>
public class BusinessException : Exception
{
    /// <summary>业务错误码，默认 400</summary>
    public int Code { get; }

    /// <summary>
    /// 构造业务异常
    /// </summary>
    public BusinessException(string message, int code = 400) : base(message)
    {
        Code = code;
    }
}
