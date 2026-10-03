using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Messages;

namespace Top.Client.Cli.Core;

public static class ServerEventType
{
    public static IReadOnlyList<Type> All { get; } = typeof(IServerEvent).Assembly.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IServerEvent).IsAssignableFrom(type))
        .ToArray();

    public static Type? Find(string name)
    {
        return All.FirstOrDefault(type => type.Name == name);
    }
}
