using MediatR; using Microsoft.Extensions.Logging;
namespace Finance.Application.Behaviors;
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull {
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) { _logger = logger; }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) {
        _logger.LogInformation("Handling {Name}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {Name}", typeof(TRequest).Name);
        return response;
    }
}
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull {
    private readonly IEnumerable<FluentValidation.IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<FluentValidation.IValidator<TRequest>> validators) { _validators = validators; }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) {
        if (!_validators.Any()) return await next();
        var ctx = new FluentValidation.ValidationContext<TRequest>(request);
        var failures = _validators.Select(v => v.Validate(ctx)).SelectMany(r => r.Errors).Where(f => f != null).ToList();
        if (failures.Count != 0) throw new FluentValidation.ValidationException(failures);
        return await next();
    }
}
