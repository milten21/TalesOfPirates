using Top.Client.LegacyBridge.Account;
using Top.Client.LegacyBridge.Chat;
using Top.Client.LegacyBridge.Inventory;
using Top.Client.LegacyBridge.Party;
using Top.Client.LegacyBridge.Play;
using Top.Client.Messages.Account.Commands;
using Top.Client.Messages.Chat.Commands;
using Top.Client.Messages.Inventory.Commands;
using Top.Client.Messages.Party.Commands;
using Top.Client.Messages.Play.Commands;
using Top.Legacy.Protocol;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge
{
    public partial class ServerConnection
    {
        private void RegisterMessages(GateSettings settings)
        {
            AddDecoder(Opcode.LoginReply, LoginDecoder.Decode);
            AddEncoder<LogInCommand>(Opcode.Login, new LoginEncoder(settings).Encode);
            AddEncoder<CreateSecurityCodeCommand>(Opcode.CreateSecondPassword, CreateSecurityCodeEncoder.Encode);
            AddDecoder(Opcode.CreateSecondPasswordReply, CreateSecurityCodeDecoder.Decode);
            AddEncoder<ChangeSecurityCodeCommand>(Opcode.ChangeSecondPassword, ChangeSecurityCodeEncoder.Encode);
            AddDecoder(Opcode.ChangeSecondPasswordReply, ChangeSecurityCodeDecoder.Decode);
            AddEncoder<DeleteCharacterCommand>(Opcode.DeleteCharacter, DeleteCharacterEncoder.Encode);
            AddDecoder(Opcode.DeleteCharacterReply, DeleteCharacterDecoder.Decode);
            AddDecoder(Opcode.BeginPlayReply, BeginPlayDecoder.Decode);
            AddDecoder(Opcode.EnterMap, EnterMapDecoder.Decode);
            AddEncoder<BeginPlayCommand>(Opcode.BeginPlay, BeginPlayEncoder.Encode);
            AddEncoder<EndPlayCommand>(Opcode.EndPlay, EndPlayEncoder.Encode);
            AddDecoder(Opcode.EndPlayReply, EndPlayDecoder.Decode);
            AddDecoder(Opcode.CharacterBeginSee, CharacterBeginSeeDecoder.Decode);
            AddDecoder(Opcode.CharacterEndSee, CharacterEndSeeDecoder.Decode);
            AddDecoder(Opcode.NotiAction, ActionNoticeDecoder.Decode);
            AddDecoder(Opcode.AttributeSync, AttributeSyncDecoder.Decode);
            AddEncoder<MoveCommand>(Opcode.BeginAction, (command, packet) =>
                MoveEncoder.Encode(command, IssuePacketId(), packet));
            AddEncoder<StopMoveCommand>(Opcode.EndAction, StopMoveEncoder.Encode);
            AddDecoder(Opcode.FailedAction, FailedActionDecoder.Decode);
            AddDecoder(Opcode.PreMoveTime, PreMoveTimeDecoder.Decode);
            AddEncoder<SyncTemporaryBagCommand>(Opcode.TemporaryKitbagSync, SyncTemporaryBagEncoder.Encode);
            AddDecoder(Opcode.TemporaryKitbagSyncReply, TemporaryBagSyncDecoder.Decode);
            AddEncoder<LockBagCommand>(Opcode.KitbagLock, LockBagEncoder.Encode);
            AddEncoder<UnlockBagCommand>(Opcode.KitbagUnlock, UnlockBagEncoder.Encode);
            AddDecoder(Opcode.KitbagLockState, BagLockDecoder.Decode);
            AddEncoder<MoveBagItemCommand>(Opcode.BeginAction, (command, packet) =>
                MoveBagItemEncoder.Encode(command, IssuePacketId(), packet));
            AddEncoder<EquipBagItemCommand>(Opcode.BeginAction, (command, packet) =>
                EquipBagItemEncoder.Encode(command, IssuePacketId(), packet));
            AddEncoder<UseBagItemCommand>(Opcode.BeginAction, (command, packet) =>
                UseBagItemEncoder.Encode(command, IssuePacketId(), packet));
            AddEncoder<UnequipItemCommand>(Opcode.BeginAction, (command, packet) =>
                UnequipItemEncoder.Encode(command, IssuePacketId(), packet));
            AddEncoder<TakeTemporaryBagItemCommand>(Opcode.BeginAction, (command, packet) =>
                TakeTemporaryBagItemEncoder.Encode(command, IssuePacketId(), packet));
            AddEncoder<ThrowBagItemCommand>(Opcode.BeginAction, (command, packet) =>
                ThrowBagItemEncoder.Encode(command, IssuePacketId(), packet));
            AddEncoder<DeleteBagItemCommand>(Opcode.BeginAction, (command, packet) =>
                DeleteBagItemEncoder.Encode(command, IssuePacketId(), packet));
            AddDecoder(Opcode.PartyInvite, PartyInviteDecoder.Decode);
            AddDecoder(Opcode.PartyInviteCancel, PartyInviteCancelDecoder.Decode);
            AddDecoder(Opcode.PartyRefresh, PartyRefreshDecoder.Decode);
            AddDecoder(Opcode.PartyMemberVitals, PartyMemberVitalsDecoder.Decode);
            AddDecoder(Opcode.PartyLeaderNotice, PartyLeaderNoticeDecoder.Decode);
            AddEncoder<AcceptInviteCommand>(Opcode.PartyAccept, AcceptInviteEncoder.Encode);
            AddEncoder<DeclineInviteCommand>(Opcode.PartyRefuse, DeclineInviteEncoder.Encode);
            AddEncoder<InviteToPartyCommand>(Opcode.PartyInviteRequest, InviteToPartyEncoder.Encode);
            AddEncoder<LeavePartyCommand>(Opcode.PartyLeave, LeavePartyEncoder.Encode);
            AddEncoder<KickPartyMemberCommand>(Opcode.PartyKick, KickPartyMemberEncoder.Encode);
            AddEncoder<SayCommand>(Opcode.Say, SayEncoder.Encode);
        }
    }
}
