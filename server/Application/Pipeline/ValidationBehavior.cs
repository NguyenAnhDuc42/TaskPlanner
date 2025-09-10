using System;
using FluentValidation;
using MediatR;

namespace Application.Pipeline;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validator;
    public ValidationBehavior(IValidator<TRequest> validator)
    {
        _validator = new List<IValidator<TRequest>>() { validator };
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validator.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = _validator
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

            if (failures.Count > 0)
                throw new ValidationException(failures);

        }

        return await next();
    }
}
