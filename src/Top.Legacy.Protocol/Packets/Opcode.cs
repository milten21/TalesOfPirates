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
        public const ushort EndPlay = CharacterToGame + 4;
        public const ushort EndPlayReply = CharacterToClient + 4;

        public const ushort DeleteCharacter = CharacterToGame + 6;
        public const ushort DeleteCharacterReply = CharacterToClient + 6;

        public const ushort CreateSecondPassword = RoleToGame + 46;
        public const ushort CreateSecondPasswordReply = CharacterToClient + 11;

        public const ushort ChangeSecondPassword = RoleToGame + 47;
        public const ushort ChangeSecondPasswordReply = CharacterToClient + 12;

        public const ushort EnterMap = ToClient + 16;
        public const ushort CharacterBeginSee = ToClient + 4;
        public const ushort CharacterEndSee = ToClient + 5;

        public const ushort BeginAction = ToGame + 6;
        public const ushort EndAction = ToGame + 7;
        public const ushort NotiAction = ToClient + 8;
        public const ushort AttributeSync = ToClient + 9;
        public const ushort FailedAction = ToClient + 20;
        public const ushort PreMoveTime = ToClient + 36;

        public const ushort KitbagLock = ToGame + 31;
        public const ushort KitbagUnlock = ToGame + 32;
        public const ushort KitbagLockState = ToClient + 53;

        public const ushort TemporaryKitbagSync = ToGame + 35;
        public const ushort TemporaryKitbagSyncReply = ToClient + 54;

        public const ushort Ping = ToClient + 15;
        public const ushort PingReply = ToGame + 15;
        public const ushort CheckPing = ToClient + 37;
        public const ushort CheckPingReply = ToGame + 17;
        public const ushort GroupPing = FromGroup + 22;
        public const ushort GroupPingReply = ToGroup + 22;

        public const ushort PartyInvite = FromGroup + 1;
        public const ushort PartyRefresh = FromGroup + 2;
        public const ushort PartyInviteCancel = FromGroup + 3;
        public const ushort PartyMemberVitals = ToClient + 19;
        public const ushort PartyLeaderNotice = ToClient + 24;
        public const ushort PartyAccept = ToGroup + 2;
        public const ushort PartyRefuse = ToGroup + 3;
    }
}
