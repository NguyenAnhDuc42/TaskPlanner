namespace Application.Common.Interfaces;

public interface IBaseCommandRequest { }

public interface ICommandRequest : IBaseCommandRequest { }

public interface ICommandRequest<TResponse> : IBaseCommandRequest { }
