using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Application.Behaviors;

public static class LoggingDecorator
{
    internal class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse> 
        where TQuery : IQueryRequest<TResponse>
    {
        private readonly IQueryHandler<TQuery, TResponse> _inner;
        private readonly ILogger<IQueryHandler<TQuery, TResponse>> _logger;

        public QueryHandler(IQueryHandler<TQuery, TResponse> inner, ILogger<IQueryHandler<TQuery, TResponse>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<Result<TResponse>> Handle(TQuery request, CancellationToken cancellationToken)
        {
            string requestName = typeof(TQuery).Name;
            _logger.LogInformation("Executing query: {Query}", requestName);
            var result = await _inner.Handle(request, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Query executed: {RequestName}", requestName);
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    _logger.LogError("Query failed: {RequestName}", requestName);
                }
            }
            return result;
        }
    }

    internal class CommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse> 
        where TCommand : ICommandRequest<TResponse>
    {
        private readonly ICommandHandler<TCommand, TResponse> _inner;
        private readonly ILogger<ICommandHandler<TCommand, TResponse>> _logger;

        public CommandHandler(ICommandHandler<TCommand, TResponse> inner, ILogger<ICommandHandler<TCommand, TResponse>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<Result<TResponse>> Handle(TCommand request, CancellationToken cancellationToken)
        {
            string requestName = typeof(TCommand).Name;
            _logger.LogInformation("Executing command: {requestName}", requestName);
            var result = await _inner.Handle(request, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Command executed: {RequestName}", requestName);
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    _logger.LogError("Command failed: {RequestName}", requestName);
                }
            }
            return result;
        }
    }

    internal class CommandBaseHandler<TCommand> : ICommandHandler<TCommand> 
        where TCommand : ICommandRequest
    {
        private readonly ICommandHandler<TCommand> _inner;
        private readonly ILogger<ICommandHandler<TCommand>> _logger;

        public CommandBaseHandler(ICommandHandler<TCommand> inner, ILogger<ICommandHandler<TCommand>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<Result> Handle(TCommand request, CancellationToken cancellationToken)
        {
            string requestName = typeof(TCommand).Name;
            _logger.LogInformation("Executing command: {requestName}", requestName);
            var result = await _inner.Handle(request, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Command executed: {RequestName}", requestName);
            }
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    _logger.LogError("Command failed: {RequestName}", requestName);
                }
            }
            return result;
        }
    }
}