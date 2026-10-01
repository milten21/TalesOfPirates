namespace Top.Client.LegacyBridge
{
    public static class ServerError
    {
        private const int GateLineError = 1;
        private const int NotSelectingCharacter = 2;
        private const int NotPlaying = 3;
        private const int MapNotReached = 4;
        private const int ServerFull = 5;
        private const int VersionMismatch = 7;
        private const int EnterFailed = 8;
        private const int EnterPositionIllegal = 9;

        private const int GroupServerLinkError = 521;
        private const int AccountServerFailure = 522;
        private const int InvalidCharacter = 524;
        private const int GuildLeader = 530;
        private const int ServerBusy = 532;
        private const int NoSecondPassword = 534;
        private const int CharacterOnline = 538;
        private const int BadBoy = 550;

        private const int InvalidAccount = 1001;
        private const int WrongPassword = 1002;
        private const int ActivationFailed = 1003;
        private const int AccountLogged = 1004;
        private const int LogoutSaving = 1005;
        private const int BannedAccount = 1006;
        private const int GroupServerLogged = 1011;
        private const int GroupServerAuthFailed = 1012;
        private const int AccountOnline = 1104;
        private const int CharacterSaving = 1105;
        private const int LoggedTwice = 1106;
        private const int GroupServerDisconnected = 1107;
        private const int UnknownCommand = 1108;
        private const int LocalStoreError = 1109;
        private const int ExpiredAccount = 1110;

        public static string ToText(int code)
        {
            return code switch
            {
                GateLineError => "Discovered exceptional line error on GateServer",
                NotSelectingCharacter => "current not yet handled character state",
                NotPlaying => "Currently not in gameplay, unable to send ENDPLAY command",
                MapNotReached => "target map cannot be reached",
                ServerFull => "This server is currently full, please select another server!",
                VersionMismatch => "Client version error, server refused connection!",
                EnterFailed => "failed to enter map!",
                EnterPositionIllegal => "Map position illegal, you'll be sent back to your birth city, please relog!",
                GroupServerLinkError => "server link operation integrality error",
                AccountServerFailure => "Account server has encountered a malfunction",
                InvalidCharacter => "Current account does not have a request (Select/Delete) to character",
                GuildLeader => "Disband your guild before you delete its leader",
                ServerBusy => "The server is busy. Try again later",
                NoSecondPassword => "Cha second password not created",
                CharacterOnline => "This character is in the game on another session",
                BadBoy =>
                    "My child, you are very bold. You have been reported to the authority. Please do not commit the offense again!",
                InvalidAccount => "Invalid Account",
                WrongPassword => "Password incorrect",
                ActivationFailed => "Account activation failed",
                AccountLogged => "This account is already online",
                LogoutSaving => "Your cha is currently in logout save mode, please try logging in again later.",
                BannedAccount => "Account has been banned",
                GroupServerLogged => "This GroupServer has login",
                GroupServerAuthFailed => "This GroupServer Verification failed",
                AccountOnline => "Your account is already online",
                CharacterSaving => "Saving your character, please try again in 15 seconds...",
                LoggedTwice => "Your account is logged on far away",
                GroupServerDisconnected => "GroupServer disconnected",
                UnknownCommand => "unknown agreement, don't deal with",
                LocalStoreError => "local saving error",
                ExpiredAccount => "This account has expired, please topup!",
                _ => string.Empty
            };
        }
    }
}
