using System;

namespace Top.Client.Messages
{
    public interface IServerConnection
    {
        bool IsOpen { get; }

        bool IsLost { get; }

        void Connect(string host, int port, bool isEncrypted);

        void Close();

        void CloseAsLost();

        void PauseDrain();

        void ResumeDrain();

        void Drain();

        void Send(IServerCommand command);

        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IServerEvent;
    }
}
