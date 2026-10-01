using System.Collections.Generic;
using Top.Client.Messages.Account.Data;

namespace Top.Client.Features.Lobby.Select;

public record CharacterSelectPayload(
    IReadOnlyList<CharacterSlot> Characters,
    int SlotCap,
    bool HasSecurityCode,
    string? EntryFailureText = null
);
