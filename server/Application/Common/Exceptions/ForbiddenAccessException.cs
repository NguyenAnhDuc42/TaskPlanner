using System;

namespace Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("You do not have permission to perform this action.") { }

    public ForbiddenAccessException(string message) : base(message) { }

    public ForbiddenAccessException(string resource, string action)
        : base($"You do not have permission to {action} {resource}.") { }
}
