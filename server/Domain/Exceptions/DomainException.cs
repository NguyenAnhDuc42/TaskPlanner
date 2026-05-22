using System;

namespace Domain;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

