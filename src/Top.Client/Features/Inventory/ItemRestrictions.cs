using System;

namespace Top.Client.Features.Inventory;

[Flags]
public enum ItemRestrictions
{
    None = 0,
    Trade = 1,
    Throw = 2,
    Delete = 4,
}
