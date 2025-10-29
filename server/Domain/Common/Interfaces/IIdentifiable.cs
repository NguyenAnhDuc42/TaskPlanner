using System;

namespace Domain.Common.Interfaces;

public interface IIdentifiable
{
    Guid Id { get; }
}
