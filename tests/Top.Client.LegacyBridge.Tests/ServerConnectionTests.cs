using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using NUnit.Framework;
using Top.Client.Messages;
using Top.Client.Messages.Events;
using Top.Client.Messages.Account.Commands;
using Top.Client.Messages.Account.Events;
using Top.Client.Messages.Inventory.Commands;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Inventory.Events;
using Top.Client.Messages.Party.Commands;
using Top.Client.Messages.Party.Data;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Contracts;
using Top.Legacy.Protocol;
using Top.Legacy.Protocol.Crypto;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;
using Top.Legacy.Protocol.Transport;
using Top.Logging;

namespace Top.Client.LegacyBridge.Tests
{
    public class ServerConnectionTests
    {
        private const string Host = "127.0.0.1";
        private const int Port = 1973;
        private const string Account = "blackbeard";
        private const string Password = "parrot123";
        private const int WrongPassword = 1002;
        private const int UnknownFailure = 1400;
        private const ushort InvalidCharacter = 524;
        private const ushort EnterFailed = 8;
        private const ushort NotPlaying = 3;
        private const int ServerBusy = 532;
        private const int NoSecondPassword = 534;
        private const int GuildLeader = 530;
        private const int CharacterOnline = 538;
        private const byte SeenNew = 0;
        private const byte SeenSwitch = 1;
        private const byte EnteredAtEdge = 0;
        private const byte EnteredByCarry = 1;
        private const uint CrabWorldId = 0xB00028CA;
        private const byte SkillAction = 2;
        private const int RightHandLink = 9;
        private const int FairyLink = 16;
        private const int FairyAppearanceLink = 24;
        private const short FairyOfLuck = 231;
        private const short FairyOfEvil = 237;
        private const short FairyOfLifeApparel = 5298;
        private const short ShortSword = 1;
        private const short ShortHair = 2000;
        private const short TotemMarker = 9998;
        private const byte AlreadyActing = 1;
        private const byte TaskSync = 4;
        private const ushort SystemInfo = 517;
        private const byte BaseMoveSpeed = 69;
        private const uint PinnedWorldId = 11;
        private const byte KitbagInit = 0;
        private const byte KitbagSwitch = 5;
        private const short Sword = 289;
        private const byte GoldAttribute = 8;
        private const uint PinnedFairyWorldId = 4;

        private const string PinnedRegenerationHex =
            "0000000B" + "08" + "0001" + "01" + "00000020";

        private const string PinnedLevelEightyHex =
            "0000000B" + "04" + "0012" +
            "00" + "00000050" + "01" + "00000843" + "02" + "00000104" + "06" + "000000FA" + "07" + "00000071" +
            "0F" + "EE6B2800" + "10" + "F8F387A5" + "11" + "E9E87690" + "1F" + "00000843" + "20" + "00000104" +
            "24" + "000000A8" + "25" + "000000A8" + "28" + "00000019" + "38" + "00000843" + "39" + "00000104" +
            "3D" + "000000A8" + "3E" + "000000A8" + "41" + "00000019";

        private const string PinnedSwordsmanHex =
            "0000000B" + "04" + "0007" +
            "04" + "00000001" + "1F" + "0000008C" + "23" + "00000005" + "28" + "00000003" + "38" + "0000008C" +
            "3C" + "00000005" + "41" + "00000003";

        private const string PinnedSpeedRaisedHex =
            "0000000B" + "04" + "0002" + "2C" + "00000258" + "45" + "00000258";

        private const string PinnedSwordsmanAtLevelEightyEntersHex =
            "00000000010000076761726E6572000100000000000000020000000B0000000B00035432006302002E2F0100035432000001" +
            "00000200000000000100000100000000000001000001000370F5000422E30000002800E10000000001000000000B01000000" +
            "010000000200080E000000000000000009FA0000000000000000000000000000000000010000000000010000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "00000000000000000000000000000000001900000800190101000000000000000000000000001C0001000000000000000000" +
            "000000001D000100000000000000000000000000200001000000000000000000000000002100010000000000000000000000" +
            "000023000100000000000000000000000000250001000000000000000000000000002600010000000000000000000000002B" +
            "442C8C0000004A00000000500100000843020000010403000000000400000001050000000006000000FA0700000071080000" +
            "000009000000000A000000010B000000010C000000010D000000000E000000000FEE6B280010917D1A6E11908FE6B2120000" +
            "00001300000000140000000015000002711600000000170000000018000005DC19000000051A000000051B000000051C0000" +
            "00051D000000051E000000051F00000843200000010421000000072200000007230000000524000000A825000000A8260000" +
            "0069270000000F280000001929000000012A000005942B000000002C000001E02D000000002E000000002F00000000300000" +
            "00003100000000320000000533000000053400000005350000000536000000053700000005380000084339000001043A0000" +
            "00073B000000073C000000053D000000A83E000000A83F00000069400000000F410000001942000000014300000594440000" +
            "000045000001E046000000004700000000480000000049000000000000180000012100000000000000012710271000000000" +
            "0001010000000000000000000000000100240002002F00010000000000000000000000000001028100000000000000012710" +
            "2710000000000001010000000000000000000000000100270001002400020000000000000000000000000002000800000000" +
            "000000012710271000000000000101000000000000000000000000010022000E002300120000000000000000000000000003" +
            "01B4000000000000000100000000000000000001010000000000000000000000000000042595000000000000000100000000" +
            "00000000000101000000000000000000000000000005000000060000000700000008000000090000000A0000000B0000000C" +
            "0000000D0000000E0000000F00000010000000110000001200000013000000140000001500000016000000170000FFFF0000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "000000000000000000000B";

        private const string PinnedFairyEntersHex =
            "00000000010000076761726E6572000100000000000000010000000400000004000A4F6273657276657232006302002E2D01" +
            "000A4F627365727665723200000100000100000000000100000100000000000001000001000367E0000440AC00000028005A" +
            "000000000100000000040100000001000000010007D0000000000000000009FA000000000000000000000000000000000001" +
            "0000000000010000000000000000000121000000000000000127102710000000000001010000000001000000000000000001" +
            "00240002002F0001000000000000000000000000000000000000000002810000000000000001271027100000000000010100" +
            "0000000100000000000000000100270001002400020000000000000000000000000000000000000000270F00000000000000" +
            "0000000000000000000000000000000001000000000000000000000000000000000000000000000000000008000000000000" +
            "0001271027100000000000010100000000010000000000000000010022000E00230012000000000000000000000000000000" +
            "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000E7000000" +
            "00000000010A5A1388009600F00001010000000001000000000001000001001A0003001C0004001D0001001B0002001E0005" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "00000000000014B2000000000000000161A861A8000000000001010000000001000000000000000001001A0000001C000000" +
            "1D0000001B0000001E0000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "00000000000000000000000000000000000000000000000000000000000000000000000000ED0100000000001C0000080019" +
            "0001000000000000000000000000001C0101000000000000000000000000001D0001000000000000000000000000001E0001" +
            "000000000000000000000000001F000100000000000000000000000000220001000000000000000000000000002300010000" +
            "00000000000000000000002600010000000000000000000000002BA706170000004A0000000001010000000B020000000603" +
            "000000000400000000050000000006000000040700000000080000000009000000000A000000010B000000010C000000010D" +
            "000000000E000000000F00000000100000000511000000001200000000130000000014000000001500000271160000000017" +
            "0000000018000005DC19000000081A000000091B000000071C000000061D0000000A1E000000051F0000006D200000002621" +
            "0000001A220000001E2300000007240000000C250000000C2600000069270000000F280000000329000000012A0000056C2B" +
            "000000002C000001E02D000000002E000000012F000000003000000000310000000032000000053300000005340000000535" +
            "0000000536000000053700000005380000006D39000000263A0000000C3B0000000C3C000000033D0000000C3E0000000B3F" +
            "00000069400000000F41000000034200000001430000056C440000000045000001E046000000004700000000480000000049" +
            "0000000000001800000000000100ED00000000000000011356138800F000F000010100000000000000000001000001001A00" +
            "00001C0000001D0000001B0000001E000000020000000301B400000000000000010000000000000000000101000000000000" +
            "0000000000000000042595000000000000000100000000000000000101010000000000000000000000000000050000000600" +
            "00000700000008000000090000000A0000000B0000000C0000000D0000000E0000000F000000100000001100000012000000" +
            "13000000140000001500000016000000170000FFFF0000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "00000000000000000000000000000000000000000000000000000000000000000004";

        private const string PinnedFairySwapHex =
            "0000000400000065050000010007D0000000000000000009FA00000000000000000000000000000000000100000000000100" +
            "0000000000000000012100000000000000012710271000000000000101000000000100000000000000000100240002002F00" +
            "0100000000000000000000000000000000000000000281000000000000000127102710000000000001010000000001000000" +
            "00000000000100270001002400020000000000000000000000000000000000000000270F0000000000000000000000000000" +
            "0000000000000000000100000000000000000000000000000000000000000000000000000800000000000000012710271000" +
            "00000000010100000000010000000000000000010022000E0023001200000000000000000000000000000000000000000000" +
            "000000000000000000000000000000000000000000000000000000000000000000000000000000ED00000000000000011356" +
            "138800F000F00001010000000001000000000001000001001A0000001C0000001D0000001B0000001E000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000014" +
            "B2000000000000000161A861A8000000000001010000000001000000000000000001001A0000001C0000001D0000001B0000" +
            "001E000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000";

        private const string PinnedFairyRemovedHex =
            "0000000400000067050000010007D0000000000000000009FA00000000000000000000000000000000000100000000000100" +
            "0000000000000000012100000000000000012710271000000000000101000000000100000000000000000100240002002F00" +
            "0100000000000000000000000000000000000000000281000000000000000127102710000000000001010000000001000000" +
            "0000000000010027000100240002000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000";

        private const string PinnedT1Member = "0000000C" + "0003" + "543100" + "000100" + "0001";

        private const string PinnedObserver2Member = "00000004" + "000A" + "4F627365727665723200" + "000100" + "0001";

        private const string PinnedPartyJoinHex = "01" + "02" + PinnedT1Member + PinnedObserver2Member;

        private const string PinnedMemberLeavesHex = "02" + "02" + PinnedT1Member + PinnedObserver2Member;

        private const string PinnedKickHex = "06" + "02" + PinnedT1Member + PinnedObserver2Member;

        private const string PinnedLeaderLeavesHex = "02" + "02" + PinnedObserver2Member + PinnedT1Member;

        private const string PinnedObserver2VitalsHex =
            "00000004" + "0000006D" + "0000006D" + "0000001C" + "00000026" + "00000001" +
            "0000010007D0000000000000000009FA00000000000000000000000000000000000100000000000100000000000000000001" +
            "2100000000000000012710271000000000000101000000000100000000000000000100240002002F00010000000000000000" +
            "0000000000000000000000000281000000000000000127102710000000000001010000000001000000000000000001002700" +
            "01002400020000000000000000000000000000000000000000270F0000000000000000000000000000000000000000000000" +
            "0100000000000000000000000000000000000000000000000000000800000000000000012710271000000000000101000000" +
            "00010000000000000000010022000E0023001200000000000000000000000000000000000000000000000000000000000000" +
            "000000000000000000000000000000000000000000000000000000000000E700000000000000010578138800F000F0000101" +
            "0000000001000000000003000001001A0003001C0004001D0001001B0002001E000500000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000014B20000000000000001" +
            "61A861A8000000000001010000000001000000000000000001001A0000001C0000001D0000001B0000001E00000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000";

        private const string PinnedLedByT1Hex = "00000004" + "0000000C";

        private const string PinnedLeaderClearedHex = "0000000C" + "00000000";

        private const string PinnedT1EntersHex =
            "00000000010000076761726E6572000100000000000000010000000C0000000C00035431000002002E2D0100035431000001" +
            "00000100000000000100000100000000000001000001000341A7000440890000002800000000000001000000000C01000000" +
            "01000000010007D0000000000000000009FA0000000000000000000000000000000000010000000000010000000000000000" +
            "0010D400000000000000012710271000000000000101000000000100000000000000000100240004002F0001000000000000" +
            "000000000000000000000000000010D600000000000000012710271000000000000101000000000100000000000000000100" +
            "270002002400030000000000000000000000000000000000000000270F000000000000000000000000000000000000000000" +
            "0000010000000000000000000000000000000000000000000000000000090000000000000001271027100000000000010100" +
            "000000010000000000000000010022001D002300210000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "00000000000000000000000000000000000000000000000000000000000000000000001C0000090019000100000000000000" +
            "0000000000001C0101000000000000000000000000001D0001000000000000000000000000001E0001000000000000000000" +
            "000000001F000100000000000000000000000000220001000000000000000000000000002300010000000000000000000000" +
            "000026000100000000000000000000000001DB0101000700000000000027100000300E26090000004A000000000801000000" +
            "CD020000002C0300000000040000000005000000000600000001070000000008000009C409000000000A000000010B000000" +
            "010C000000010D000000000E000000000F000003FC10000007B611000003E812000000001300000000140000000015000002" +
            "711600000000170000000018000005DC190000000F1A000000051B000000051C000000051D000000051E000000051F000000" +
            "CD200000002C2100000037220000003B23000000092400000018250000001A2600000069270000000F280000000329000000" +
            "012A000005942B000000002C000001E02D000000002E000000012F0000000030000000003100000000320000000F33000000" +
            "05340000000535000000053600000005370000000538000000CD390000002C3A0000001A3B0000001A3C000000023D000000" +
            "183E000000183F00000069400000000F410000000342000000014300000594440000000045000001E0460000000047000000" +
            "0048000000004900000000000018000001B50000000000000001000000000000000000010100000000000000000000000000" +
            "000100080000000000000001270A271000000000000101000000000000000000000000010022000E00230012000000000000" +
            "000000000000000201210000000000000001270E2710000000000001010000000000000000000000000100240002002F0001" +
            "0000000000000000000000000003028100000000000000012710271000000000000101000000000000000000000000010027" +
            "0001002400020000000000000000000000000004259500000000000000010000000000000000000101000000000000000000" +
            "00000000000501BC000000000000000100000000000000000001010000000000000000000000000000060000000700000008" +
            "000000090000000A0000000B0000000C0000000D0000000E0000000F00000010000000110000001200000013000000140000" +
            "001500000016000000170000FFFF0201DB000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "00000000000000000000000000000000000000000000000000000C";

