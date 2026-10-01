using System;

namespace Top.Client.Features.Party;

public record PendingInvite(uint InviterId, string Name, DateTime ReceivedAt);
