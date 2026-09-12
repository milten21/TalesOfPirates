namespace Top.Legacy.Protocol.Packets
{
    public static class Opcode
    {
        private const ushort ToGame = 0;
        private const ushort ToClient = 500;
        private const ushort FromGroup = 5000;
        private const ushort ToGroup = 6000;
        private const ushort RoleToGame = ToGame + 300;
        private const ushort CharacterToGame = ToGame + 430;
        private const ushort CharacterToClient = ToClient + 430;

        public const ushort ServerPublicKey = CharacterToClient + 13;
        public const ushort PrivateKey = RoleToGame + 55;

        public const ushort Login = CharacterToGame + 1;
        public const ushort LoginReply = CharacterToClient + 1;
        public const ushort Logout = CharacterToGame + 2;

        public const ushort BeginPlay = CharacterToGame + 3;
        public const ushort BeginPlayReply = CharacterToClient + 3;

        public const ushort EnterMap = ToClient + 16;
        public const ushort ChaBeginSee = ToClient + 4;
        public const ushort ChaEndSee = ToClient + 5;

        public const ushort BeginAction = ToGame + 6;
        public const ushort NotiAction = ToClient + 8;
        public const ushort FailedAction = ToClient + 20;

        public const ushort Ping = ToClient + 15;
        public const ushort PingReply = ToGame + 15;
        public const ushort CheckPing = ToClient + 37;
        public const ushort CheckPingReply = ToGame + 17;
        public const ushort GroupPing = FromGroup + 22;
        public const ushort GroupPingReply = ToGroup + 22;
    }
}