        private const string PinnedT1SwordMovedHex =
            "0000000C" + "0000000A" + "06" + "05" + "0000" + "0000" + "0006" + "01B5" + "00000000" + "0000" + "0001" +
            "0000" + "0000" + "0000" + "0000" + "00" + "01" + "01" + "00000000" + "00000000" + "00000000" + "00" + "FFFF";

        private const string PinnedEmptyTemporaryBagHex = "00" + "0020" + "FFFF";

        private const string PinnedObserverMoveInLockedBagHex = "00000002" + "00000001" + "0F" + "0007";

        private const uint PinnedObserverWorldId = 2;

        private const string PinnedObserverVestThrownHex =
            "00000002" + "00000001" + "06" + "04" + "000D" + "0000" + "FFFF";

        private const string PinnedObserverThrowTooFarHex = "00000002" + "00000001" + "0F" + "0008";

        private const string PinnedT1ThreeApplesTakenHex =
            "0000000C" + "00000001" + "06" + "05" + "0006" + "0737" + "00000000" + "0000" + "0003" + "0000" + "0000" +
            "0000" + "0000" + "00" + "01" + "01" + "00000000" + "00000000" + "00000000" + "00" + "FFFF";

        private const string PinnedT1ElvenFruitLeftInTemporaryBag =
            "0001" + "0C2C" + "00000000" + "0000" + "0002" + "0000" + "0000" + "0000" + "0000" + "00" + "01" + "01" +
            "00000000" + "00000000" + "00000000" + "00" + "FFFF";

        private const string PinnedT1TemporaryBagAfterAppleTakeHex =
            "0000000C" + "00000001" + "1C" + "05" + "0000" + "0000" + PinnedT1ElvenFruitLeftInTemporaryBag;

        private const string PinnedT1TemporaryBagAfterTakeIntoFullBagHex =
            "0000000C" + "00000002" + "1C" + "05" + "0000" + "0000" + PinnedT1ElvenFruitLeftInTemporaryBag;

        private const string PinnedT1TakeIntoFullBagHex = "0000000C" + "00000002" + "0F" + "0002";

        private const uint PinnedT1WorldId = 12;

        private List<FakeGate> _gates;
        private List<IServerEvent> _delivered;
        private GateSettings _settings;
        private ServerConnection _connection;
        private LogCapture _log;
        private ILogWriter _savedWriter;

        [SetUp]
        public void BuildConnection()
        {
            _gates = [];
            _delivered = [];
            _settings = new GateSettings();
            _connection = new ServerConnection(_settings, OpenGate);
            _log = new LogCapture();
            _savedWriter = Log.Writer;
            Log.Writer = _log;
        }

        [TearDown]
        public void RestoreLog()
        {
            Log.Writer = _savedWriter;
        }

        [Test]
        public void The_target_is_given_at_connect_time()
        {
            _connection.Connect("top.example.com", 1974, false);

            Assert.That(Gate.Host, Is.EqualTo("top.example.com"));
            Assert.That(Gate.Port, Is.EqualTo(1974));
            Assert.That(Gate.IsEncrypted, Is.False);
        }

        [Test]
        public void Nothing_is_delivered_before_the_drain()
        {
            Subscribe<ConnectedEvent>();
            _connection.Connect(Host, Port, true);

            Gate.Listener.OnOpened();

            Assert.That(_delivered, Is.Empty);
        }

        [Test]
        public void An_open_connection_reports_connected_on_the_drain()
        {
            Subscribe<ConnectedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnOpened();

            _connection.Drain();

            Assert.That(_delivered, Has.Count.EqualTo(1).And.All.InstanceOf<ConnectedEvent>());
        }

        [Test]
        public void A_login_the_server_accepts_reports_the_character_names()
        {
            Subscribe<LoginSucceededEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.LoginReply, BuildAcceptedReply("Blackbeard", "Anne"));

            _connection.Drain();

            Assert.That(TakeSingle<LoginSucceededEvent>().Characters.Select(character => character.Name),
                Is.EqualTo(new[] { "Blackbeard", "Anne" }));
        }

        [Test]
        public void A_login_the_server_accepts_reports_what_each_character_is()
        {
            Subscribe<LoginSucceededEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.LoginReply, Pack(LoginReply.Accepted(
                3,
                [new CharacterSummary("Blackbeard", "Swordsman", 15, 7,
                    new ushort[CharacterSummary.EquipmentSlots])],
                false)));

            _connection.Drain();

            var character = TakeSingle<LoginSucceededEvent>().Characters[0];

