using FluentValidation;
using MediatR;
using OrderFlow.Domain.Common;

namespace OrderFlow.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationFailures = _validators
            .Select(validator => validator.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(failure => failure != null)
            .ToList();

        if (validationFailures.Count != 0)
        {
            var firstFailure = validationFailures[0];
            var error = Error.Validation(firstFailure.PropertyName, firstFailure.ErrorMessage);

            // Using reflection or generic helper to create Result.Failure<T>
            return CreateFailureResult(error);
        }

        return await next(cancellationToken);
    }

    private static TResponse CreateFailureResult(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (Result.Failure(error) as TResponse)!;
        }

        var genericType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethods()
            .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
            .MakeGenericMethod(genericType);

        return (TResponse)failureMethod.Invoke(null, [error])!;
    }
}
