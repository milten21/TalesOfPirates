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
        [TestCase(Opcode.EnterMap, 516)]
        [TestCase(Opcode.ChaBeginSee, 504)]
        [TestCase(Opcode.ChaEndSee, 505)]
        [TestCase(Opcode.BeginAction, 6)]
        [TestCase(Opcode.NotiAction, 508)]
        [TestCase(Opcode.FailedAction, 520)]
        [TestCase(Opcode.ServerPublicKey, 943)]
        [TestCase(Opcode.PrivateKey, 355)]
        [TestCase(Opcode.Ping, 515)]
        [TestCase(Opcode.PingReply, 15)]
        [TestCase(Opcode.CheckPing, 537)]
        [TestCase(Opcode.CheckPingReply, 17)]
        [TestCase(Opcode.GroupPing, 5022)]
        [TestCase(Opcode.GroupPingReply, 6022)]
        public void An_opcode_counts_off_the_base_the_original_declares(ushort opcode, int expected)
        {
            Assert.That(opcode, Is.EqualTo(expected));
        }
    }
}