            Assert.That(character.Name, Is.EqualTo("Blackbeard"));
            Assert.That(character.Job, Is.EqualTo("Swordsman"));
            Assert.That(character.Level, Is.EqualTo(15));
            Assert.That(character.TypeId, Is.EqualTo(7));
        }

        [Test]
        public void A_login_the_server_accepts_reports_what_each_character_wears()
        {
            Subscribe<LoginSucceededEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.LoginReply, Pack(LoginReply.Accepted(
                3,
                [new CharacterSummary("Blackbeard", "Swordsman", 15, 7, BuildEquipmentIds())],
                false)));

            _connection.Drain();

            var look = TakeSingle<LoginSucceededEvent>().Characters[0].Look;

            Assert.That(look.HairId, Is.Zero);
            Assert.That(look.EquipmentIds, Is.EqualTo(BuildEquipmentIds().Select(id => (int)id)));
        }

        [Test]
        public void A_login_the_server_accepts_reports_the_slot_cap_and_the_security_code()
        {
            Subscribe<LoginSucceededEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.LoginReply, Pack(LoginReply.Accepted(9, [], true)));

            _connection.Drain();

            var accepted = TakeSingle<LoginSucceededEvent>();

            Assert.That(accepted.Characters, Is.Empty);
            Assert.That(accepted.SlotCap, Is.EqualTo(9));
            Assert.That(accepted.HasSecurityCode, Is.True);
        }

        [Test]
        public void A_login_the_server_rejects_reports_its_code_and_the_message_for_it()
        {
            Subscribe<LoginFailedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.LoginReply, BuildRefusedReply(WrongPassword, string.Empty));

            _connection.Drain();

            var rejected = TakeSingle<LoginFailedEvent>();
            Assert.That(rejected.Code, Is.EqualTo(WrongPassword));
            Assert.That(rejected.Text, Is.EqualTo("Password incorrect"));
        }

        [Test]
        public void A_rejection_with_an_unknown_code_reports_the_text_the_server_sent()
        {
            Subscribe<LoginFailedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.LoginReply, BuildRefusedReply(UnknownFailure, "Come back tomorrow"));

            _connection.Drain();

            Assert.That(TakeSingle<LoginFailedEvent>().Text, Is.EqualTo("Come back tomorrow"));
        }

        [Test]
        public void A_login_is_sent_as_the_login_packet_with_the_client_version()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new LogInCommand(Account, Password));

            var sent = Gate.Sent.Single();
            var request = LoginRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.Login));
            Assert.That(request.Account, Is.EqualTo(Account));
            Assert.That(request.Digest, Is.EqualTo(PasswordDigest.Hex(Password)));
            Assert.That(request.ClientVersion, Is.EqualTo(_settings.ClientVersion));
        }

        [Test]
        public void A_begin_play_is_sent_as_the_begin_play_packet_with_the_character_index()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new BeginPlayCommand(1));

            var sent = Gate.Sent.Single();
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.BeginPlay));
            Assert.That(BeginPlayRequest.Read(new PacketReader(sent.Payload)).CharacterIndex, Is.EqualTo(1));
        }

        [Test]
        public void A_begin_play_the_server_refuses_reports_its_code_and_the_message_for_it()
        {
            Subscribe<BeginPlayFailedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.BeginPlayReply, Pack(packet => new BeginPlayReply(InvalidCharacter).Write(packet)));

            _connection.Drain();

            var failed = TakeSingle<BeginPlayFailedEvent>();
            Assert.That(failed.Code, Is.EqualTo(InvalidCharacter));
            Assert.That(failed.Text, Is.EqualTo("Current account does not have a request (Select/Delete) to character"));
        }

        [Test]
        public void A_create_security_code_is_sent_as_the_create_second_password_packet_with_the_code()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new CreateSecurityCodeCommand("12345678"));

            var sent = Gate.Sent.Single();
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.CreateSecondPassword));
            Assert.That(CreateSecondPasswordRequest.Read(new PacketReader(sent.Payload)).Code, Is.EqualTo("12345678"));
        }

        [Test]
        public void A_create_security_code_the_server_accepts_reports_the_code_created()
        {
            Subscribe<SecurityCodeCreatedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveCreateSecondPasswordReply(0);

            _connection.Drain();

            Assert.That(_delivered.Single(), Is.InstanceOf<SecurityCodeCreatedEvent>());
        }

        [TestCase(NoSecondPassword, "Cha second password not created")]
        [TestCase(ServerBusy, "The server is busy. Try again later")]
        [TestCase(UnknownFailure, "")]
        public void A_create_security_code_the_server_refuses_reports_its_code_and_the_message_for_it(int code,
            string text)
        {
            Subscribe<CreateSecurityCodeFailedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveCreateSecondPasswordReply((ushort)code);

            _connection.Drain();

            var failed = TakeSingle<CreateSecurityCodeFailedEvent>();
            Assert.That(failed.Code, Is.EqualTo(code));
            Assert.That(failed.Text, Is.EqualTo(text));
        }

        [Test]
        public void A_change_security_code_is_sent_as_the_change_second_password_packet_with_both_codes()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new ChangeSecurityCodeCommand("12345678", "876543210"));

            var sent = Gate.Sent.Single();
            var request = ChangeSecondPasswordRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.ChangeSecondPassword));
            Assert.That(request.OldCode, Is.EqualTo("12345678"));
            Assert.That(request.NewCode, Is.EqualTo("876543210"));
        }

        [Test]
        public void A_change_security_code_the_server_accepts_reports_the_code_changed()
        {
            Subscribe<SecurityCodeChangedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveChangeSecondPasswordReply(0);

            _connection.Drain();

            Assert.That(_delivered.Single(), Is.InstanceOf<SecurityCodeChangedEvent>());
        }

        [TestCase(NoSecondPassword, "Cha second password not created")]
        [TestCase(ServerBusy, "The server is busy. Try again later")]
        [TestCase(UnknownFailure, "")]
        public void A_change_security_code_the_server_refuses_reports_its_code_and_the_message_for_it(int code,
            string text)
        {
            Subscribe<ChangeSecurityCodeFailedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveChangeSecondPasswordReply((ushort)code);

            _connection.Drain();

            var failed = TakeSingle<ChangeSecurityCodeFailedEvent>();
            Assert.That(failed.Code, Is.EqualTo(code));
            Assert.That(failed.Text, Is.EqualTo(text));
        }

        [Test]
        public void A_delete_character_is_sent_as_the_delete_character_packet_with_the_index_and_the_code()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new DeleteCharacterCommand(2, "12345678"));

            var sent = Gate.Sent.Single();
            var request = DeleteCharacterRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.DeleteCharacter));
            Assert.That(request.CharacterIndex, Is.EqualTo(2));
            Assert.That(request.Code, Is.EqualTo("12345678"));
        }

        [Test]
        public void A_delete_character_the_server_accepts_reports_the_character_deleted()
        {
            Subscribe<CharacterDeletedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveDeleteCharacterReply(0);

            _connection.Drain();

            Assert.That(_delivered.Single(), Is.InstanceOf<CharacterDeletedEvent>());
        }

        [TestCase(NoSecondPassword, "Cha second password not created")]
        [TestCase(GuildLeader, "Disband your guild before you delete its leader")]
        [TestCase(CharacterOnline, "This character is in the game on another session")]
        [TestCase(ServerBusy, "The server is busy. Try again later")]
        [TestCase(UnknownFailure, "")]
        public void A_delete_character_the_server_refuses_reports_its_code_and_the_message_for_it(int code,
            string text)
        {
            Subscribe<DeleteCharacterFailedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveDeleteCharacterReply((ushort)code);

            _connection.Drain();

            var failed = TakeSingle<DeleteCharacterFailedEvent>();
            Assert.That(failed.Code, Is.EqualTo(code));
            Assert.That(failed.Text, Is.EqualTo(text));
        }

        [Test]
        public void An_enter_map_reply_reports_the_map_and_the_character_standing_on_it()
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Pack(packet => BuildEnterMapReply().Write(packet)));

            _connection.Drain();

            var entered = TakeSingle<MapEnteredEvent>();
            Assert.That(entered.MapName, Is.EqualTo("garner"));
            Assert.That(entered.Character.WorldId, Is.EqualTo(1));
            Assert.That(entered.Character.ControlType, Is.EqualTo(CharacterControlType.Player));
            Assert.That(entered.Character.TypeId, Is.EqualTo(4), "the character table id, not the look's type");
            Assert.That(entered.Character.Name, Is.EqualTo("Blackbeard"));
            Assert.That(entered.Character.Position, Is.EqualTo(new Vector2(1902.61f, 2848.80f)));
            Assert.That(entered.Character.Angle, Is.EqualTo(359));
            Assert.That(entered.Character.ExistState, Is.EqualTo(1));
            Assert.That(entered.Character.TeamLeaderId, Is.EqualTo(9));
            Assert.That(entered.Character.GuildId, Is.EqualTo(12));
            Assert.That(entered.Character.GuildName, Is.EqualTo("Crew"));
            Assert.That(entered.Character.GuildMotto, Is.EqualTo("Yarr"));
            Assert.That(entered.Character.GuildPermission, Is.EqualTo(2));
            Assert.That(entered.Character.Level, Is.EqualTo(15));
            Assert.That(entered.Character.Health, Is.EqualTo(120));
            Assert.That(entered.Character.MaxHealth, Is.EqualTo(300));
        }

        [TestCase(EnteredAtEdge, EnterType.Edge)]
        [TestCase(EnteredByCarry, EnterType.Carry)]
        public void An_enter_map_reply_reports_how_the_character_entered_the_map(byte enterType, EnterType expected)
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Pack(packet => BuildEnterMapReply(enterType).Write(packet)));

            _connection.Drain();

            Assert.That(TakeSingle<MapEnteredEvent>().EnterType, Is.EqualTo(expected));
        }

        [Test]
        public void An_enter_map_reply_reports_the_move_speed_in_meters_per_second()
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Pack(packet => BuildEnterMapReply().Write(packet)));

            _connection.Drain();

            Assert.That(TakeSingle<MapEnteredEvent>().Character.MoveSpeed, Is.EqualTo(4.8f), "480 cm/s in the packet");
        }

        [Test]
        public void An_enter_map_reply_reports_the_mana_the_job_and_the_experience_of_the_character()
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Unhex(PinnedSwordsmanAtLevelEightyEntersHex));

            _connection.Drain();

            var character = TakeSingle<MapEnteredEvent>().Character;
            Assert.That(character.Level, Is.EqualTo(80));
            Assert.That(character.Health, Is.EqualTo(2115));
            Assert.That(character.MaxHealth, Is.EqualTo(2115));
            Assert.That(character.Mana, Is.EqualTo(260));
            Assert.That(character.MaxMana, Is.EqualTo(260));
            Assert.That(character.Job, Is.EqualTo(1), "Swordsman");
            Assert.That(character.Experience, Is.EqualTo(4_000_000_000));
            Assert.That(character.LevelThreshold, Is.EqualTo(2_425_349_810), "level 80 in the level table");
            Assert.That(character.NextLevelThreshold, Is.EqualTo(2_440_895_086), "level 81 in the level table");
        }

        [Test]
        public void A_kitbag_action_the_server_sent_after_a_move_reports_the_emptied_and_the_filled_slot()
        {
            Subscribe<BagChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.NotiAction, Unhex(PinnedT1SwordMovedHex));

            _connection.Drain();

            var changed = TakeSingle<BagChangedEvent>();
            Assert.That(changed.WorldId, Is.EqualTo(PinnedT1WorldId));
            Assert.That(changed.Sync.Capacity, Is.Null);
            Assert.That(changed.Sync.Slots.Select(SummarizeSlot),
                Is.EqualTo(new (int, int?, int?)[] { (0, null, null), (6, 437, 1) }),
                "item 437 moved from the first slot to the seventh");
        }

        [Test]
        public void A_kitbag_action_the_server_sent_after_a_throw_reports_the_emptied_slot()
        {
            Subscribe<BagChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.NotiAction, Unhex(PinnedObserverVestThrownHex));

            _connection.Drain();

            var changed = TakeSingle<BagChangedEvent>();
            Assert.That(changed.WorldId, Is.EqualTo(PinnedObserverWorldId));
            Assert.That(changed.Sync.Slots.Select(SummarizeSlot),
                Is.EqualTo(new (int, int?, int?)[] { (13, null, null) }), "the vest left the fourteenth slot");
        }

        [Test]
        public void An_enter_map_reply_the_server_sent_reports_the_whole_bag_with_its_capacity_and_the_gold()
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Unhex(PinnedT1EntersHex));

            _connection.Drain();

            var entered = TakeSingle<MapEnteredEvent>();
            Assert.That(entered.Bag.Capacity, Is.EqualTo(24));
            Assert.That(entered.Bag.Slots.Select(slot => slot.Index), Is.EqualTo(Enumerable.Range(0, 24)));
            Assert.That(entered.Bag.Slots.Select(slot => slot.Item?.ItemId).Take(7),
                Is.EqualTo(new int?[] { 437, 8, 289, 641, 9621, 444, null }));
            Assert.That(entered.Bag.Slots.Skip(6).Select(slot => slot.Item), Is.All.Null);
            Assert.That(entered.Character.Gold, Is.EqualTo(2_500));
        }

        [Test]
        public void An_enter_map_reply_the_server_sent_reports_the_item_in_each_equipment_slot()
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Unhex(PinnedT1EntersHex));

            _connection.Drain();

            var equipment = TakeSingle<MapEnteredEvent>().Equipment;
            Assert.That(equipment, Has.Count.EqualTo(34));
            Assert.That(equipment[(int)EquipmentSlot.Face]!.ItemId, Is.EqualTo(2554));
            Assert.That(equipment[(int)EquipmentSlot.Body]!.ItemId, Is.EqualTo(4308));
            Assert.That(equipment[(int)EquipmentSlot.Shoes]!.ItemId, Is.EqualTo(4310));
            Assert.That(equipment[(int)EquipmentSlot.RightHand]!.ItemId, Is.EqualTo(9));
            Assert.That(equipment[(int)EquipmentSlot.RightHand]!.MaxDurability, Is.EqualTo(10_000));
            Assert.That(equipment[(int)EquipmentSlot.LeftHand], Is.Null,
                "the server marks the left hand with item 9999, which is not an item");
            Assert.That(equipment.Count(item => item != null), Is.EqualTo(4));
        }

        [Test]
        public void A_temporary_kitbag_sync_reply_the_server_sent_reports_an_empty_temporary_bag_of_32_slots()
        {
            Subscribe<TemporaryBagChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.TemporaryKitbagSyncReply, Unhex(PinnedEmptyTemporaryBagHex));

            _connection.Drain();

            var sync = TakeSingle<TemporaryBagChangedEvent>().Sync;
            Assert.That(sync.Capacity, Is.EqualTo(32));
            Assert.That(sync.Slots, Is.Empty);
        }

        [Test]
        public void A_kitbag_action_reports_each_changed_slot_of_the_bag_and_no_capacity()
        {
            Subscribe<BagChangedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveKitbag(new KitbagSync(KitbagSwitch, 0,
                new[] { new KitbagSlot(0, BuildSwordGrid()), new KitbagSlot(5, ItemGrid.Empty) }));

            _connection.Drain();

            var changed = TakeSingle<BagChangedEvent>();
            Assert.That(changed.WorldId, Is.EqualTo(CrabWorldId));
            Assert.That(changed.Sync.Capacity, Is.Null);
            Assert.That(changed.Sync.Slots.Select(slot => slot.Index), Is.EqualTo(new[] { 0, 5 }));
            Assert.That(changed.Sync.Slots[1].Item, Is.Null, "an emptied slot");
        }

        [Test]
        public void A_kitbag_action_reports_what_an_occupied_slot_holds()
        {
            Subscribe<BagChangedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveKitbag(new KitbagSync(KitbagSwitch, 0, new[] { new KitbagSlot(3, BuildSwordGrid()) }));

            _connection.Drain();

            var item = TakeSingle<BagChangedEvent>().Sync.Slots.Single().Item!;
            Assert.That(item.ItemId, Is.EqualTo(Sword));
            Assert.That(item.IsLocked, Is.True, "a lock value that is not 0 locks the item");
            Assert.That(item.NeedLevel, Is.EqualTo(10));
            Assert.That(item.Count, Is.EqualTo(1));
            Assert.That((item.Durability, item.MaxDurability), Is.EqualTo((9_000, 10_000)));
            Assert.That((item.Energy, item.MaxEnergy), Is.EqualTo((120, 3_450)));
            Assert.That(item.ForgeLevel, Is.EqualTo(2));
            Assert.That(item.IsValid, Is.True);
            Assert.That(item.IsTradable, Is.False);
            Assert.That(item.Expiration, Is.EqualTo(1_800_000_000));
            Assert.That(item.ForgeParameter, Is.EqualTo(123_456));
            Assert.That(item.InstanceParameter, Is.EqualTo(7));
        }

        [Test]
        public void An_item_with_a_lock_value_of_0_reports_it_is_not_locked()
        {
            Subscribe<BagChangedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveKitbag(new KitbagSync(KitbagSwitch, 0, new[] { new KitbagSlot(3, BuildSwordGrid(lockValue: 0)) }));

            _connection.Drain();

            Assert.That(TakeSingle<BagChangedEvent>().Sync.Slots.Single().Item!.IsLocked, Is.False);
        }

        [Test]
        public void An_item_reports_its_instance_attributes_as_the_stats_they_raise()
        {
            Subscribe<BagChangedEvent>();
            _connection.Connect(Host, Port, true);
            var attributes = new[]
            {
                new ItemInstanceAttribute(26, 3),
                new ItemInstanceAttribute(28, 4),
                new ItemInstanceAttribute(0, 0),
                new ItemInstanceAttribute(47, 5),
                new ItemInstanceAttribute(181, 9)
            };
            ReceiveKitbag(new KitbagSync(KitbagSwitch, 0,
                new[] { new KitbagSlot(0, BuildSwordGrid(attributes)) }));

            _connection.Drain();

            Assert.That(TakeSingle<BagChangedEvent>().Sync.Slots.Single().Item!.InstanceStats,
                Is.EqualTo(new Dictionary<CharacterStat, int>
                {
                    [CharacterStat.Strength] = 3,
                    [CharacterStat.Accuracy] = 4,
                    [CharacterStat.PhysicalResist] = 5
                }),
                "item attributes 26, 28 and 47 raise strength, accuracy and physical resist; 181 is no stat");
        }

        [Test]
        public void An_item_without_instance_attributes_reports_no_stats()
        {
            Subscribe<BagChangedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveKitbag(new KitbagSync(KitbagSwitch, 0, new[] { new KitbagSlot(0, BuildSwordGrid()) }));

            _connection.Drain();

            Assert.That(TakeSingle<BagChangedEvent>().Sync.Slots.Single().Item!.InstanceStats, Is.Empty);
        }

        [Test]
        public void A_kitbag_init_action_reports_the_capacity_of_the_bag()
        {
            Subscribe<BagChangedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveKitbag(new KitbagSync(KitbagInit, 24, new[] { new KitbagSlot(0, BuildSwordGrid()) }));

            _connection.Drain();

            Assert.That(TakeSingle<BagChangedEvent>().Sync.Capacity, Is.EqualTo(24));
        }

        [Test]
        public void A_temporary_kitbag_action_reports_each_changed_slot_of_the_temporary_bag()
        {
            Subscribe<TemporaryBagChangedEvent>();
            _connection.Connect(Host, Port, true);
            var kitbag = new KitbagSync(KitbagSwitch, 0,
                new[] { new KitbagSlot(2, ItemGrid.Empty), new KitbagSlot(4, BuildSwordGrid()) });
            Gate.Listener.OnReceived(Opcode.NotiAction,
                Pack(packet => new TemporaryKitbagNotice(CrabWorldId, 42, kitbag).Write(packet)));

            _connection.Drain();

            var sync = TakeSingle<TemporaryBagChangedEvent>().Sync;
            Assert.That(sync.Capacity, Is.Null);
            Assert.That(sync.Slots.Select(slot => slot.Index), Is.EqualTo(new[] { 2, 4 }));
            Assert.That(sync.Slots.Select(slot => slot.Item?.ItemId), Is.EqualTo(new int?[] { null, Sword }));
        }

        [Test]
        public void A_temporary_bag_sync_is_sent_as_the_temporary_kitbag_sync_packet_with_no_body()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new SyncTemporaryBagCommand());

            var sent = Gate.Sent.Single();
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.TemporaryKitbagSync));
            Assert.That(sent.Payload, Is.Empty);
        }

        [Test]
        public void A_temporary_kitbag_sync_reply_reports_the_capacity_and_the_slots_of_the_temporary_bag()
        {
            Subscribe<TemporaryBagChangedEvent>();
            _connection.Connect(Host, Port, true);
            var kitbag = new KitbagSync(KitbagInit, 16, new[] { new KitbagSlot(0, BuildSwordGrid()) });
            Gate.Listener.OnReceived(Opcode.TemporaryKitbagSyncReply,
                Pack(packet => new TemporaryKitbagSyncReply(kitbag).Write(packet)));

            _connection.Drain();

            var sync = TakeSingle<TemporaryBagChangedEvent>().Sync;
            Assert.That(sync.Capacity, Is.EqualTo(16));
            Assert.That(sync.Slots.Single().Item!.ItemId, Is.EqualTo(Sword));
        }

        [Test]
        public void A_bag_lock_is_sent_as_the_kitbag_lock_packet_with_no_body()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new LockBagCommand());

            var sent = Gate.Sent.Single();
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.KitbagLock));
            Assert.That(sent.Payload, Is.Empty);
        }

        [Test]
        public void A_bag_unlock_is_sent_as_the_kitbag_unlock_packet_with_the_security_code()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new UnlockBagCommand("12345678"));

            var sent = Gate.Sent.Single();
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.KitbagUnlock));
            Assert.That(KitbagUnlockRequest.Read(new PacketReader(sent.Payload)).Code, Is.EqualTo("12345678"));
        }

        [Test]
        public void A_bag_item_move_is_sent_as_a_begin_action_item_move_of_the_whole_stack()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new MoveBagItemCommand(PinnedT1WorldId, 0, 20, 6));

            var sent = Gate.Sent.Single();
            var request = BeginActionRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.BeginAction));
            Assert.That(request.WorldId, Is.EqualTo(PinnedT1WorldId));
            Assert.That(request.ActionType, Is.EqualTo(ItemMoveRequest.Action));
            var move = (ItemMoveRequest)request;
            Assert.That((move.SourceGridId, move.Count, move.TargetGridId), Is.EqualTo(((short)0, (short)20, (short)6)));
        }

        [Test]
        public void A_bag_item_split_is_sent_as_an_item_move_of_part_of_the_stack()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new MoveBagItemCommand(PinnedT1WorldId, 3, 7, 4));

            var move = (ItemMoveRequest)BeginActionRequest.Read(new PacketReader(Gate.Sent.Single().Payload));
            Assert.That((move.SourceGridId, move.Count, move.TargetGridId), Is.EqualTo(((short)3, (short)7, (short)4)));
        }

        [Test]
        public void A_temporary_bag_item_take_is_sent_as_a_begin_action_temporary_item_drag_to_the_bag_slot()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new TakeTemporaryBagItemCommand(PinnedT1WorldId, 2, 5, 7));

            var sent = Gate.Sent.Single();
            var request = BeginActionRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.BeginAction));
            Assert.That(request.WorldId, Is.EqualTo(PinnedT1WorldId));
            Assert.That(request.ActionType, Is.EqualTo(TemporaryKitbagDragRequest.Action));
            var drag = (TemporaryKitbagDragRequest)request;
            Assert.That((drag.SourceGridId, drag.Count, drag.TargetGridId), Is.EqualTo(((short)2, (short)5, (short)7)));
        }

        [Test]
        public void A_temporary_bag_item_take_with_no_bag_slot_lets_the_server_pick_the_first_free_slot()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new TakeTemporaryBagItemCommand(PinnedT1WorldId, 2, 5, null));

            var drag = (TemporaryKitbagDragRequest)BeginActionRequest.Read(new PacketReader(Gate.Sent.Single().Payload));
            Assert.That(drag.TargetGridId, Is.EqualTo(TemporaryKitbagDragRequest.FirstFreeGridId));
        }

        [Test]
        public void A_temporary_bag_item_split_is_sent_as_a_temporary_item_drag_of_part_of_the_stack()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new TakeTemporaryBagItemCommand(PinnedT1WorldId, 3, 2, 8));

            var drag = (TemporaryKitbagDragRequest)BeginActionRequest.Read(new PacketReader(Gate.Sent.Single().Payload));
            Assert.That((drag.SourceGridId, drag.Count, drag.TargetGridId), Is.EqualTo(((short)3, (short)2, (short)8)));
        }

        [Test]
        public void A_temporary_bag_item_take_the_server_accepted_reports_the_filled_bag_slot_and_the_emptied_temporary_slot()
        {
            Subscribe<BagChangedEvent>();
            Subscribe<TemporaryBagChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.NotiAction, Unhex(PinnedT1ThreeApplesTakenHex));
            Gate.Listener.OnReceived(Opcode.NotiAction, Unhex(PinnedT1TemporaryBagAfterAppleTakeHex));

            _connection.Drain();

            Assert.That(_delivered, Has.Count.EqualTo(2));
            Assert.That(((BagChangedEvent)_delivered[0]).Sync.Slots.Select(SummarizeSlot),
                Is.EqualTo(new (int, int?, int?)[] { (6, 1847, 3) }), "three apples in the seventh slot");
            Assert.That(((TemporaryBagChangedEvent)_delivered[1]).Sync.Slots.Select(SummarizeSlot),
                Is.EqualTo(new (int, int?, int?)[] { (0, null, null), (1, 3116, 2) }));
        }

        [Test]
        public void A_temporary_bag_item_take_into_a_full_bag_reports_the_bag_full_and_the_item_back_in_its_slot()
        {
            Subscribe<TemporaryBagChangedEvent>();
            Subscribe<ItemActionFailedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.NotiAction, Unhex(PinnedT1TemporaryBagAfterTakeIntoFullBagHex));
            Gate.Listener.OnReceived(Opcode.NotiAction, Unhex(PinnedT1TakeIntoFullBagHex));

            _connection.Drain();

            Assert.That(_delivered, Has.Count.EqualTo(2));
            Assert.That(((TemporaryBagChangedEvent)_delivered[0]).Sync.Slots.Select(SummarizeSlot),
                Is.EqualTo(new (int, int?, int?)[] { (0, null, null), (1, 3116, 2) }));
            Assert.That(((ItemActionFailedEvent)_delivered[1]).Failure, Is.EqualTo(ItemActionFailure.BagFull));
        }

        [Test]
        public void An_equip_is_sent_as_a_begin_action_item_use_that_lets_the_server_pick_the_link()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new EquipBagItemCommand(PinnedT1WorldId, 2, false));

            var sent = Gate.Sent.Single();
            var request = BeginActionRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.BeginAction));
            Assert.That(request.WorldId, Is.EqualTo(PinnedT1WorldId));
            Assert.That(request.ActionType, Is.EqualTo(ItemUseRequest.Action));
            var use = (ItemUseRequest)request;
            Assert.That((use.GridId, use.TargetGridId), Is.EqualTo(((short)2, ItemUseRequest.FirstFreeLink)));
        }

        [Test]
        public void An_equip_into_the_second_slot_is_sent_with_the_second_link_target()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new EquipBagItemCommand(PinnedT1WorldId, 2, true));

            var use = (ItemUseRequest)BeginActionRequest.Read(new PacketReader(Gate.Sent.Single().Payload));
            Assert.That((use.GridId, use.TargetGridId), Is.EqualTo(((short)2, ItemUseRequest.SecondLink)));
        }

        [Test]
        public void A_bag_item_use_is_sent_as_a_begin_action_item_use_with_no_target_item()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new UseBagItemCommand(PinnedT1WorldId, 5));

            var sent = Gate.Sent.Single();
            var request = BeginActionRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.BeginAction));
            Assert.That(request.WorldId, Is.EqualTo(PinnedT1WorldId));
            Assert.That(request.ActionType, Is.EqualTo(ItemUseRequest.Action));
            var use = (ItemUseRequest)request;
            Assert.That((use.GridId, use.TargetGridId), Is.EqualTo(((short)5, ItemUseRequest.NoTargetItem)));
        }

        [Test]
        public void An_unequip_is_sent_as_a_begin_action_item_unfix_of_the_link_to_the_bag_slot()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new UnequipItemCommand(PinnedT1WorldId, EquipmentSlot.Ring2, 11));

            var sent = Gate.Sent.Single();
            var request = BeginActionRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.BeginAction));
            Assert.That(request.WorldId, Is.EqualTo(PinnedT1WorldId));
            Assert.That(request.ActionType, Is.EqualTo(ItemUnfixRequest.Action));
            var unfix = (ItemUnfixRequest)request;
            Assert.That((unfix.LinkId, unfix.GridId, unfix.DropPosition),
                Is.EqualTo(((byte)8, (short)11, (Point?)null)));
        }

        [Test]
        public void A_bag_item_throw_is_sent_as_a_begin_action_item_throw_at_the_drop_position_in_whole_centimetres()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new ThrowBagItemCommand(PinnedT1WorldId, 4, 3, new Vector2(2246.75f, 2826.746f)));

            var sent = Gate.Sent.Single();
            var request = BeginActionRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.BeginAction));
            Assert.That(request.WorldId, Is.EqualTo(PinnedT1WorldId));
            Assert.That(request.ActionType, Is.EqualTo(ItemThrowRequest.Action));
            var throwing = (ItemThrowRequest)request;
            Assert.That((throwing.GridId, throwing.Count, throwing.DropPosition),
                Is.EqualTo(((short)4, (short)3, new Point(224675, 282675))));
        }

        [Test]
        public void A_bag_item_delete_is_sent_as_a_begin_action_item_delete_of_the_bag_slot()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new DeleteBagItemCommand(PinnedT1WorldId, 5));

            var sent = Gate.Sent.Single();
            var request = BeginActionRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.BeginAction));
            Assert.That(request.WorldId, Is.EqualTo(PinnedT1WorldId));
            Assert.That(request.ActionType, Is.EqualTo(ItemDeleteRequest.Action));
            Assert.That(((ItemDeleteRequest)request).GridId, Is.EqualTo(5));
        }

        [Test]
        public void Bag_item_actions_count_one_packet_id()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new MoveBagItemCommand(PinnedT1WorldId, 0, 1, 6));
            _connection.Send(new EquipBagItemCommand(PinnedT1WorldId, 2, false));
            _connection.Send(new UnequipItemCommand(PinnedT1WorldId, EquipmentSlot.Body, 3));
            _connection.Send(new TakeTemporaryBagItemCommand(PinnedT1WorldId, 0, 1, null));
            _connection.Send(new ThrowBagItemCommand(PinnedT1WorldId, 1, 1, new Vector2(2246.75f, 2826.75f)));
            _connection.Send(new DeleteBagItemCommand(PinnedT1WorldId, 2));
            _connection.Send(new UseBagItemCommand(PinnedT1WorldId, 1));

            Assert.That(Gate.Sent.Select(ReadPacketId), Is.EqualTo(new uint[] { 1, 2, 3, 4, 5, 6, 7 }));
        }

        [Test]
        public void Bag_item_moves_and_character_moves_count_one_packet_id()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(BuildMoveCommand());
            _connection.Send(new MoveBagItemCommand(PinnedT1WorldId, 0, 1, 6));

            Assert.That(Gate.Sent.Select(ReadPacketId), Is.EqualTo(new uint[] { 1, 2 }));
        }

        [Test]
        public void An_item_failed_action_the_server_sent_for_a_move_in_a_locked_bag_reports_the_bag_locked()
        {
            Subscribe<ItemActionFailedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.NotiAction, Unhex(PinnedObserverMoveInLockedBagHex));

            _connection.Drain();

            Assert.That(TakeSingle<ItemActionFailedEvent>().Failure, Is.EqualTo(ItemActionFailure.BagLocked));
        }

        [Test]
        public void An_item_failed_action_the_server_sent_for_a_throw_out_of_range_reports_too_far()
        {
            Subscribe<ItemActionFailedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.NotiAction, Unhex(PinnedObserverThrowTooFarHex));

            _connection.Drain();

            Assert.That(TakeSingle<ItemActionFailedEvent>().Failure, Is.EqualTo(ItemActionFailure.TooFar));
        }

        [TestCase(1, ItemActionFailure.ItemMissing)]
        [TestCase(2, ItemActionFailure.BagFull)]
        [TestCase(7, ItemActionFailure.BagLocked)]
        [TestCase(17, ItemActionFailure.OutOfBagRange)]
        [TestCase(18, ItemActionFailure.Expired)]
        [TestCase(-4, ItemActionFailure.BagFull, TestName = "The kitbag full code a made item fails with means the bag is full")]
        public void An_item_failed_action_reports_why_the_item_action_failed(short result, ItemActionFailure expected)
        {
            Subscribe<ItemActionFailedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.NotiAction,
                Pack(packet => new ItemFailedNotice(PinnedT1WorldId, 10, result).Write(packet)));

            _connection.Drain();

            Assert.That(TakeSingle<ItemActionFailedEvent>().Failure, Is.EqualTo(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void A_kitbag_lock_state_reports_whether_the_bag_is_locked(bool isLocked)
        {
            Subscribe<BagLockReportedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.KitbagLockState,
                Pack(packet => new KitbagLockStateReply(isLocked).Write(packet)));

            _connection.Drain();

            Assert.That(TakeSingle<BagLockReportedEvent>().IsLocked, Is.EqualTo(isLocked));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void An_enter_map_reply_reports_whether_the_bag_is_locked(bool isLocked)
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap,
                Pack(packet => BuildEnterMapReply(isKitbagLocked: isLocked).Write(packet)));

            _connection.Drain();

            Assert.That(TakeSingle<MapEnteredEvent>().IsBagLocked, Is.EqualTo(isLocked));
        }

        [Test]
        public void An_enter_map_reply_reports_the_bag()
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Pack(packet => BuildEnterMapReply(
                kitbag: new[] { new KitbagSlot(0, BuildSwordGrid()), new KitbagSlot(1, ItemGrid.Empty) })
                .Write(packet)));

            _connection.Drain();

            var entered = TakeSingle<MapEnteredEvent>();
            Assert.That(entered.Bag.Capacity, Is.EqualTo(24));
            Assert.That(entered.Bag.Slots.Select(slot => slot.Item?.ItemId), Is.EqualTo(new int?[] { Sword, null }));
        }

        [Test]
        public void An_attribute_sync_reports_the_gold_of_the_character()
        {
            Subscribe<CharacterAttributesChangedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveAttributeSync(TaskSync, new CharacterAttribute(GoldAttribute, 2_000_000_000));

            _connection.Drain();

            Assert.That(TakeSingle<CharacterAttributesChangedEvent>().Attributes.Gold, Is.EqualTo(2_000_000_000L));
        }

        [Test]
        public void An_enter_map_the_server_refuses_reports_a_failed_begin_play()
        {
            Subscribe<BeginPlayFailedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Pack(packet => EnterMapReply.Refused(EnterFailed).Write(packet)));

            _connection.Drain();

            var failed = TakeSingle<BeginPlayFailedEvent>();
            Assert.That(failed.Code, Is.EqualTo(EnterFailed));
            Assert.That(failed.Text, Is.EqualTo("failed to enter map!"));
        }

        [Test]
        public void An_end_play_is_sent_as_the_end_play_packet_with_no_body()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new EndPlayCommand());

            var sent = Gate.Sent.Single();
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.EndPlay));
            Assert.That(sent.Payload, Is.Empty);
        }

        [Test]
        public void A_move_is_sent_as_a_begin_action_move_in_whole_centimetres()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new MoveCommand(1, [new Vector2(2246.75f, 2826.75f), new Vector2(2244.754f, 2826.746f)]));

            var sent = Gate.Sent.Single();
            var request = BeginActionRequest.Read(new PacketReader(sent.Payload));
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.BeginAction));
            Assert.That(request.WorldId, Is.EqualTo(1));
            Assert.That(request.ActionType, Is.EqualTo(MoveRequest.Action));
            Assert.That(
                ((MoveRequest)request).Path,
                Is.EqualTo(new[] { new Point(224675, 282675), new Point(224475, 282675) }));
        }

        [Test]
        public void Begin_actions_count_their_packet_ids_up_from_one_on_each_connection()
        {
            _connection.Connect(Host, Port, true);
            _connection.Send(BuildMoveCommand());
            _connection.Send(BuildMoveCommand());
            var earlier = Gate;

            _connection.Connect(Host, Port, true);
            _connection.Send(BuildMoveCommand());

            Assert.That(earlier.Sent.Select(ReadPacketId), Is.EqualTo(new uint[] { 1, 2 }));
            Assert.That(Gate.Sent.Select(ReadPacketId), Is.EqualTo(new uint[] { 1 }));
        }

        [Test]
        public void A_stop_move_is_sent_as_the_end_action_packet_with_no_body()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new StopMoveCommand());

            var sent = Gate.Sent.Single();
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.EndAction));
            Assert.That(sent.Payload, Is.Empty);
        }

        [Test]
        public void An_end_play_the_server_accepts_reports_the_characters_to_select_from()
        {
            Subscribe<PlayEndedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EndPlayReply, Pack(packet => EndPlayReply.Accepted(
                4,
                [
                    new CharacterSummary("Blackbeard", "Swordsman", 15, 7, new ushort[CharacterSummary.EquipmentSlots]),
                    new CharacterSummary("Anne", "Explorer", 3, 4, new ushort[CharacterSummary.EquipmentSlots])
                ]).Write(packet)));

            _connection.Drain();

            var ended = TakeSingle<PlayEndedEvent>();
            Assert.That(ended.Characters.Select(character => character.Name), Is.EqualTo(new[] { "Blackbeard", "Anne" }));
            Assert.That(ended.Characters[0].Job, Is.EqualTo("Swordsman"));
            Assert.That(ended.Characters[0].Level, Is.EqualTo(15));
            Assert.That(ended.Characters[0].TypeId, Is.EqualTo(7));
            Assert.That(ended.SlotCap, Is.EqualTo(4));
        }

        [Test]
        public void An_end_play_the_server_accepts_reports_what_each_character_wears()
        {
            Subscribe<PlayEndedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EndPlayReply, Pack(packet => EndPlayReply.Accepted(
                4,
                [new CharacterSummary("Blackbeard", "Swordsman", 15, 7, BuildEquipmentIds())]).Write(packet)));

            _connection.Drain();

            var look = TakeSingle<PlayEndedEvent>().Characters[0].Look;

            Assert.That(look.HairId, Is.Zero);
            Assert.That(look.EquipmentIds, Is.EqualTo(BuildEquipmentIds().Select(id => (int)id)));
        }

        [Test]
        public void An_end_play_the_server_refuses_reports_its_code_and_the_message_for_it()
        {
            Subscribe<EndPlayFailedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EndPlayReply, Pack(packet => EndPlayReply.Refused(NotPlaying).Write(packet)));

            _connection.Drain();

            var failed = TakeSingle<EndPlayFailedEvent>();
            Assert.That(failed.Code, Is.EqualTo(NotPlaying));
            Assert.That(failed.Text, Is.EqualTo("Currently not in gameplay, unable to send ENDPLAY command"));
        }

        [Test]
        public void An_appear_packet_reports_the_character_that_came_into_view()
        {
            Subscribe<CharacterAppearedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.CharacterBeginSee, Pack(packet => BuildCrabBeginSee().Write(packet)));

            _connection.Drain();

            var appeared = TakeSingle<CharacterAppearedEvent>();
            Assert.That(appeared.SeeType, Is.EqualTo(SeeType.New));
            Assert.That(appeared.Character.WorldId, Is.EqualTo(CrabWorldId));
            Assert.That(appeared.Character.ControlType, Is.EqualTo(CharacterControlType.Monster));
            Assert.That(appeared.Character.TypeId, Is.EqualTo(107));
            Assert.That(appeared.Character.Name, Is.EqualTo("Crab"));
            Assert.That(appeared.Character.Position, Is.EqualTo(new Vector2(2192.20f, 2767.00f)));
            Assert.That(appeared.Character.Angle, Is.EqualTo(90));
            Assert.That(appeared.Character.Level, Is.EqualTo(3));
            Assert.That(appeared.Character.Health, Is.EqualTo(40));
            Assert.That(appeared.Character.MaxHealth, Is.EqualTo(65));
            Assert.That(appeared.Character.Look!.EquipmentIds, Has.Count.EqualTo(LookInfo.LinkCount).And.All.Zero);
        }

        [Test]
        public void An_appear_packet_reports_the_move_speed_in_meters_per_second()
        {
            Subscribe<CharacterAppearedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.CharacterBeginSee, Pack(packet => BuildCrabBeginSee().Write(packet)));

            _connection.Drain();

            Assert.That(TakeSingle<CharacterAppearedEvent>().Character.MoveSpeed, Is.EqualTo(3.5f), "350 cm/s in the packet");
        }

        [Test]
        public void An_appear_packet_reports_the_mana_the_job_and_the_experience_of_the_character()
        {
            Subscribe<CharacterAppearedEvent>();
            _connection.Connect(Host, Port, true);
            var attributes = new[]
            {
                new CharacterAttribute(CharacterAttribute.Mana, 30),
                new CharacterAttribute(CharacterAttribute.MaxMana, 45),
                new CharacterAttribute(CharacterAttribute.Job, 2),
                new CharacterAttribute(CharacterAttribute.Experience, 3_000_000_000),
                new CharacterAttribute(CharacterAttribute.LevelThreshold, 2_900_000_000),
                new CharacterAttribute(CharacterAttribute.NextLevelThreshold, 3_100_000_000)
            };
            Gate.Listener.OnReceived(Opcode.CharacterBeginSee,
                Pack(packet => BuildCrabBeginSee(attributes: attributes).Write(packet)));

            _connection.Drain();

            var character = TakeSingle<CharacterAppearedEvent>().Character;
            Assert.That(character.Mana, Is.EqualTo(30));
            Assert.That(character.MaxMana, Is.EqualTo(45));
            Assert.That(character.Job, Is.EqualTo(2));
            Assert.That(character.Experience, Is.EqualTo(3_000_000_000));
            Assert.That(character.LevelThreshold, Is.EqualTo(2_900_000_000));
            Assert.That(character.NextLevelThreshold, Is.EqualTo(3_100_000_000));
        }

        [Test]
        public void A_look_action_reports_the_new_look_of_the_character()
        {
            Subscribe<CharacterLookChangedEvent>();
            _connection.Connect(Host, Port, true);
            var links = Enumerable.Repeat(ItemGrid.Empty, LookInfo.LinkCount).ToArray();
            links[RightHandLink] = new ItemGrid(ShortSword, 0xBEEF, 0, 0, new short[2], new short[2], 0, true, true, 0,
                null, null, null, null);
            var look = LookInfo.OfCharacter(LookInfo.FullSync, 1, ShortHair, links);
            Gate.Listener.OnReceived(Opcode.NotiAction,
                Pack(packet => new LookNotice(CrabWorldId, 42, look).Write(packet)));

            _connection.Drain();

            var changed = TakeSingle<CharacterLookChangedEvent>();
            Assert.That(changed.WorldId, Is.EqualTo(CrabWorldId));
            Assert.That(changed.Look.HairId, Is.EqualTo(ShortHair));
            Assert.That(changed.Look.EquipmentIds[RightHandLink], Is.EqualTo(ShortSword));
            Assert.That(changed.Look.EquipmentIds.Count(id => id != 0), Is.EqualTo(1));
        }

        [Test]
        public void A_look_action_reports_the_item_in_each_equipment_slot_of_the_character()
        {
            Subscribe<CharacterLookChangedEvent>();
            _connection.Connect(Host, Port, true);
            var links = Enumerable.Repeat(ItemGrid.Empty, LookInfo.LinkCount).ToArray();
            links[RightHandLink] = BuildSwordGrid();
            ReceiveLook(LookInfo.OfCharacter(LookInfo.FullSync, 1, ShortHair, links));

            _connection.Drain();

            var changed = TakeSingle<CharacterLookChangedEvent>();
            Assert.That(changed.Equipment, Has.Count.EqualTo(34));
            Assert.That(changed.Equipment[(int)EquipmentSlot.RightHand]!.ItemId, Is.EqualTo(Sword));
            Assert.That(changed.Equipment[(int)EquipmentSlot.RightHand]!.ForgeParameter, Is.EqualTo(123_456));
            Assert.That(changed.Equipment.Count(item => item != null), Is.EqualTo(1));
        }

        [Test]
        public void A_look_action_reports_a_totem_marker_as_an_empty_equipment_slot()
        {
            Subscribe<CharacterLookChangedEvent>();
            _connection.Connect(Host, Port, true);
            var links = Enumerable.Repeat(ItemGrid.Empty, LookInfo.LinkCount).ToArray();
            links[(int)EquipmentSlot.Gloves] = new ItemGrid(TotemMarker, 0, 0, 0, new short[2], new short[2],
                0, false, false, 0, null, 0, 0, null);
            ReceiveLook(LookInfo.OfCharacter(LookInfo.FullSync, 1, ShortHair, links));

            _connection.Drain();

            Assert.That(TakeSingle<CharacterLookChangedEvent>().Equipment, Is.All.Null);
        }

        [Test]
        public void An_enter_map_reply_reports_the_fairy_the_character_wears()
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Unhex(PinnedFairyEntersHex));

            _connection.Drain();

            Assert.That(TakeSingle<MapEnteredEvent>().Character.Look!.Fairy,
                Is.EqualTo(new Fairy(ItemId: FairyOfLuck, AppearanceItemId: FairyOfLifeApparel, Level: 15, Stamina: 53,
                    MaxStamina: 100, Growth: 150, MaxGrowth: 240)),
                "Fairy of Luck in the Fairy of Life Apparel, endure 2650 / 5000, instance attributes 3, 2, 4, 1 and 5");
        }

        [Test]
        public void The_fairy_stamina_is_the_endure_divided_by_fifty_and_its_level_is_the_sum_of_its_five_stats()
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            var links = BuildLinks();
            links[FairyLink] = BuildFairyGrid(FairyOfLuck, 12_649, 15_000, 1_160, 2_000);
            links[FairyAppearanceLink] = BuildWornGrid(FairyOfLifeApparel);
            Gate.Listener.OnReceived(Opcode.EnterMap, Pack(packet =>
                BuildEnterMapReply(look: LookInfo.OfCharacter(LookInfo.FullSync, 1, ShortHair, links)).Write(packet)));

            _connection.Drain();

            Assert.That(TakeSingle<MapEnteredEvent>().Character.Look!.Fairy,
                Is.EqualTo(new Fairy(ItemId: FairyOfLuck, AppearanceItemId: FairyOfLifeApparel, Level: 13, Stamina: 252,
                    MaxStamina: 300, Growth: 1_160, MaxGrowth: 2_000)),
                "endure 12649 / 15000 in the packet, and the attribute with id 1 is no part of the level");
        }

        [Test]
        public void An_enter_map_reply_of_a_character_with_no_fairy_reports_none()
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Unhex(PinnedSwordsmanAtLevelEightyEntersHex));

            _connection.Drain();

            Assert.That(TakeSingle<MapEnteredEvent>().Character.Look!.Fairy, Is.Null);
        }

        [Test]
        public void An_enter_map_reply_of_a_character_whose_fairy_is_not_valid_reports_none()
        {
            Subscribe<MapEnteredEvent>();
            _connection.Connect(Host, Port, true);
            var links = BuildLinks();
            links[FairyLink] = BuildFairyGrid(FairyOfLuck, 12_649, 15_000, 1_160, 2_000, isValid: false);
            Gate.Listener.OnReceived(Opcode.EnterMap, Pack(packet =>
                BuildEnterMapReply(look: LookInfo.OfCharacter(LookInfo.FullSync, 1, ShortHair, links)).Write(packet)));

            _connection.Drain();

            Assert.That(TakeSingle<MapEnteredEvent>().Character.Look!.Fairy, Is.Null);
        }

        [Test]
        public void An_appear_packet_reports_the_fairy_of_the_character_that_came_into_view()
        {
            Subscribe<CharacterAppearedEvent>();
            _connection.Connect(Host, Port, true);
            var links = BuildLinks();
            links[FairyLink] = BuildFairyGrid(FairyOfEvil, 5_000, 15_000, 0, 1_500);
            var beginSee = BuildCrabBeginSee(LookInfo.OfCharacter(LookInfo.FullSync, 1, ShortHair, links));
            Gate.Listener.OnReceived(Opcode.CharacterBeginSee, Pack(packet => beginSee.Write(packet)));

            _connection.Drain();

            Assert.That(TakeSingle<CharacterAppearedEvent>().Character.Look!.Fairy,
                Is.EqualTo(new Fairy(ItemId: FairyOfEvil, AppearanceItemId: null, Level: 13, Stamina: 100,
                    MaxStamina: 300, Growth: 0, MaxGrowth: 1_500)),
                "no appearance item worn");
        }

        [Test]
        public void An_item_data_sync_reports_the_fairy_condition_and_no_look_change()
        {
            Subscribe<CharacterLookChangedEvent>();
            Subscribe<FairyConditionChangedEvent>();
            _connection.Connect(Host, Port, true);
            var links = BuildLinks();
            links[FairyLink] = BuildFairyGrid(FairyOfLuck, 12_599, 0, 1_200, 0);
            links[FairyAppearanceLink] = BuildWornGrid(FairyOfLifeApparel);
            ReceiveLook(LookInfo.OfCharacter(LookInfo.ItemDataSync, 1, ShortHair, links));

            _connection.Drain();

            var changed = TakeSingle<FairyConditionChangedEvent>();
            Assert.That(changed.WorldId, Is.EqualTo(CrabWorldId));
            Assert.That(changed.ItemId, Is.EqualTo(FairyOfLuck));
            Assert.That(changed.Stamina, Is.EqualTo(251), "the current endure 12599, divided by 50");
            Assert.That(changed.Growth, Is.EqualTo(1_200));
        }

        [Test]
        public void An_item_data_sync_of_a_character_with_no_fairy_reports_nothing()
        {
            Subscribe<CharacterLookChangedEvent>();
            Subscribe<FairyConditionChangedEvent>();
            _connection.Connect(Host, Port, true);
            var links = BuildLinks();
            links[RightHandLink] = BuildWornGrid(ShortSword);
            ReceiveLook(LookInfo.OfCharacter(LookInfo.ItemDataSync, 1, ShortHair, links));

            _connection.Drain();

            Assert.That(_delivered, Is.Empty);
        }

        [Test]
        public void A_look_switch_to_another_fairy_reports_the_new_fairy_in_full()
        {
            Subscribe<CharacterLookChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.NotiAction, Unhex(PinnedFairySwapHex));

            _connection.Drain();

            var changed = TakeSingle<CharacterLookChangedEvent>();
            Assert.That(changed.WorldId, Is.EqualTo(PinnedFairyWorldId));
            Assert.That(changed.Look.Fairy,
                Is.EqualTo(new Fairy(ItemId: FairyOfEvil, AppearanceItemId: FairyOfLifeApparel, Level: 0, Stamina: 99,
                    MaxStamina: 100, Growth: 240, MaxGrowth: 240)),
                "Fairy of Evil, swapped in for the Fairy of Luck");
        }

        [Test]
        public void A_look_switch_after_the_fairy_was_taken_off_reports_none()
        {
            Subscribe<CharacterLookChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.NotiAction, Unhex(PinnedFairyRemovedHex));

            _connection.Drain();

            Assert.That(TakeSingle<CharacterLookChangedEvent>().Look.Fairy, Is.Null);
        }

        [Test]
        public void A_look_action_on_a_boat_reports_the_ship_bow_and_engine_it_shows()
        {
            Subscribe<ShipLookChangedEvent>();
            _connection.Connect(Host, Port, true);
            var look = LookInfo.OfBoat(LookInfo.ItemDataSync, 301, new BoatLook(401, 11, 115, 1, 21, 62, 73));
            Gate.Listener.OnReceived(Opcode.NotiAction,
                Pack(packet => new LookNotice(CrabWorldId, 42, look).Write(packet)));

            _connection.Drain();

            var changed = TakeSingle<ShipLookChangedEvent>();
            Assert.That(changed.WorldId, Is.EqualTo(CrabWorldId));
            Assert.That((changed.Look.ShipId, changed.Look.BowId, changed.Look.EngineId), Is.EqualTo((11, 115, 21)));
        }

        [Test]
        public void An_appear_packet_of_a_boat_reports_the_ship_it_shows_and_no_character_look()
        {
            Subscribe<CharacterAppearedEvent>();
            _connection.Connect(Host, Port, true);
            var boat = BuildCrabBeginSee(LookInfo.OfBoat(0, 301, new BoatLook(401, 11, 115, 1, 21, 62, 73)));
            Gate.Listener.OnReceived(Opcode.CharacterBeginSee, Pack(packet => boat.Write(packet)));

            _connection.Drain();

            var appeared = TakeSingle<CharacterAppearedEvent>().Character;
            Assert.That(appeared.Look, Is.Null);
            Assert.That((appeared.ShipLook!.ShipId, appeared.ShipLook.BowId, appeared.ShipLook.EngineId),
                Is.EqualTo((11, 115, 21)));
        }

        [Test]
        public void A_move_action_reports_the_points_the_character_travelled_in_meters()
        {
            Subscribe<CharacterMovedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveMove(new MoveInfo(0, null, [new Point(220349, 277825), new Point(219965, 277825)]));

            _connection.Drain();

            var moved = TakeSingle<CharacterMovedEvent>();
            Assert.That(moved.WorldId, Is.EqualTo(CrabWorldId));
            Assert.That(moved.Points, Is.EqualTo(new[] { new Vector2(2203.49f, 2778.25f), new Vector2(2199.65f, 2778.25f) }));
            Assert.That(moved.MoveState, Is.EqualTo(MoveStates.None));
            Assert.That(moved.StopState, Is.Null);
        }

        [Test]
        public void A_move_action_that_ends_reports_why_and_the_state_the_character_stops_in()
        {
            Subscribe<CharacterMovedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveMove(new MoveInfo(2, 1, [new Point(220349, 277825), new Point(220349, 277825)]));

            _connection.Drain();

            var moved = TakeSingle<CharacterMovedEvent>();
            Assert.That(moved.MoveState, Is.EqualTo(MoveStates.Block));
            Assert.That(moved.StopState, Is.EqualTo(1));
        }

        [TestCase(0x01, MoveStates.Arrive)]
        [TestCase(0x02, MoveStates.Block)]
        [TestCase(0x04, MoveStates.Cancel)]
        [TestCase(0x08, MoveStates.InRange)]
        [TestCase(0x10, MoveStates.NoTarget)]
        [TestCase(0x20, MoveStates.CannotMove)]
        [TestCase(0x06, MoveStates.Block | MoveStates.Cancel)]
        public void A_move_action_reports_each_way_a_move_ends(short packetState, MoveStates expected)
        {
            Subscribe<CharacterMovedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveMove(new MoveInfo(packetState, 1, [new Point(219965, 277825), new Point(219849, 277825)]));

            _connection.Drain();

            Assert.That(TakeSingle<CharacterMovedEvent>().MoveState, Is.EqualTo(expected));
        }

        [Test]
        public void A_move_action_with_no_points_reports_nothing_and_is_logged()
        {
            Subscribe<CharacterMovedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveMove(new MoveInfo(0, null, []));

            _connection.Drain();

            Assert.That(_delivered, Is.Empty);
            Assert.That(_log.Entries.Single().Level, Is.EqualTo(LogLevel.Info));
        }

        [Test]
        public void An_action_that_is_neither_a_look_nor_a_move_reports_nothing_and_is_logged()
        {
            Subscribe<CharacterLookChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.NotiAction,
                Pack(packet => new UnreadActionNotice(CrabWorldId, 42, SkillAction).Write(packet)));

            _connection.Drain();

            Assert.That(_delivered, Is.Empty);
            Assert.That(_log.Entries.Single().Level, Is.EqualTo(LogLevel.Info));
            Assert.That(_log.Entries.Single().Message, Does.Contain(Opcode.NotiAction.ToString()));
        }

        [Test]
        public void A_failed_action_reports_the_character_the_action_and_the_reason()
        {
            Subscribe<ActionFailedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveFailedAction(AlreadyActing);

            _connection.Drain();

            var failed = TakeSingle<ActionFailedEvent>();
            Assert.That(failed.WorldId, Is.EqualTo(1));
            Assert.That(failed.ActionType, Is.EqualTo(MoveRequest.Action));
            Assert.That(failed.Reason, Is.EqualTo(ActionFailureReason.AlreadyActing));
        }

        [TestCase(0, ActionFailureReason.Forbidden)]
        [TestCase(1, ActionFailureReason.AlreadyActing)]
        [TestCase(2, ActionFailureReason.BadPath)]
        [TestCase(3, ActionFailureReason.CannotMove)]
        public void A_failed_action_reports_each_reason_an_action_is_refused(byte packetReason,
            ActionFailureReason expected)
        {
            Subscribe<ActionFailedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveFailedAction(packetReason);

            _connection.Drain();

            Assert.That(TakeSingle<ActionFailedEvent>().Reason, Is.EqualTo(expected));
        }

        [Test]
        public void A_pre_move_time_reports_its_milliseconds()
        {
            Subscribe<PreMoveTimeChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.PreMoveTime, Pack(packet => new PreMoveTime(500).Write(packet)));

            _connection.Drain();

            Assert.That(TakeSingle<PreMoveTimeChangedEvent>().Milliseconds, Is.EqualTo(500));
        }

        [Test]
        public void A_regeneration_sync_reports_the_new_health_and_nothing_else()
        {
            Subscribe<CharacterAttributesChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.AttributeSync, Unhex(PinnedRegenerationHex));

            _connection.Drain();

            var changed = TakeSingle<CharacterAttributesChangedEvent>();
            Assert.That(changed.WorldId, Is.EqualTo(PinnedWorldId));
            Assert.That(changed.Reason, Is.EqualTo(AttributeChangeReason.Regeneration));
            Assert.That(changed.Attributes, Is.EqualTo(new ChangedAttributes { Health = 32 }));
        }

        [Test]
        public void A_level_up_sync_reports_the_vitals_and_the_experience_above_the_signed_range()
        {
            Subscribe<CharacterAttributesChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.AttributeSync, Unhex(PinnedLevelEightyHex));

            _connection.Drain();

            var changed = TakeSingle<CharacterAttributesChangedEvent>();
            Assert.That(changed.Reason, Is.EqualTo(AttributeChangeReason.Quest), "a GM experience grant");
            Assert.That(changed.Attributes, Is.EqualTo(new ChangedAttributes
            {
                Level = 80,
                Health = 2115,
                MaxHealth = 2115,
                Mana = 260,
                MaxMana = 260,
                Experience = 4_000_000_000,
                LevelThreshold = 3_924_326_032,
                NextLevelThreshold = 4_176_709_541,
            }), "the server sends the thresholds of levels 99 and 100 with the level held at 80");
        }

        [Test]
        public void A_job_change_sync_reports_the_job_index()
        {
            Subscribe<CharacterAttributesChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.AttributeSync, Unhex(PinnedSwordsmanHex));

            _connection.Drain();

            Assert.That(TakeSingle<CharacterAttributesChangedEvent>().Attributes,
                Is.EqualTo(new ChangedAttributes { Job = 1, MaxHealth = 140 }));
        }

        [Test]
        public void A_move_speed_sync_reports_the_new_speed_in_meters_per_second()
        {
            Subscribe<CharacterAttributesChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.AttributeSync, Unhex(PinnedSpeedRaisedHex));

            _connection.Drain();

            Assert.That(TakeSingle<CharacterAttributesChangedEvent>().Attributes,
                Is.EqualTo(new ChangedAttributes { MoveSpeed = 6f }), "600 cm/s in the packet");
        }

        [TestCase(0, AttributeChangeReason.Initial)]
        [TestCase(1, AttributeChangeReason.Equipment)]
        [TestCase(2, AttributeChangeReason.ItemUse)]
        [TestCase(3, AttributeChangeReason.Combat)]
        [TestCase(4, AttributeChangeReason.Quest)]
        [TestCase(5, AttributeChangeReason.Trade)]
        [TestCase(6, AttributeChangeReason.PointReassign)]
        [TestCase(7, AttributeChangeReason.SkillState)]
        [TestCase(8, AttributeChangeReason.Regeneration)]
        [TestCase(9, AttributeChangeReason.JobChange)]
        [TestCase(10, AttributeChangeReason.Correction)]
        public void An_attribute_sync_reports_the_reason_its_sync_type_names(byte syncType,
            AttributeChangeReason expected)
        {
            Subscribe<CharacterAttributesChangedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveAttributeSync(syncType, new CharacterAttribute(CharacterAttribute.Health, 90));

            _connection.Drain();

            Assert.That(TakeSingle<CharacterAttributesChangedEvent>().Reason, Is.EqualTo(expected));
        }

        [Test]
        public void An_attribute_sync_with_no_attribute_the_client_knows_reports_nothing_and_is_logged()
        {
            Subscribe<CharacterAttributesChangedEvent>();
            _connection.Connect(Host, Port, true);
            ReceiveAttributeSync(TaskSync, new CharacterAttribute(BaseMoveSpeed, 600));

            _connection.Drain();

            Assert.That(_delivered, Is.Empty);
            Assert.That(_log.Entries.Single().Level, Is.EqualTo(LogLevel.Info));
        }

        [Test]
        public void A_disappear_packet_reports_the_world_id_and_the_see_type()
        {
            Subscribe<CharacterDisappearedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.CharacterEndSee,
                Pack(packet => new CharacterEndSee(SeenSwitch, CrabWorldId).Write(packet)));

            _connection.Drain();

            var disappeared = TakeSingle<CharacterDisappearedEvent>();
            Assert.That(disappeared.WorldId, Is.EqualTo(CrabWorldId));
            Assert.That(disappeared.SeeType, Is.EqualTo(SeeType.Switch));
        }

        [Test]
        public void A_party_invite_reports_the_inviter_id_and_name()
        {
            Subscribe<PartyInvitedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.PartyInvite,
                Pack(packet => new PartyInvite("Anne", 9, 1200).Write(packet)));

            _connection.Drain();

            var invited = TakeSingle<PartyInvitedEvent>();
            Assert.That(invited.InviterId, Is.EqualTo(9u));
            Assert.That(invited.InviterName, Is.EqualTo("Anne"));
        }

        [TestCase(1, PartyInviteEndReason.Busy)]
        [TestCase(2, PartyInviteEndReason.Timeout)]
        [TestCase(3, PartyInviteEndReason.InviterOffline)]
        [TestCase(4, PartyInviteEndReason.PartyFull)]
        [TestCase(5, PartyInviteEndReason.Cancelled)]
        public void A_party_invite_cancel_reports_the_inviter_id_and_each_reason(byte packetReason,
            PartyInviteEndReason expected)
        {
            Subscribe<PartyInviteEndedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.PartyInviteCancel,
                Pack(packet => new PartyInviteCancel(packetReason, 9).Write(packet)));

            _connection.Drain();

            var ended = TakeSingle<PartyInviteEndedEvent>();
            Assert.That(ended.InviterId, Is.EqualTo(9u));
            Assert.That(ended.Reason, Is.EqualTo(expected));
        }

        [Test]
        public void An_accept_invite_is_sent_as_the_party_accept_packet_with_the_inviter_id()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new AcceptInviteCommand(9));

            var sent = Gate.Sent.Single();
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.PartyAccept));
            Assert.That(PartyAccept.Read(new PacketReader(sent.Payload)).InviterId, Is.EqualTo(9u));
        }

        [Test]
        public void A_decline_invite_is_sent_as_the_party_refuse_packet_with_the_inviter_id()
        {
            _connection.Connect(Host, Port, true);

            _connection.Send(new DeclineInviteCommand(9));

            var sent = Gate.Sent.Single();
            Assert.That(sent.Opcode, Is.EqualTo(Opcode.PartyRefuse));
            Assert.That(PartyRefuse.Read(new PacketReader(sent.Payload)).InviterId, Is.EqualTo(9u));
        }

        [Test]
        public void A_pinned_join_reports_the_full_list_and_the_member_that_joined()
        {
            Subscribe<PartyChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.PartyRefresh, Unhex(PinnedPartyJoinHex));

            _connection.Drain();

            var changed = TakeSingle<PartyChangedEvent>();
            Assert.That(changed.Kind, Is.EqualTo(PartyChangeKind.Joined));
            Assert.That(changed.ChangedMemberName, Is.EqualTo("Observer2"));
            Assert.That(changed.Members, Is.EqualTo(new[] { new PartyMember(12, "T1"), new PartyMember(4, "Observer2") }));
        }

        [TestCase(PinnedMemberLeavesHex, PartyChangeKind.Left, "Observer2")]
        [TestCase(PinnedKickHex, PartyChangeKind.Kicked, "Observer2")]
        [TestCase(PinnedLeaderLeavesHex, PartyChangeKind.Left, "T1")]
        public void A_pinned_change_that_leaves_one_member_ends_the_party(string hex, PartyChangeKind kind,
            string changedMemberName)
        {
            Subscribe<PartyChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.PartyRefresh, Unhex(hex));

            _connection.Drain();

            var changed = TakeSingle<PartyChangedEvent>();
            Assert.That(changed.Kind, Is.EqualTo(kind));
            Assert.That(changed.ChangedMemberName, Is.EqualTo(changedMemberName));
            Assert.That(changed.Members, Is.Empty);
        }

        [TestCase(PartyRefresh.Leave, PartyChangeKind.Left)]
        [TestCase(PartyRefresh.Kick, PartyChangeKind.Kicked)]
        public void A_change_that_leaves_two_members_reports_the_list_without_its_last_entry(byte refreshKind,
            PartyChangeKind kind)
        {
            Subscribe<PartyChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.PartyRefresh, Pack(packet => new PartyRefresh(refreshKind,
            [
                new PartyMemberInfo(12, "T1", string.Empty, 1),
                new PartyMemberInfo(4, "Observer2", string.Empty, 1),
                new PartyMemberInfo(7, "Anne", string.Empty, 1),
            ]).Write(packet)));

            _connection.Drain();

            var changed = TakeSingle<PartyChangedEvent>();
            Assert.That(changed.Kind, Is.EqualTo(kind));
            Assert.That(changed.ChangedMemberName, Is.EqualTo("Anne"));
            Assert.That(changed.Members, Is.EqualTo(new[] { new PartyMember(12, "T1"), new PartyMember(4, "Observer2") }));
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void A_refresh_of_a_kind_the_client_never_receives_reports_nothing_and_is_logged(byte refreshKind)
        {
            Subscribe<PartyChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.PartyRefresh, Pack(packet => new PartyRefresh(refreshKind,
                [new PartyMemberInfo(12, "T1", string.Empty, 1)]).Write(packet)));

            _connection.Drain();

            Assert.That(_delivered, Is.Empty);
            Assert.That(_log.Entries.Single().Level, Is.EqualTo(LogLevel.Info));
        }

        [Test]
        public void Pinned_member_vitals_report_the_vitals_the_level_and_the_look()
        {
            Subscribe<PartyMemberVitalsChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.PartyMemberVitals, Unhex(PinnedObserver2VitalsHex));

            _connection.Drain();

            var changed = TakeSingle<PartyMemberVitalsChangedEvent>();
            Assert.That(changed.MemberId, Is.EqualTo(4u));
            Assert.That(changed.Vitals.Level, Is.EqualTo(1));
            Assert.That(changed.Vitals.Health, Is.EqualTo(109));
            Assert.That(changed.Vitals.MaxHealth, Is.EqualTo(109));
            Assert.That(changed.Vitals.Mana, Is.EqualTo(28));
            Assert.That(changed.Vitals.MaxMana, Is.EqualTo(38));
            Assert.That(changed.Vitals.TypeId, Is.EqualTo(1));
            Assert.That(changed.Vitals.Look.HairId, Is.EqualTo(2000));
            Assert.That(changed.Vitals.Look.EquipmentIds[FairyLink], Is.EqualTo(FairyOfLuck));
            Assert.That(changed.Vitals.Look.Fairy.AppearanceItemId, Is.EqualTo(FairyOfLifeApparel));
        }

        [Test]
        public void A_pinned_leader_notice_reports_the_character_and_its_team_leader()
        {
            Subscribe<CharacterTeamLeaderChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.PartyLeaderNotice, Unhex(PinnedLedByT1Hex));

            _connection.Drain();

            var changed = TakeSingle<CharacterTeamLeaderChangedEvent>();
            Assert.That(changed.WorldId, Is.EqualTo(4u));
            Assert.That(changed.TeamLeaderId, Is.EqualTo(12u));
        }

        [Test]
        public void A_pinned_leader_notice_after_the_party_ended_reports_no_team_leader()
        {
            Subscribe<CharacterTeamLeaderChangedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.PartyLeaderNotice, Unhex(PinnedLeaderClearedHex));

            _connection.Drain();

            var changed = TakeSingle<CharacterTeamLeaderChangedEvent>();
            Assert.That(changed.WorldId, Is.EqualTo(12u));
            Assert.That(changed.TeamLeaderId, Is.Zero);
        }

        [Test]
        public void A_send_before_any_connection_is_logged_and_dropped()
        {
            _connection.Send(new LogInCommand(Account, Password));

            Assert.That(_gates, Is.Empty);
            Assert.That(_log.Entries.Select(entry => entry.Level), Is.EqualTo(new[] { LogLevel.Warning }));
        }

        [Test]
        public void A_send_after_the_disconnect_landed_is_logged_and_dropped()
        {
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnClosed(CloseReason.GateClosed);
            _connection.Drain();

            _connection.Send(new LogInCommand(Account, Password));

            Assert.That(Gate.Sent, Is.Empty);
            Assert.That(_log.Entries.Select(entry => entry.Level), Is.EqualTo(new[] { LogLevel.Warning }));
        }

        [Test]
        public void A_connection_is_open_from_the_connect_until_the_disconnect_lands()
        {
            Assert.That(_connection.IsOpen, Is.False);

            _connection.Connect(Host, Port, true);

            Assert.That(_connection.IsOpen, Is.True);

            Gate.Listener.OnClosed(CloseReason.GateClosed);
            _connection.Drain();

            Assert.That(_connection.IsOpen, Is.False);
        }

        [Test]
        public void A_command_with_no_encoder_is_refused()
        {
            _connection.Connect(Host, Port, true);

            Assert.Throws<ArgumentException>(() => _connection.Send(new UnknownCommand()));
        }

        [TestCase(CloseReason.CallerClosed, DisconnectReason.ClientClosed)]
        [TestCase(CloseReason.Unreachable, DisconnectReason.Unreachable)]
        [TestCase(CloseReason.HandshakeFailed, DisconnectReason.HandshakeFailed)]
        [TestCase(CloseReason.GateClosed, DisconnectReason.ServerClosed)]
        [TestCase(CloseReason.TimedOut, DisconnectReason.TimedOut)]
        [TestCase(CloseReason.Broken, DisconnectReason.Broken)]
        public void Every_close_reason_reaches_the_consumer(CloseReason closeReason, DisconnectReason expected)
        {
            Subscribe<DisconnectedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnClosed(closeReason);

            _connection.Drain();

            Assert.That(TakeSingle<DisconnectedEvent>().Reason, Is.EqualTo(expected));
        }

        [Test]
        public void Closing_reports_the_disconnect_on_the_next_drain()
        {
            Subscribe<DisconnectedEvent>();
            _connection.Connect(Host, Port, true);

            _connection.Close();

            Assert.That(Gate.IsDisposed, Is.True);
            Assert.That(_delivered, Is.Empty);

            _connection.Drain();

            Assert.That(TakeSingle<DisconnectedEvent>().Reason, Is.EqualTo(DisconnectReason.ClientClosed));
        }

        [Test]
        public void A_close_from_inside_a_handler_lands_on_the_next_drain()
        {
            _connection.Subscribe<ConnectedEvent>(_ => _connection.Close());
            Subscribe<DisconnectedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnOpened();

            _connection.Drain();

            Assert.That(_delivered, Is.Empty);

            _connection.Drain();

            Assert.That(TakeSingle<DisconnectedEvent>().Reason, Is.EqualTo(DisconnectReason.ClientClosed));
        }

        [Test]
        public void Handlers_run_on_the_draining_thread_in_the_order_the_packets_were_received()
        {
            var threads = new List<int>();
            _connection.Subscribe<LoginFailedEvent>(rejected =>
            {
                threads.Add(Environment.CurrentManagedThreadId);
                _delivered.Add(rejected);
            });
            _connection.Connect(Host, Port, true);
            var listener = Gate.Listener;

            RunOnAnotherThread(() =>
            {
                for (ushort code = 1; code <= 3; code++)
                {
                    listener.OnReceived(Opcode.LoginReply, BuildRefusedReply(code, string.Empty));
                }
            });
            _connection.Drain();

            Assert.That(threads, Is.EqualTo(Enumerable.Repeat(Environment.CurrentManagedThreadId, 3)));
            Assert.That(_delivered.Cast<LoginFailedEvent>().Select(rejected => rejected.Code), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Messages_that_arrive_while_the_drain_is_held_are_delivered_in_order_once_it_resumes()
        {
            Subscribe<LoginFailedEvent>();
            _connection.Connect(Host, Port, true);
            _connection.PauseDrain();

            for (ushort code = 1; code <= 3; code++)
            {
                Gate.Listener.OnReceived(Opcode.LoginReply, BuildRefusedReply(code, string.Empty));
                _connection.Drain();
            }

            Assert.That(_delivered, Is.Empty);

            _connection.ResumeDrain();
            _connection.Drain();

            Assert.That(_delivered.Cast<LoginFailedEvent>().Select(failed => failed.Code), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void A_handler_that_holds_the_drain_leaves_what_follows_until_it_resumes()
        {
            _connection.Subscribe<MapEnteredEvent>(_ => _connection.PauseDrain());
            Subscribe<DisconnectedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnReceived(Opcode.EnterMap, Pack(packet => BuildEnterMapReply().Write(packet)));
            Gate.Listener.OnClosed(CloseReason.GateClosed);

            _connection.Drain();

            Assert.That(_delivered, Is.Empty);

            _connection.ResumeDrain();
            _connection.Drain();

            Assert.That(TakeSingle<DisconnectedEvent>().Reason, Is.EqualTo(DisconnectReason.ServerClosed));
        }

        [Test]
        public void Connecting_again_closes_the_earlier_connection()
        {
            _connection.Connect(Host, Port, true);
            var earlier = Gate;

            _connection.Connect(Host, Port, true);

            Assert.That(earlier.IsDisposed, Is.True);
            Assert.That(Gate, Is.Not.SameAs(earlier));
            Assert.That(Gate.IsDisposed, Is.False);
        }

        [Test]
        public void The_close_of_a_replaced_connection_is_never_delivered()
        {
            Subscribe<DisconnectedEvent>();
            _connection.Connect(Host, Port, true);

            _connection.Connect(Host, Port, true);
            _connection.Drain();

            Assert.That(_delivered, Is.Empty);
        }

        [Test]
        public void A_late_packet_from_a_replaced_connection_is_never_delivered()
        {
            Subscribe<ConnectedEvent>();
            Subscribe<LoginSucceededEvent>();
            Subscribe<DisconnectedEvent>();
            _connection.Connect(Host, Port, true);
            var replaced = Gate;
            replaced.ReportsCloseOnDispose = false;
            _connection.Connect(Host, Port, true);

            replaced.Listener.OnOpened();
            replaced.Listener.OnReceived(Opcode.LoginReply, BuildAcceptedReply("Blackbeard"));
            replaced.Listener.OnClosed(CloseReason.Broken);
            _connection.Drain();

            Assert.That(_delivered, Is.Empty);
        }

        [Test]
        public void Nothing_is_delivered_from_a_connection_after_its_disconnect()
        {
            Subscribe<ConnectedEvent>();
            Subscribe<LoginSucceededEvent>();
            Subscribe<DisconnectedEvent>();
            _connection.Connect(Host, Port, true);
            var listener = Gate.Listener;

            listener.OnClosed(CloseReason.Broken);
            listener.OnReceived(Opcode.LoginReply, BuildAcceptedReply("Blackbeard"));
            listener.OnOpened();
            listener.OnClosed(CloseReason.GateClosed);
            _connection.Drain();

            Assert.That(_delivered, Has.Count.EqualTo(1).And.All.InstanceOf<DisconnectedEvent>());
        }

        [Test]
        public void Subscriptions_survive_a_reconnect()
        {
            Subscribe<ConnectedEvent>();
            _connection.Connect(Host, Port, true);
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnOpened();

            _connection.Drain();

            Assert.That(_delivered, Has.Count.EqualTo(1).And.All.InstanceOf<ConnectedEvent>());
        }

        [Test]
        public void A_handler_that_fails_is_logged_and_the_others_still_run()
        {
            _connection.Subscribe<ConnectedEvent>(_ => throw new InvalidOperationException("the handler failed"));
            Subscribe<ConnectedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnOpened();

            _connection.Drain();

            Assert.That(_delivered, Has.Count.EqualTo(1));
            Assert.That(_log.Entries.Single().Level, Is.EqualTo(LogLevel.Error));
            Assert.That(_log.Entries.Single().Exception.Message, Is.EqualTo("the handler failed"));
        }

        [Test]
        public void A_packet_with_no_decoder_is_logged_each_time_it_is_received()
        {
            _connection.Connect(Host, Port, true);

            Gate.Listener.OnReceived(SystemInfo, new PacketReader(Array.Empty<byte>()));
            Gate.Listener.OnReceived(SystemInfo, new PacketReader(Array.Empty<byte>()));

            Assert.That(_log.Entries.Select(entry => entry.Level), Is.EqualTo(new[] { LogLevel.Info, LogLevel.Info }));
            Assert.That(_log.Entries.Select(entry => entry.Message), Has.All.Contains(SystemInfo.ToString()));
        }

        [Test]
        public void Subscribing_to_the_event_marker_is_refused()
        {
            Assert.Throws<ArgumentException>(() => _connection.Subscribe<IServerEvent>(_ => { }));
        }

        [Test]
        public void Subscribing_to_an_abstract_event_is_refused()
        {
            Assert.Throws<ArgumentException>(() => _connection.Subscribe<AbstractEvent>(_ => { }));
        }

        [Test]
        public void A_subscription_matches_its_exact_type_only()
        {
            Subscribe<LoginFailedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnOpened();
            Gate.Listener.OnReceived(Opcode.LoginReply, BuildAcceptedReply("Blackbeard"));

            _connection.Drain();

            Assert.That(_delivered, Is.Empty);
        }

        [Test]
        public void A_disposed_subscription_receives_nothing_more()
        {
            var subscription = Subscribe<ConnectedEvent>();
            _connection.Connect(Host, Port, true);
            Gate.Listener.OnOpened();

            subscription.Dispose();
            _connection.Drain();

            Assert.That(_delivered, Is.Empty);
        }

        private FakeGate Gate => _gates[^1];

        private IGateConnection OpenGate(string host, int port, bool isEncrypted)
        {
            var gate = new FakeGate(host, port, isEncrypted);
            _gates.Add(gate);

            return gate;
        }

        private IDisposable Subscribe<TEvent>() where TEvent : IServerEvent
        {
            return _connection.Subscribe<TEvent>(serverEvent => _delivered.Add(serverEvent));
        }

        private TEvent TakeSingle<TEvent>() where TEvent : IServerEvent
        {
            Assert.That(_delivered, Has.Count.EqualTo(1).And.All.InstanceOf<TEvent>());

            return (TEvent)_delivered[0];
        }

        private static PacketReader BuildAcceptedReply(params string[] names)
        {
            var characters = names
                .Select(name => new CharacterSummary(name, "Explorer", 1, 1,
                    new ushort[CharacterSummary.EquipmentSlots]))
                .ToList();

            return Pack(LoginReply.Accepted(3, characters, false));
        }

        private static MoveCommand BuildMoveCommand()
        {
            return new MoveCommand(1, [new Vector2(2246.75f, 2826.75f), new Vector2(2244.75f, 2826.75f)]);
        }

        private static uint ReadPacketId((ushort Opcode, byte[] Payload) sent)
        {
            return BeginActionRequest.Read(new PacketReader(sent.Payload)).PacketId;
        }

        private static (int, int?, int?) SummarizeSlot(BagSlot slot)
        {
            return (slot.Index, slot.Item?.ItemId, slot.Item?.Count);
        }

        private void ReceiveMove(MoveInfo move)
        {
            Gate.Listener.OnReceived(Opcode.NotiAction,
                Pack(packet => new MoveNotice(CrabWorldId, 42, move).Write(packet)));
        }

        private void ReceiveLook(LookInfo look)
        {
            Gate.Listener.OnReceived(Opcode.NotiAction,
                Pack(packet => new LookNotice(CrabWorldId, 42, look).Write(packet)));
        }

        private void ReceiveKitbag(KitbagSync kitbag)
        {
            Gate.Listener.OnReceived(Opcode.NotiAction,
                Pack(packet => new KitbagNotice(CrabWorldId, 42, kitbag).Write(packet)));
        }

        private void ReceiveFailedAction(byte reason)
        {
            Gate.Listener.OnReceived(Opcode.FailedAction,
                Pack(packet => new FailedActionNotice(1, MoveRequest.Action, reason).Write(packet)));
        }

        private void ReceiveAttributeSync(byte syncType, params CharacterAttribute[] attributes)
        {
            Gate.Listener.OnReceived(Opcode.AttributeSync,
                Pack(packet => new AttributeSync(CrabWorldId, syncType, attributes).Write(packet)));
        }

        private void ReceiveCreateSecondPasswordReply(ushort code)
        {
            Gate.Listener.OnReceived(Opcode.CreateSecondPasswordReply,
                Pack(packet => new CreateSecondPasswordReply(code).Write(packet)));
        }

        private void ReceiveChangeSecondPasswordReply(ushort code)
        {
            Gate.Listener.OnReceived(Opcode.ChangeSecondPasswordReply,
                Pack(packet => new ChangeSecondPasswordReply(code).Write(packet)));
        }

        private void ReceiveDeleteCharacterReply(ushort code)
        {
            Gate.Listener.OnReceived(Opcode.DeleteCharacterReply,
                Pack(packet => new DeleteCharacterReply(code).Write(packet)));
        }

        private static ushort[] BuildEquipmentIds()
        {
            var equipmentIds = new ushort[CharacterSummary.EquipmentSlots];

            equipmentIds[0] = (ushort)ShortHair;
            equipmentIds[RightHandLink] = (ushort)ShortSword;

            return equipmentIds;
        }

        private static PacketReader BuildRefusedReply(ushort code, string text)
        {
            return Pack(LoginReply.Refused(code, text));
        }

        private static PacketReader Pack(LoginReply reply)
        {
            return Pack(reply.Write);
        }

        private static PacketReader Unhex(string hex)
        {
            return new PacketReader(Convert.FromHexString(hex));
        }

        private static PacketReader Pack(Action<PacketWriter> write)
        {
            var packet = new PacketWriter();
            write(packet);

            return new PacketReader(packet.ToArray());
        }

        private static ItemGrid[] BuildLinks()
        {
            return Enumerable.Repeat(ItemGrid.Empty, LookInfo.LinkCount).ToArray();
        }

        private static ItemGrid BuildWornGrid(short id)
        {
            return new ItemGrid(id, 0xBEEF, 0, 1, new short[2], new short[2], 0, true, true, 0, null, 0, 0, null);
        }

        private static ItemGrid BuildFairyGrid(short id, short endure, short maxEndure, short energy, short maxEnergy,
            bool isValid = true)
        {
            var attributes = new[]
            {
                new ItemInstanceAttribute(26, 3),
                new ItemInstanceAttribute(27, 2),
                new ItemInstanceAttribute(28, 4),
                new ItemInstanceAttribute(1, 99),
                new ItemInstanceAttribute(30, 4)
            };

            return new ItemGrid(id, 0xFA1, 0, 1, new[] { endure, maxEndure }, new[] { energy, maxEnergy }, 0, isValid,
                true, 0, null, 0, 7, attributes);
        }

        private static ItemGrid BuildSwordGrid(ItemInstanceAttribute[] attributes = null, uint lockValue = 0xBEE)
        {
            return new ItemGrid(Sword, lockValue, 10, 1, new short[] { 9_000, 10_000 }, new short[] { 120, 3_450 }, 2,
                true, false, 1_800_000_000, null, 123_456, 7, attributes);
        }

        private static EnterMapReply BuildEnterMapReply(byte enterType = EnteredByCarry, LookInfo look = null,
            KitbagSlot[] kitbag = null, bool isKitbagLocked = false)
        {
            var actor = new ActorCreate(4, 1, 1, "Blackbeard", 0, 33566253, 1, "Blackbeard", string.Empty, 1, 12,
                "Crew", "Yarr", 2, string.Empty, 1, new Point(190261, 284880), 40, 359, 9, true, 0,
                new EntityEvent(1, 1, 0, string.Empty), look ?? LookInfo.OfCharacter(0, 1, 2000, BuildLinks()), 0,
                Enumerable.Repeat(new AppendLook(0, false), AppendLook.Slots).ToArray());
            var attributes = new[]
            {
                new CharacterAttribute(CharacterAttribute.Level, 15),
                new CharacterAttribute(CharacterAttribute.Health, 120),
                new CharacterAttribute(CharacterAttribute.MaxHealth, 300),
                new CharacterAttribute(CharacterAttribute.MoveSpeed, 480)
            };
            var shortcuts = Enumerable.Repeat(new ShortcutSlot(0, 0), ShortcutSlot.Count).ToArray();

            return new EnterMapReply(SwitchMap.Entered(enterType, false, "garner", true), false, isKitbagLocked, 0,
                actor, 28, 0, Array.Empty<SkillSlot>(), 0, Array.Empty<SkillState>(), 0, attributes,
                new KitbagSync(KitbagInit, 24, kitbag ?? Array.Empty<KitbagSlot>()), shortcuts, Array.Empty<Boat>(), 1);
        }

        private static CharacterBeginSee BuildCrabBeginSee(LookInfo look = null,
            CharacterAttribute[] attributes = null)
        {
            var noLinks = Enumerable.Repeat(ItemGrid.Empty, LookInfo.LinkCount).ToArray();
            var actor = new ActorCreate(107, CrabWorldId, CrabWorldId, string.Empty, 0, 67108867, 5, "Crab",
                string.Empty, 0, 0, string.Empty, string.Empty, 0, string.Empty, 1, new Point(219220, 276700), 40, 90,
                0, false, 0, new EntityEvent(0, 1, 0, string.Empty),
                look ?? LookInfo.OfCharacter(0, 107, 0, noLinks), 0,
                Enumerable.Repeat(new AppendLook(0, false), AppendLook.Slots).ToArray());
            attributes ??= new[]
            {
                new CharacterAttribute(CharacterAttribute.Level, 3),
                new CharacterAttribute(CharacterAttribute.Health, 40),
                new CharacterAttribute(CharacterAttribute.MaxHealth, 65),
                new CharacterAttribute(CharacterAttribute.MoveSpeed, 350)
            };

            return new CharacterBeginSee(SeenNew, actor, new NpcShow(0, 0), 0, null, null, 0, attributes, 0,
                Array.Empty<SkillState>());
        }

        private static void RunOnAnotherThread(Action action)
        {
            var thread = new Thread(() => action());
            thread.Start();
            thread.Join();
        }

        private class UnknownCommand : IServerCommand
        {
        }

        private abstract class AbstractEvent : IServerEvent
        {
        }
    }
}
