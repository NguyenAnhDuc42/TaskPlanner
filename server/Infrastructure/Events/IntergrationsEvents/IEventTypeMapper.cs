using System;

namespace Infrastructure.Events.IntergrationsEvents;

public interface IEventTypeMapper
{
    Type? GetClrType(string publicName);
    string GetPublicName(Type clrType);
    void Register(string publicName, Type clrType);
}
