# Legacy protocol packets

This folder records every packet that `src/Top.Legacy.Protocol` reads or writes. For each packet it gives:

- the direction, the opcode and our reader or writer type
- the payload, with each variant
- each situation in which the original server sends the packet, or how the server reacts to it
- how our client handles each situation
- the gaps: what the server can send that we do not read or do not handle

| File | Packets |
|---|---|
| [session.md](session.md) | ServerPublicKey 943, PrivateKey 355, Ping 515/15, CheckPing 537/17, GroupPing 5022/6022, Login 431/931, Logout 432, DeleteCharacter 436/936, CreateSecondPassword 346/941, ChangeSecondPassword 347/942, BeginPlay 433/933, EndPlay 434/934 |
| [view.md](view.md) | EnterMap 516, CharacterBeginSee 504, CharacterEndSee 505, and the blocks they embed |
| [actions.md](actions.md) | NotiAction 508 (all action types), BeginAction 6, EndAction 7, FailedAction 520, PreMoveTime 536 |
| [attributes.md](attributes.md) | AttributeSync 509 |
| [items.md](items.md) | TemporaryKitbagSync 35/554, KitbagLock 31, KitbagUnlock 32, lock state 553 |
| [chat.md](chat.md) | Say 1 |
| [party.md](party.md) | PartyInvite 5001, PartyRefresh 5002, PartyInviteCancel 5003, PartyInviteRequest 6001, PartyAccept 6002, PartyRefuse 6003, PartyLeave 6004, PartyKick 6005, PartyMemberVitals 519, PartyLeaderNotice 524 |
| [opcodes.md](opcodes.md) | Inventory of all 383 opcodes: server status, original client status, our status, feature group |

The research was done on 2026-09-27 against `sources/` of the reference repo and the working tree of
`prototype/godot`. Line numbers change when the code changes. Use them as a start point, not as a fact.

## Conventions

- File paths are relative to the root of the reference repo (see `CLAUDE.md`).
- `sources/` is one server build. Other servers on the same protocol can differ. See "Porting" in
  `docs/agents/standards.md`.
- A bare server file name is under `sources/Server/GameServer/src/`. `ToClient.cpp` and `ToGameServer.cpp` are
  under `sources/Server/GateServer/src/`. `GroupServerAppServ.cpp` is under
  `sources/Server/GroupServer/src/`. `AccountServer2.cpp` is under `sources/Server/AccountServer/src/`.
- `PacketCmd_SC.cpp`, `NetProtocol.cpp`, `NetIF.cpp`, `ProCirculateCS.cpp`, `HMAttack.cpp` and `STMove.cpp` are the
  original client, under `sources/Client/src/`.
- `CompCommand.h`, `NetCommand.h`, `NetRetCode.h` and `ChaAttrType.h` are under
  `sources/Libraries/common/include/`.
- "Self" means `CCharacter::ReflectINFof` (`Character.cpp:484-497`): the packet goes to the owning player only.
- "Eyeshot" means `Entity::NotiChgToEyeshot(pk, bIncludeOwn)` (`Entity.cpp:176`): the packet goes to all players in
  view. The owner is included unless the call passes `false`.
- Positions on the wire are in centimetres. The bridge converts them to metres.

## How a packet reaches the client

1. `GateConnection.ReadPackets` (`src/Top.Legacy.Protocol/Transport/GateConnection.cs:209-250`) answers the ping
   opcodes 515, 537 and 5022 itself. It gives every other opcode to the listener.
2. `GateListener.OnReceived` (`src/Top.Client.LegacyBridge/GateListener.cs:36-55`) finds the decoder that
   `ServerConnection.Messages.cs` registered for the opcode.
   - No decoder: `Log.Info("packet N was received and has no decoder")`, and the packet is dropped.
   - The decoder returns `null`: `Log.Info("... carries nothing the client decodes")`, and the packet is dropped.
   - The reader throws (for example `InvalidDataException` at the packet end): `GateConnection.Deliver`
     (`GateConnection.cs:300-310`) logs `Log.Error` and drops the packet.
3. The event goes into a queue. `ServerConnection.Drain` gives it to the handlers that `Subscribe<TEvent>` added. An
   event with no handler is dropped with no log (`ServerConnection.cs:183`).

A packet goes out through `ServerConnection.Send` (`ServerConnection.cs:117-137`), which finds the encoder for the
command type. `GateConnection.WriteToGate` (`GateConnection.cs:330-352`) writes the opcode, the packet counter (the
gate WPE check, `ToClient.cpp:307-319`) and the payload.

