# Party packets

See [README.md](README.md) for the file path conventions.

## Summary

- This file covers the party invite flow (5001, 5003, 6002, 6003), the invite request, leave and kick (6001, 6004,
  6005), the member list (5002), the member vitals (519) and the leader id (524).
- The invite readers and writers are pinned on bytes built by hand from the confirmed C++ source. The 5002, 519 and 524
  readers are pinned on bytes captured from the local server on 2026-09-27: `T1` (account one) and `Observer2` (account
  two) joined, left, were kicked and the leader left.
- The server can end an invite for five reasons. Our client reads and maps all five, but only busy and timeout are
  reachable from a direct player action; the other three are side effects of other state changes (see Gaps).

---

## Party invite (5001) / invite cancel (5003) / accept (6002) / refuse (6003)

**Types**

- Reader: `PartyInvite` (`Messages/Party/PartyInvite.cs`) through `PartyInviteDecoder`.
- Reader: `PartyInviteCancel` (`Messages/Party/PartyInviteCancel.cs`) through `PartyInviteCancelDecoder`.
- Writer: `PartyAccept` (`Messages/Party/PartyAccept.cs`) through `AcceptInviteEncoder`.
- Writer: `PartyRefuse` (`Messages/Party/PartyRefuse.cs`) through `DeclineInviteEncoder`.

**Payload**

- PartyInvite (5001): `string` inviter name, `u32` inviter character id, `u16` head icon. Confirmed against both the
  server write (`GroupServerAppTeam.cpp:78-81`, `WriteString` then `WriteLong` then `WriteShort`) and the original
  client's read (`NetPkChat.cpp:341-347`). The icon is read and then dropped — the design does not show it.
- PartyInviteCancel (5003): `u8` reason, `u32` inviter character id. Confirmed against every server write site (all
  `WriteChar(reason)` then `WriteLong(chaid)`) and the original client's read (`NetPkChat.cpp:349-354`). Reason codes,
  `sources/Libraries/common/include/NetCommand.h:777-781`:

  | Code | Name | Meaning |
  |---|---|---|
  | 1 | `MSG_TEAM_CANCLE_BUSY` | a newer invite evicted this one |
  | 2 | `MSG_TEAM_CANCLE_TIMEOUT` | the invite timed out |
  | 3 | `MSG_TEAM_CANCLE_OFFLINE` | the inviter is no longer reachable |
  | 4 | `MSG_TEAM_CANCLE_ISFULL` | the inviter's team is now full |
  | 5 | `MSG_TEAM_CANCLE_CANCEL` | the invite is stale (see Server, below) |

- PartyAccept (6002) / PartyRefuse (6003): `u32` inviter character id, nothing else. Confirmed against the client
  send (`NetPkChat.cpp:102-118`, `CS_Team_Confirm`/`CS_Team_Refuse`, a single `WriteLong`) and the server handlers
  (`GroupServerAppTeam.cpp:274-276` and `:86-88`, a single `ReadLong`).

**Server**

- CMD_PC_TEAM_INVITE (5001) has exactly one send site in the whole server tree: `GroupServerAppTeam.cpp:78-81`, inside
  `CP_TEAM_INVITE`.
- CMD_PC_TEAM_CANCEL (5003) has six send sites, all in `GroupServerAppTeam.cpp`, all to the invited player:
  - `:72-75`, reason busy: `Invitation::BeginInvited` (`Invitation.cpp:17-39`) evicts the oldest pending invite when
    the invitee already holds `InvitedMax` pending invites (`Invitation.cpp:24-28`) — a per-invitee cap, not a global
    one. The handler sends this and then the new invite's own 5001 back to back (`:77-82`), so a client can see a
    busy cancel immediately followed by a fresh invite in one round trip.
  - `:578-581`, reason offline: the periodic sweep `Player::TeamInvitedCheck` (driven by `Invitation::_Run`,
    `Invitation.cpp:84-92`) finds the inviter's stored character id no longer matches the inviter's current one (the
    inviter switched character).
  - `:586-589`, reason party full: the inviter's team has reached `MemberMax` by the time the sweep runs (`:583`).
  - `:594-598`, reason cancelled: the inviter still has a team, but is no longer its leader (`:591`).
  - `:607-611`, reason cancelled: the invitee already has a team of more than one member by the time the sweep runs
    (`:599`) — this invite is stale.
  - `:620-624`, reason timeout: `GetCurrentTick() - invited->m_tick >= const_team.PendTimeOut` (`:612`).
  - No explicit "inviter withdraws the invite" opcode or code path was found. Both cancelled cases are always a side
    effect of other state changes, never a direct player action, in this build.
