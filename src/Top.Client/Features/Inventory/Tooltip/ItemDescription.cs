using System.Collections.Generic;

namespace Top.Client.Features.Inventory.Tooltip;

public record ItemDescription(
    string Name,
    ItemNameTone NameTone,
    int GemColor,
    int? Level,
    string? TypeLine,
    string? Text,
    IReadOnlyList<string> Fits,
    IReadOnlyList<ItemGemLevel> GemLevels,
    IReadOnlyList<ItemRow> Rows,
    IReadOnlyList<ItemSocket?> Sockets,
    ItemRestrictions Restrictions,
    string? Stack,
    long? TradeValue,
    bool IsLocked,
    ItemExpiry? Expiry);
