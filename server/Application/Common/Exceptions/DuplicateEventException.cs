using System;

namespace Application.Common.Exceptions;

public class DuplicateEventException : Exception
{
    public string DeduplicationKey { get; }
    public DuplicateEventException(string deduplicationKey)
        : base($"Event with deduplication key '{deduplicationKey}' already exists")
    {
        DeduplicationKey = deduplicationKey;
    }
}