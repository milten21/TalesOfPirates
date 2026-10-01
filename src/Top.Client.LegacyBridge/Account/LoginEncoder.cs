using System;
using Top.Client.Messages.Account.Commands;
using Top.Legacy.Protocol;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Account
{
    public class LoginEncoder
    {
        private readonly GateSettings _settings;

        public LoginEncoder(GateSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Encode(LogInCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new LoginRequest(command.Account, command.Password, _settings.ClientVersion).Write(packet);
        }
    }
}