- CMD_CP_TEAM_ACCEPT (6002) is handled at `GroupServerAppServ.cpp:481`, which forwards to `GroupServerAppTeam.cpp:274-276`.
- CMD_CP_TEAM_REFUSE (6003) is handled at `GroupServerAppServ.cpp:484`, which forwards to `GroupServerAppTeam.cpp:86-88`.
- Server settings, group server `[team]` config, read in `GroupServerAppInit.cpp:80-82`:
  - `InvitedTime` → `const_team.PendTimeOut` (milliseconds), enforced at `GroupServerAppTeam.cpp:612`.
  - `MaxInvitedNum` → `const_team.InvitedMax`, enforced at `Invitation.cpp:24`.
  - `MaxPersonNum` → `const_team.MemberMax`, used at `GroupServerAppTeam.cpp:22` and `:583`.
  - This build has no compiled-in fallback for these; the reference config's values are 30 s / 5 / 5.

**Client**

- **Receive an invite:** `PartyInviteDecoder` returns `PartyInvitedEvent(InviterId, InviterName)`. `WorldScene`
  calls `PartyInviteQueue.AddInvite`, timestamped with `DateTime.UtcNow` at the moment the event is handled, and shows
  the queue on the HUD.
- **Receive an invite end:** `PartyInviteCancelDecoder` returns
  `PartyInviteEndedEvent(InviterId, (PartyInviteEndReason)Reason)`. `WorldScene` calls
  `PartyInviteQueue.RemoveInvite`. The reason travels on the event but nothing shows it yet (`// TODO`: the System
  line, once chat exists).
- **Send an answer:** the invite card's Accept or Decline button raises a signal with the inviter id, relayed through
  `HudScreen` to `WorldScene.AcceptInvite` / `DeclineInvite`. Both send the command and call
  `PartyInviteQueue.RemoveInvite` at once, before any server reply arrives.
- **The original client** (`TeamInviteFormMgr.cpp`): one window per inviter, keyed by inviter id.
  `AddInviteForm` (`:28-72`) refuses to open a second window for an id already open (`:30`) — mostly redundant, since
  the server itself already refuses a second pending invite from the same inviter and sends no second 5001
  (`GroupServerAppTeam.cpp:49-56`). No countdown timer exists anywhere in the form. Both buttons remove the window at
  once, before any server reply (`_MainMousePlayerGroupEvent`, `:103-115`) — our client matches this. No Esc binding
  was found on the form.

**Gaps**

- The bridge test builds bytes for all five cancel reasons by hand from the confirmed field layout above. Only busy
  and timeout are reachable from a direct player action in this build; a live capture of offline, party-full and
  cancelled would need contrived server states (see Server, above) and was not attempted.
- The client hardcodes the 30 s invite duration for its own countdown display (`PartyInviteQueue.InviteSeconds`,
  `// TODO`: a server-declared value). The server's own timeout (`InvitedTime`) is authoritative; a mismatch only
  changes how early or late the display reaches zero relative to the real end event, never whether the invite ends.

---

## Party invite request (6001) / leave (6004) / kick (6005)

**Types**

- Writer: `PartyInviteRequest` (`Messages/Party/PartyInviteRequest.cs`) through `InviteToPartyEncoder`.
- Writer: `PartyLeave` (`Messages/Party/PartyLeave.cs`) through `LeavePartyEncoder`.
- Writer: `PartyKick` (`Messages/Party/PartyKick.cs`) through `KickPartyMemberEncoder`.

**Payload**

- PartyInviteRequest (6001): `string` name of the invited character, nothing else. Confirmed against the client send
  (`NetPkChat.cpp:92-100`, `CS_Team_Invite`, a single `WriteString`) and the server handler
  (`GroupServerAppTeam.cpp:30`, a single `ReadString`).
