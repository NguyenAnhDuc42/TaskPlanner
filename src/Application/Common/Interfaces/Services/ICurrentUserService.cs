using System;

namespace src.Infrastructure.Abstractions.IServices;

public interface ICurrentUserService
{
    Guid CurrentUserId();
}
