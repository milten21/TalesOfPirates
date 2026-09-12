using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Transport
{
    public interface IGateListener
    {
        void OnOpened();

        void OnReceived(ushort opcode, PacketReader packet);

        void OnClosed(CloseReason reason);
    }
}