- PartyLeave (6004): no body. Confirmed against the client send (`NetPkChat.cpp:130-137`, `CS_Team_Leave`) and the
  server handler (`GroupServerAppTeam.cpp:367-370`, which reads nothing).
- PartyKick (6005): `u32` member id, nothing else. Confirmed against the client send (`NetPkChat.cpp:120-128`,
  `CS_Team_Kick`, a single `WriteLong`) and the server handler (`GroupServerAppTeam.cpp:466`, a single `ReadLong`).
  The member id is the character id of the 5002 list, which equals the world id of the member's main character (see
  Party refresh). It is not the character type id of the view packets.

**Server** (all in `GroupServer/src/GroupServerAppTeam.cpp`)

- CMD_CP_TEAM_INVITE (6001) is handled at `GroupServerAppServ.cpp:478`, which forwards to `CP_TEAM_INVITE` (`:8`).
  The server checks in this order. Each refusal goes to the inviter as a system message (`Player::SendSysInfo`,
  `GroupServer/src/Player.cpp:275-282`, opcode 517 `CMD_MC_SYSINFO`):
  - `:18-21`: the inviter is in a party and does not lead it.
  - `:22-25`: the inviter's party already has `MemberMax` members.
  - `:31-34`: the name is longer than 16 bytes. No message: the packet is ignored. An empty name goes on to the next
    check.
  - `:36-42`: no player in the world has that character name, or it is the inviter.
  - `:43-48`: the invited player is already in a party.
  - `:49-55`: the inviter already has an open invite to that player.
  - `:56-61`: the invited player does not take requests (`Player::CanReceiveRequests`).
  - Else `:62-83`: the server opens the invite and sends 5001 to the invited player. When that player already holds
    `InvitedMax` invites, the oldest ends first with a busy 5003 (see Party invite).
- CMD_CP_TEAM_LEAVE (6004) is handled at `GroupServerAppServ.cpp:487`, which forwards to `CP_TEAM_LEAVE` (`:367`).
  - `:370`: a player in no party is ignored.
  - Else `:378`: a 5002 of kind leave goes to all members, the leaver included, and `CMD_PM_TEAM` goes to the game
    server, which sends the 524 leader notices. With two members the leader leaves too and the party ends
    (`:445-448`).
  - The group server also calls this handler itself when a player switches character or logs out
    (`GroupServer/src/Player.cpp:94`).
- CMD_CP_TEAM_KICK (6005) is handled at `GroupServerAppServ.cpp:490`, which forwards to `CP_TEAM_KICK` (`:460`).
  - `:463`, `:465`: a player in no party, or a player who does not lead the party, is ignored. No message.
  - `:467-470`: an id equal to the leader's own id is ignored.
  - `:471-480`: an id that is not a member of the party is only logged on the server.
  - Else `:487`: a 5002 of kind kick goes to all members, the kicked member included, and `CMD_PM_TEAM` goes to the
    game server, as for a leave. The same end rule applies.

**Client**

- **Invite, leave, kick:** `InviteToPartyCommand(CharacterName)`, `LeavePartyCommand` and
  `KickPartyMemberCommand(MemberId)`. Only `Top.Client.Cli` sends them now: the `invite <name>`, `leave` and
  `kick <name>` steps. `kick` takes the member id from the last `PartyChangedEvent` member list. The game client has no
  UI action that sends them yet.
- **The original client** sends 6001 from the player menu of a character in view (`UIStartForm.cpp:1365-1377`) and
  from the chat member menu (`UIChat.cpp:1148`, `:1203`, `:1256`). It sends 6004 from the player menu
  (`UIStartForm.cpp:1381-1385`), from the chat member menu (`UIChat.cpp:1112`) and from the menu of the player's own
  frame (`CStartMgr::_OnSelfMenu`, `UIStartForm.cpp:1911-1937`), which is disabled on the arena maps `starena1` to
  `starena3`. It sends 6005 from the chat member menu (`UIChat.cpp:1120`), with the
  id of the selected member. All three senders return with no packet when `CStartMgr::IsCanTeamAndInfo`
  (`UIStartForm.cpp:1950-1954`) is false; that flag is the `CanTeam` field of the map entry packet
  (`NetProtocol.cpp:1229`).

