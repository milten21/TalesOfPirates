using System;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Messages.Play.Events
{
    public class ShipLookChangedEvent : IServerEvent
    {
        public ShipLookChangedEvent(uint worldId, ShipLook look)
        {
            WorldId = worldId;
            Look = look ?? throw new ArgumentNullException(nameof(look));
        }

        public uint WorldId { get; }

        public ShipLook Look { get; }
    }
}
