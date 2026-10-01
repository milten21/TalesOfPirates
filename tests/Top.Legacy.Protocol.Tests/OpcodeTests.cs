using NUnit.Framework;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class OpcodeTests
    {
        [TestCase(Opcode.Login, 431)]
        [TestCase(Opcode.LoginReply, 931)]
        [TestCase(Opcode.Logout, 432)]
        [TestCase(Opcode.BeginPlay, 433)]
        [TestCase(Opcode.BeginPlayReply, 933)]
        [TestCase(Opcode.EndPlay, 434)]
        [TestCase(Opcode.EndPlayReply, 934)]
        [TestCase(Opcode.DeleteCharacter, 436)]
        [TestCase(Opcode.DeleteCharacterReply, 936)]
        [TestCase(Opcode.CreateSecondPassword, 346)]
        [TestCase(Opcode.CreateSecondPasswordReply, 941)]
        [TestCase(Opcode.ChangeSecondPassword, 347)]
        [TestCase(Opcode.ChangeSecondPasswordReply, 942)]
        [TestCase(Opcode.EnterMap, 516)]
        [TestCase(Opcode.CharacterBeginSee, 504)]
        [TestCase(Opcode.CharacterEndSee, 505)]
        [TestCase(Opcode.BeginAction, 6)]
        [TestCase(Opcode.EndAction, 7)]
        [TestCase(Opcode.NotiAction, 508)]
        [TestCase(Opcode.AttributeSync, 509)]
        [TestCase(Opcode.FailedAction, 520)]
        [TestCase(Opcode.PreMoveTime, 536)]
        [TestCase(Opcode.KitbagLock, 31)]
        [TestCase(Opcode.KitbagUnlock, 32)]
        [TestCase(Opcode.KitbagLockState, 553)]
        [TestCase(Opcode.TemporaryKitbagSync, 35)]
        [TestCase(Opcode.TemporaryKitbagSyncReply, 554)]
        [TestCase(Opcode.ServerPublicKey, 943)]
        [TestCase(Opcode.PrivateKey, 355)]
        [TestCase(Opcode.Ping, 515)]
        [TestCase(Opcode.PingReply, 15)]
        [TestCase(Opcode.CheckPing, 537)]
        [TestCase(Opcode.CheckPingReply, 17)]
        [TestCase(Opcode.GroupPing, 5022)]
        [TestCase(Opcode.GroupPingReply, 6022)]
        [TestCase(Opcode.PartyInvite, 5001)]
        [TestCase(Opcode.PartyRefresh, 5002)]
        [TestCase(Opcode.PartyInviteCancel, 5003)]
        [TestCase(Opcode.PartyAccept, 6002)]
        [TestCase(Opcode.PartyRefuse, 6003)]
        [TestCase(Opcode.PartyMemberVitals, 519)]
        [TestCase(Opcode.PartyLeaderNotice, 524)]
        public void An_opcode_counts_off_the_base_the_original_declares(ushort opcode, int expected)
        {
            Assert.That(opcode, Is.EqualTo(expected));
        }
    }
}