## Most important findings

These are defects or missing behaviour with a visible effect. The files give the details.

| # | Finding | File |
|---|---|---|
| 1 | Only the first EnterMap is handled. Map switch, teleport, relive and the GM transform send EnterMap again while the player is in the world. No subscriber exists then, and the event is dropped. The client keeps the old map. | [view.md](view.md#entermap-516) |
| 2 | The client does not track the controlled character. Boat launch and dock do not move control. A login at sea shows the human and no boat. | [view.md](view.md#characterbeginsee-504) |
| 3 | A LOOK NotiAction with sync type CHANGE is shorter than our reader expects: the server writes 2 bytes for an unchanged slot, the reader reads 8. The fairy condition path depends on this packet. No capture confirms it yet. | [actions.md](actions.md#look-5) |
| 4 | Normal combat HP changes come in NotiAction SKILL_TAR, which we do not read. | [actions.md](actions.md#skill_tar-4) |
| 5 | The group server always refuses a security code change with 534, because of a stray `;` (`GroupServerAppServ.cpp:2058-2060`). | [session.md](session.md#changesecondpassword-347--reply-942) |
| 6 | When the client is in the wrong state, the gate answers Create and Change security code with a DeleteCharacterReply 936 code 2. We drop it, and the form waits and does not stop. | [session.md](session.md#deletecharacter-436--deletecharacterreply-936) |
| 7 | A database error can make the group server write a character list that our reader cannot read. There is no reply timeout, so the login screen or the world waits and does not stop. | [session.md](session.md#login-431--loginreply-931) |
| 8 | Kitbag readers find a boat item with a fixed id list `{ 3988 }`. The server uses item type 43. | [view.md](view.md#itemgrid-as-kitbag-content) |
| 9 | AttributeSync: we map 11 of 74 attribute ids and ignore the sync type. AP, TP, fame and all combat stats make no event. | [attributes.md](attributes.md) |
| 10 | Known issue, kept by user decision: the server's temporary bag has 32 slots. The client holds and shows 16, as the inventory spec says. An item in slot 16 or higher is dropped with a warning and the player cannot see it. | [items.md](items.md#temporarykitbagsync-35--reply-554) |
| 11 | `MakeItem` reports a full bag as item-failed code -4, a kitbag code (`Character.cpp:3346`). The original client shows "Unknown item usage failure code". We map -4 to bag full. | [actions.md](actions.md#item_failed-15) |
| 12 | An equip names no link: the server fills the first free fitting link, or with target -2 the second ring or sword link. The client rings only the equip slots the server can fill, so a bracelet always goes to bracelet 1 when both are empty or both full. | [actions.md](actions.md#item_use-11) |
| 13 | The server refuses every unequip with bag full while the bag has no free slot, with a notice that asks for 5 free slots (`CharacterCmd.cpp:1076-1080`). An unequip onto a filled bag slot does not swap: the item goes to the first free slot. | [actions.md](actions.md#item_unfix-10) |
| 14 | A take from the temporary bag reports every failure after the pop as bag full, also a trade or stall lock of the bag. Take all against a full bag gives one bag-full notice for each item it cannot place. | [actions.md](actions.md#kitbagtmp_drag-29) |
| 15 | A delete has no count: the server always destroys the whole stack. The confirmation names the count. | [actions.md](actions.md#item_delete-13) |
| 16 | The server lets a boat deed be thrown and deleted; the thrown deed takes the boat off the character, and a pick-up resets the boat to level 1 with 0 experience. Our item table has no flags for boat types, so the client disables both. | [actions.md](actions.md#item_throw-9) |
| 17 | The server has one 200 ms throttle for consumable use, shared by all players (`CharacterCmd.cpp:929-934`). A use inside it gets no reply: nothing is used and no failure comes. A script refusal comes as a system notice with the reason, which we do not show, then ITEM_FAILED UNUSE, which the toast shows as "Can't use this item". A teleport scroll is used up but the client stays on the old map (finding 1). | [actions.md](actions.md#item_use-11) |
| 18 | The group server tells the inviter why a party invite was refused only with a system message 517, which we do not decode. A refused invite looks the same as an open one, and a leave or a kick that the server ignores gets no reply at all. | [party.md](party.md#party-invite-request-6001--leave-6004--kick-6005) |
| 19 | The game server drops a chat line, a GM command too, that comes less than its `say_interval` (1 s in the shipped config) after the last line. It tells the sender only with a system message 517, which we do not decode, so a dropped line looks the same as a line that the server took. | [chat.md](chat.md#say-1) |
