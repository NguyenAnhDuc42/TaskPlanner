using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using FluentValidation;

namespace Application.Behaviors;

public static class ValidationDecorator
{
    private static Result<TResponse>? Validate<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators, TRequest request)
    {
        if (!validators.Any()) return null;

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count > 0)
        {
            var errorMessage = string.Join(" | ", failures.Select(f => f.ErrorMessage));
            return Error.Validation("Request.ValidationFailed", errorMessage);
        }

        return null;
    }

    private static Result? Validate<TRequest>(IEnumerable<IValidator<TRequest>> validators, TRequest request)
    {
        if (!validators.Any()) return null;

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count > 0)
        {
            var errorMessage = string.Join(" | ", failures.Select(f => f.ErrorMessage));
            return Error.Validation("Request.ValidationFailed", errorMessage);
        }

        return null;
    }

    internal class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
        where TQuery : IQueryRequest<TResponse>
    {
        private readonly IQueryHandler<TQuery, TResponse> _inner;
        private readonly IEnumerable<IValidator<TQuery>> _validators;

        public QueryHandler(IQueryHandler<TQuery, TResponse> inner, IEnumerable<IValidator<TQuery>> validators)
        {
            _inner = inner;
            _validators = validators;
        }

        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            var validationResult = Validate<TQuery, TResponse>(_validators, query);
            if (validationResult is not null) return validationResult; 

            return await _inner.Handle(query, cancellationToken);
        }
    }

    internal class CommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommandRequest<TResponse>
    {
        private readonly ICommandHandler<TCommand, TResponse> _inner;
        private readonly IEnumerable<IValidator<TCommand>> _validators;

        public CommandHandler(ICommandHandler<TCommand, TResponse> inner, IEnumerable<IValidator<TCommand>> validators)
        {
            _inner = inner;
            _validators = validators;
        }

        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var validationResult = Validate<TCommand, TResponse>(_validators, command);
            if (validationResult is not null) return validationResult;

            return await _inner.Handle(command, cancellationToken);
        }
    }

    internal class CommandBaseHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommandRequest
    {
        private readonly ICommandHandler<TCommand> _inner;
        private readonly IEnumerable<IValidator<TCommand>> _validators;

        public CommandBaseHandler(ICommandHandler<TCommand> inner, IEnumerable<IValidator<TCommand>> validators)
        {
            _inner = inner;
            _validators = validators;
        }

        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var validationResult = Validate(_validators, command);
            if (validationResult is not null) return validationResult;

            return await _inner.Handle(command, cancellationToken);
        }
    }
}