**Gaps**

- The 517 system message that tells the inviter why an invite was refused has no decoder. A refused invite looks the
  same as an invite that is open.
- Our senders do not check the `CanTeam` map flag (it is read and dropped, see [view.md](view.md)), the level 8 rule of
  the player menu, or the 16-byte name limit. The server does not check the map flag or the level either.
- A leave or a kick that the server ignores sends nothing back. `Top.Client.Cli` checks the party itself before it
  sends: it refuses a `leave` or a `kick` with no party, and a `kick` when another member leads the party.

---

## Party refresh (5002)

**Type**

- S→C group, opcode 5002 (`CMD_PC_TEAM_REFRESH`). Reader: `PartyRefresh` (`Messages/Party/PartyRefresh.cs`), with
  one `PartyMemberInfo` (`Messages/Party/PartyMemberInfo.cs`) for each member, through `PartyRefreshDecoder`.

**Payload**

- `u8` kind, `u8` count, then count times: `u32` character id, `string` name, `string` motto, `u16` icon. Confirmed
  against every server write (below) and the original client's read (`NetPkChat.cpp:356-375`).
- Kinds (`NetCommand.h:565-572`): 1 `TEAM_MSG_ADD` (join), 2 `TEAM_MSG_LEAVE` (leave), 6 `TEAM_MSG_KICK` (kick). Kinds
  3, 4 and 5 are game-server messages; no 5002 send site writes them.
- The list is in join order, so the leader is first. On a leave or a kick the list holds the remaining members, then
  the member that left, and the count is the count before the removal. The captured bytes show both orders: a member
  leaving (`[T1, Observer2]`) and the leader leaving (`[Observer2, T1]`).
- The character id equals the world id of the character's main character (captured: `T1` has world id 12 and list id
  12).

**Server** (all in `GroupServer/src/GroupServerAppTeam.cpp`, each sent to every member in the list)

- `:193`, kind join: `MP_TEAM_CREATE`, a party formed at a game server's request (`CMD_MP_TEAM_CREATE`, sent by the
  volunteer list answer, `CharacterPrl.cpp:1399`).
- `:293`, kind join: `CP_TEAM_ACCEPT`, a player accepts an invite (6002).
- `:378`, kind leave: `CP_TEAM_LEAVE`, a player leaves (6004). The group server also calls it when a player switches
  character or logs out (`GroupServer/src/Player.cpp:94`). With a count of 2 the leader leaves too, and the party ends
  (`:445-448`).
- `:487`, kind kick: `CP_TEAM_KICK`, the leader kicks a member (6005). The same end rule applies.
- After a map change the group server sends no 5002 (`MP_SWITCH`, `:627`, sends only `CMD_PM_TEAM` update to the game
  servers).

**Client**

- `PartyRefreshDecoder` returns `PartyChangedEvent(Kind, ChangedMemberName, Members)`:
  - join: `Joined`, the last entry's name (the member that joined), the full list;
  - leave: `Left`, the last entry's name, the list without its last entry;
  - kick: `Kicked`, the last entry's name, the list without its last entry;
  - fewer than two members after the change: an empty list (the party ended).
  - `// TODO` in the decoder: the chat message for the join, the leave and the kick.
- `WorldScene.ChangeParty` passes it to `PartyRoster.ChangeMembers`: a list without the player is no party; otherwise
  the members are the list without the player, in list order, the first entry leads, and a member that stays keeps its
  vitals. `HudScreen.ShowParty` then shows one `PartyRow` for each member and the gold ring on the player frame when
  the player leads.

**Gaps**

- The motto and the icon are read and dropped.
- A refresh with another kind (3, 4, 5) or with no members gives no event and is logged at Info.
- A leave or a kick with three or more members is covered by a test built from the captured layout, not by a capture:
  the local stack has two accounts.

---

## Party member vitals (519)

**Type**

- S→C game, opcode 519 (`CMD_MC_TEAM`). Reader: `PartyMemberVitals` (`Messages/Party/PartyMemberVitals.cs`) through
  `PartyMemberVitalsDecoder`.

