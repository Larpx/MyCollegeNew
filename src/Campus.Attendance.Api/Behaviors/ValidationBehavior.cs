using Campus.Attendance.Shared.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Campus.Attendance.Api.Behaviors;

/// <summary>
/// MediatR 管线行为：在 Handler 执行前自动触发 FluentValidation 校验，校验失败抛出 ValidationException
/// </summary>
/// <typeparam name="TRequest">请求类型</typeparam>
/// <typeparam name="TResponse">响应类型</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="validators">请求类型的所有验证器</param>
    /// <param name="logger">日志器</param>
    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    /// <summary>
    /// 管线处理：先校验，通过后执行下一个处理器
    /// </summary>
    /// <param name="request">请求对象</param>
    /// <param name="next">下一个处理器委托</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应对象</returns>
    /// <exception cref="Shared.Exceptions.ValidationException">校验失败时抛出</exception>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => f.ErrorMessage)
            .ToList();

        if (failures.Count != 0)
        {
            _logger.LogWarning("请求 {RequestType} 校验失败: {Errors}", typeof(TRequest).Name, string.Join("; ", failures));
            throw new Shared.Exceptions.ValidationException(failures);
        }

        return await next();
    }
}