**Payload**

- `u32` member id, `i32` HP, `i32` max HP, `i32` SP, `i32` max SP, `i32` level, then the look block (`LookInfo`, the
  same block as in the view packets; see [view.md](view.md)). Confirmed against the server write
  (`Player.cpp:419-428`, `WriteLookData(wpk, LOOK_TEAM)`) and the original client's read (`PacketCmd_SC.cpp:1273-1299`).
- `LOOK_TEAM` writes the full look with the item extra data (`CharacterPacket.cpp:418-484`); only the blind-chaos
  branch (`:343`) drops it, and that branch is for `LOOK_OTHER` only.
- The member id is `pMainCha->GetID()`, the main character's world id, which is the character id of the 5002 list.

**Server**

- One send site: `CPlayer::NoticeTeamMemberData` (`Player.cpp:408-431`), called by `CCharacter::OnTeamNotice`
  (`CharacterRun.cpp:285-292`) on the character's 1 s team timer (`Character.cpp:136`, `CharacterRun.cpp:147`). It goes
  to every other member (`SENDTOCLIENT2`), on any map and any game server.
- HP and SP come from the controlled character (`GetCtrlCha`), so a sailing member sends the ship's HP and SP. The level
  and the look come from the main character.

**Client**

- `PartyMemberVitalsDecoder` returns `PartyMemberVitalsChangedEvent(MemberId, PartyMemberVitals(Level, Health,
  MaxHealth, Mana, MaxMana, TypeId, Look))`. The look goes through `CharacterStateMapper.ToCharacterLook`, so it
  carries the fairy record too.
- `WorldScene.ChangePartyMemberVitals` passes it to `PartyRoster.ChangeVitals`, which stores it on the member with that
  id and ignores an unknown id. The HUD row shows the level, the HP numbers, the HP bar and the SP bar, and the dead
  style at 0 HP.

**Gaps**

- The look is stored but not shown yet (ticket 09, party portraits).
- A boat look maps to no character look (null). The main character is never a boat, so the server does not send one.
- A dead member was not captured; the dead style is checked with made-up vitals in a probe.

---

## Party leader notice (524)

**Type**

- S→C game, opcode 524 (`CMD_MC_TLEADER_ID`). Reader: `PartyLeaderNotice` (`Messages/Party/PartyLeaderNotice.cs`)
  through `PartyLeaderNoticeDecoder`.

**Payload**

- `u32` world id, `u32` leader id. Confirmed against the server write (`Player.cpp:440-443`) and the original client's
  read (`PacketCmd_SC.cpp:1304-1316`). A leader id of 0 means no party.

**Server**

- One send site: `CPlayer::NoticeTeamLeaderID` (`Player.cpp:433-451`). It goes to every other member
  (`SENDTOCLIENT2`) and to every player in view of the character (`NotiChgToEyeshot`), owner included. A client in the
  party that also sees the character gets it twice (captured).
- Callers:
  - `CGameApp::ProcessTeamMsg` (`GameAppNet.cpp:1166`), for each game-server team message: join, leave and the update
    after a map change. The leader id is the first member's database character id (`:1165`).
  - `CPlayer::ClearTeamMember` (`Player.cpp:390-396`), with leader 0, when a member leaves or the party ends.
- The world id is the controlled character's (`GetCtrlCha()->GetID()`), which is the ship when the member sails.

**Client**

- `PartyLeaderNoticeDecoder` returns `CharacterTeamLeaderChangedEvent(WorldId, TeamLeaderId)`.
- `WorldScene.ChangeTeamLeader` merges it into the character state's `TeamLeaderId` of that character, if it is in
  view (`Character.ChangeTeamLeader`, which also refreshes its nameplate). When it is the player's own character, every
  nameplate takes the new player state (`CharacterRegistry.ChangePlayer`), so the team rules show members in view by
  name and HP.
- The party rows do not use it. The HUD leader is the first entry of the 5002 list, as in the original.

**Gaps**

- A notice for a character not in view is dropped; the character state gets its leader id from the next begin-see.
- The nameplate team rules compare the leader id with the world id; a sailing member sends the ship's world id (the
  existing `// TODO` in `NameplateRule`).
