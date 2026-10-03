# Action packets

See [README.md](README.md) for the file path conventions.

## NotiAction (508)

`CMD_MC_NOTIACTION` = `CMD_MC_BASE + 8`. Server to client.

**Header** (every writer, for example `MoveAble.cpp:911-914`):

| Field | Type | Notes |
|---|---|---|
| worldId | u32 | |
| packetId | u32 | The id from the last BeginAction of the character (`CharacterPrl.cpp:1991`). SKILL_TAR uses `SFire.ulPacketID`. |
| actionType | u8 | `EActionType`, see the table below |
| body | — | Depends on actionType |

**Original client reader:** `SC_CharacterAction` (`PacketCmd_SC.cpp:760-1201`), dispatched from `NetIF.cpp:68`.

**Our reader:** `ActionNotice.Read` (`src/Top.Legacy.Protocol/Messages/Play/ActionNotice.cs:31-45`) reads the header,
then looks up the body reader for the action type (`ActionNotice.cs:9-17`). Each action type has its own `ActionNotice`
subclass in `Messages/Play/`. Body readers are registered only for MOVE (1, `MoveNotice`), LOOK (5, `LookNotice`),
KITBAG (6, `KitbagNotice`), ITEM_FAILED (15, `ItemFailedNotice`) and KITBAGTMP (28, `TemporaryKitbagNotice`).
For all other types the body is not read and the result is an `UnreadActionNotice`, `ActionNoticeDecoder.Decode`
returns `null`, and the packet is dropped with a log (`GateListener.cs:47-51`).

### All action types

`EActionType`, `CompCommand.h:18-60`.

| Value | Name | Use | Sent as NotiAction | We read the body |
|---|---|---|---|---|
| 0 | NONE | – | no | – |
| 1 | MOVE | C→S and S→C | yes | **yes** |
| 2 | SKILL | C→S only | no | – |
| 3 | SKILL_SRC | S→C | yes | no |
| 4 | SKILL_TAR | S→C | yes | no |
| 5 | LOOK | S→C (the C→S handler is commented out, `CharacterPrl.cpp:2443-2460`) | yes | **yes** |
| 6 | KITBAG | S→C | yes | **yes** |
| 7 | SKILLBAG | – | no. The skill bag goes in `CMD_MC_SYNSKILLBAG` (`Character.cpp:5429-5437`) | – |
| 8 | ITEM_PICK | C→S | no | – |
| 9 | [ITEM_THROW](#item_throw-9) | C→S | no | – |
| 10 | ITEM_UNFIX | C→S | no | – |
| 11 | ITEM_USE | C→S | no | – |
| 12 | ITEM_POS | C→S | no | – |
| 13 | [ITEM_DELETE](#item_delete-13) | C→S | no | – |
| 14 | ITEM_INFO | C→S. The original client has an empty S→C case (`PacketCmd_SC.cpp:1094`) | no writer found | – |
| 15 | ITEM_FAILED | S→C | yes | **yes** |
| 16 | LEAN | C→S, S→C relay | yes | no |
| 17 | CHANGE_CHA | S→C | yes | no |
| 18 | EVENT | C→S | no | – |
| 19 | FACE | C→S, S→C relay | yes | no |
| 20 | STOP_STATE | C→S | no | – |
| 21 | SKILL_POSE | C→S, S→C relay | yes | no |
| 22 | PK_CTRL | C→S and S→C | yes | no |
| 23 | LOOK_ENERGY | S→C | yes | no |
| 24 | TEMP | C→S, S→C relay | yes | no |
| 25 | SHORTCUT | C→S | the writer `SynShortcut` exists (`Character.cpp:5308`) with **no call site** | – |
| 26 | BANK | C→S and S→C | yes | no |
| 27 | CLOSE_BANK | C→S | no | – |
| 28 | KITBAGTMP | S→C | yes | **yes** |
| 29 | [KITBAGTMP_DRAG](#kitbagtmp_drag-29) | C→S | no | – |
| 30 | GUILDBANK | S→C | yes | no |
| 31 | REQUESTGUILDBANK | C→S | no | – |
| 32 | REQUESTGUILDLOGS | C→S and S→C reply | yes | no |
| 33 | UPDATEGUILDLOGS | C→S and S→C reply | yes | no |

Every NotiAction writer in the server is one of the `WRITE_CHAR(.., enumACTION_*)` sites in the sections below.

### MOVE (1)

**Payload.** Server: `MoveAble.cpp:915-918`, `:950-953`. Original client: `PacketCmd_SC.cpp:776-786`.

| Field | Type | Notes |
|---|---|---|
| sState | i16 | Bit flags `EMoveState` (`CompCommand.h:176-185`): ON 0, ARRIVE 0x01, BLOCK 0x02, CANCEL 0x04, INRANGE 0x08, NOTARGET 0x10, CANTMOVE 0x20 |
| sStopState | i16 | Only if sState is not 0. `EExistState` (`CompCommand.h:202-216`). For a player it is always `enumEXISTS_WAITING` = 1, the default of `Cmd_BeginMove` (`Character.h:168`). An AI move can pass another value (`Action.cpp:303-306`). |
| points | sequence | `u16` byte length, then `{i32 x, i32 y}` in centimetres. Always at least 2 points (`MoveAble.cpp:787-788`). |

**Writers:** `NotiMovToEyeshot` (`MoveAble.cpp:908-941`) sends to the eyeshot with the owner. `NotiSelfMov`
(`MoveAble.cpp:943-976`) sends to self only, so a monster gets nothing from it.

**Situations.** `AttemptMove` (`MoveAble.cpp:683-806`) computes the state.

| Site | Situation | sState | To |
|---|---|---|---|
| `MoveAble.cpp:471` (`BeginMove`) | A move to a target starts, and the target does not exist | NOTARGET (plus CANCEL if a stop was queued, `:456-457`) | self |
| `:484` | A move to a target starts, and the actor is already in range. Both points are the current position (`:483`) | INRANGE | self |
| `:494` | Move is forbidden (`enumACTCONTROL_MOVE` off) | CANCEL | self |
| `:503` | `ATTR_MSPD == 0` | CANTMOVE | self |
| `:516` | First step of a new move. The step covers the heartbeat (300 ms for a player, 600 ms otherwise, `:374-380`) plus the ping (`:509-512`) | ON, or ARRIVE / BLOCK / INRANGE if the first step ends the move | eyeshot |
| `:532` | The first step moved nothing (`ulElapse == 0`) | result of `AttemptMove` | self |
| `:588` (`OnMove`) | A stop was requested (`chRequestState == 1`): EndAction, a new move queued by `Cmd_BeginMove`, or move forbidden during the move (`:557-562`). The server first walks the remaining pre-moved distance | CANCEL (plus what `AttemptMove` adds) | eyeshot |
| `:609` | The move target disappeared during the move | NOTARGET | eyeshot |
| `:653` | Each heartbeat step during the move | ON, or the end state: ARRIVE (last point), BLOCK (obstacle, `:744-747`), INRANGE (`:737-740`) | eyeshot |

**Our handling**

- `MoveInfo.Read` (`Messages/Play/MoveInfo.cs:30-46`) reads the state, the stop state if the state is not 0, and the
  points.
- `ActionNoticeDecoder.DecodeMove` (`ActionNoticeDecoder.cs:46-58`) makes
  `CharacterMovedEvent(worldId, points, (MoveStates)state, stopState)`. `MoveStates`
  (`src/Top.Client.Messages/Play/Data/MoveStates.cs`) matches `EMoveState`. Zero points give `null`. The server never
  sends zero points.
- The subscriber is `WorldScene.FollowCharacter` (`src/Top.Client/World/WorldScene.cs:86, 267-282`).
  - **Own character:** `PredictedMotion.Correct` (`PredictedMotion.cs:147-174`).
    - It keeps the last point as the server position and resets the 3 s answer timeout.
    - BLOCK or CANTMOVE: `AbandonMove`, which snaps to the server position.
    - Otherwise it snaps back if the drift is more than 20 m. On any state that is not 0 it calls `AcceptServerEnd`
      (`:285-299`): a pending move is sent, and on CANCEL it walks to the server position.
  - **Other characters:** `CharacterRegistry.Follow`, then `FollowedMotion.Follow` (`FollowedMotion.cs:27-42`). It
    snaps if the gap is more than the snap distance. Otherwise it appends the points. It sets
    `IsPathOpen = (state == None)`.
- `StopState` is in the event, but no subscriber reads it.

### SKILL_SRC (3)

**Payload.** Server: `FightAble.cpp:592-667`, `:712-781`. Original client: `PacketCmd_SC.cpp:850-904`.

| Field | Type | Notes |
|---|---|---|
| fightId | u8 | |
| angle | i16 | |
| sState | i16 | `EFightState` (`CompCommand.h:187-200`) |
| stopState | i16 | Only if sState is not ON. `enumEXISTS_WITHERING` if the DIE bit is set, else `m_SFightInit.sStopState` |
| skillId | i32 | |
| skillTime | i32 | Attack speed or resume time |
| tarType | u8 | 1: `i32 targetId, i32 x, i32 y`. 2: `i32 x, i32 y` |
| execTime | i16 | |
| attrCount | i16 | then `{u8 id, i32 value}` × attrCount |
| stateCount | u8 | then `{u8 id, u8 lv}` × stateCount |

**Writers:** `NotiSkillSrcToEyeshot` (eyeshot) and `NotiSkillSrcToSelf` (self).

**Situations**

| Situation | sState | To |
|---|---|---|
| Not a fight entity (`FightAble.cpp:121-122`) | OFF | self |
| Skill that is not plain in `BeginFight` (`:199-200`) | OFF | self |
| Target out of range at begin (`:217-218`) | TARGET_OUT | self |
| Target does not exist at begin (`:173-175`) | TARGET_NO | eyeshot |
| Skill no longer allowed during the fight (`:242-243`) | CANCEL | eyeshot |
| The client asked for a stop: EndAction, then `DesireFightEnd` (`:255-257`) | CANCEL | eyeshot |
| Target gone during the fight (`:267-269`) | TARGET_NO | eyeshot |
| Target left range (`:283-284`) | TARGET_OUT | eyeshot |
| Not enough SP or energy (`:1296-1297`, `:1324-1325`) | adds NO_EXPEND | eyeshot |
| The `szUse` script set NO_EXPEND (`:1357-1359`) | NO_EXPEND | eyeshot |
| Each successful cast on a point (`:1431`, `:1446`) | ON, or STOP for a one-shot skill | eyeshot |
| Each successful cast on an entity (`:1508`) | ON or STOP | eyeshot |
| Entity target missing (`:1488-1489`) | TARGET_NO | eyeshot |
| Target not legal (`:1498-1499`) | TARGET_IMMUNE | eyeshot |
| Target died from a range skill (`:1410-1411`) or a single-target skill (`:1552-1553`) | TARGET_DIE | eyeshot |

**Our client:** not read, dropped.

### SKILL_TAR (4)

**Payload.** Server: `FightAble.cpp:828-873`.

| Field | Type | Notes |
|---|---|---|
| fightId | u8 | |
| targetFightState | i16 | |
| crit | u8 | |
| miss | u8 | |
| beatBack | u8 | If 1: `{i32 x, i32 y}` follows |
| srcId | i32 | |
| srcX, srcY | i32, i32 | |
| skillId | i32 | |
| tarInfo1, tarInfo2 | i32, i32 | |
| execTime | i16 | |
| target attributes | `WriteAttr` (`:1192-1209`) | `u8 synType` (3, ATTACK), `i16 n`, `{u8 id, i32 v}` × n |
| hasStates | u8 | If 1: `WriteSkillState` (`:1170-1190`): `u32 tick`, `u8 n`, `{u8 id, u8 lv, u32 duration, u32 start}` × n |
| hasSrc | u8 | If 1: `i16 srcFightState`, `WriteAttr` for the source, `u8 hasSrcStates`, `[WriteSkillState]` |

The original client reads up to the source attributes and stops there (`PacketCmd_SC.cpp:926-1034`, early `break` at
`:1011-1012`).

**Situation:** `SkillTarEffect` (`FightAble.cpp:355-470`, send at `:452`) runs once for each target hit. Callers: the
single-target path (`:1533`) and `RangeEffect` (`:1390`), which serves range skills and delayed or projectile skills
through `g_CTimeSkillMgr`.

**To:** the eyeshot of the target.

**Normal combat HP changes reach the client here, not in AttributeSync.** See [attributes.md](attributes.md).

**Our client:** not read, dropped.

### LOOK (5)

**Payload.** Server: `WriteLookData` (`CharacterPacket.cpp:302-486`). Original client: `ReadChaLookPacket`
(`PacketCmd_SC.cpp:3485-3569`). This is the [LookInfo](view.md#lookinfo) block.

- `u8 synType`: `ESynLookType` (`CompCommand.h:109-113`). SWITCH 0, CHANGE 1.
- `i16 typeId`
- `u8 isBoat`
  - **Boat (1):** `i16` posId, boatId, header, body, engine, cannon, equipment. Only for a boat that a player controls
    (`:328-338`).
  - **Human (0):** `i16 hairId`, then 34 links (`enumEQUIP_NUM`). Each link starts with `i16 id, u32 dbId, i16 needLv`.
    If id is 0, nothing more follows.
    - SWITCH then has: `i16` num, endure[0], endure[1], energy[0], energy[1]; `u8` forgeLv, valid, tradable;
      `i32` expiration; `u8 hasDb`. If hasDb is 1: `i32 forge, i32 instId, u8 hasAttr`, and `{i16 id, i16 v}` × 5 if
      hasAttr is 1.
    - CHANGE then has: `i16 endure[0], i16 energy[0], u8 valid, u8 tradable, i32 expiration`.

**Defect in CHANGE: the server writes fewer bytes than both clients read.**

- In CHANGE mode the server writes **only `i16 0`** (2 bytes) for a slot that did not change
  (`CharacterPacket.cpp:429-436`). It does the same for a null look (`:313-317`) and in the blind-chaos branch
  (`:354-361`).
- The original client reads `id + dbId + needLv` (8 bytes) for every slot (`PacketCmd_SC.cpp:3515-3517`). Our
  `ItemGrid.ReadLink` (`ItemGrid.cs:114-121`) does the same.
- Most senders call `SetLookChangeFlag()` first (default `false`, `Character.h:619`; `Character.cpp:887-897`), so most
  slots are unchanged. A CHANGE packet with any unchanged slot then reads wrong in both clients. In our client the
  read most likely throws at the packet end, and the packet is dropped (`GateConnection.cs:306-309`).
- Our tests build CHANGE packets with our own writer, which writes 8 bytes for each empty slot
  (`ServerConnectionTests.cs:847-880`). The tests do not show the defect.
- **Not confirmed on the wire.** Capture a real CHANGE packet before a fix.

**Situations**

- **SWITCH (0), to the eyeshot with the owner.** `SynLook(enumSYN_LOOK_SWITCH)` (`Character.cpp:5320-5337`). It sends
  only if at least one slot has its change flag set (`:5322`).
  - Equip an item: `Cmd_UseEquipItem` (`CharacterCmd.cpp:906`)
  - Unequip: `Cmd_UnfixItem` (`CharacterCmd.cpp:1233`)
  - `Cmd_RemoveItem` when equipment changed (`CharacterCmd.cpp:2258`)
  - Item repair accepted: `Cmd_ItemRepairAnswer` (`CharacterCmd.cpp:2905`)
  - Hair change (`CharacterPrl.cpp:2709`)
  - Lua `SynLook` (`Expand.h:26-31`) and Lua `EquipItem` (`Expand.h:58-72`), registered at `Expand.h:7841-7842`
- **SWITCH, blind-chaos variant.** When `g_Config.m_bBlindChaos` is on, the character is a player and the map is the
  chaos map, the five C++ sites use `SynLook(LOOK_SELF, true)` (`Character.cpp:5340-5363`): `CharacterCmd.cpp:904,
  1231, 2256, 2903` and `CharacterPrl.cpp:2707`. Two packets go out:
  - LOOK_SELF to self
  - LOOK_OTHER to the eyeshot **without** the owner. Equipment ids are replaced by chaos gear, and hasDb is 0.

  The original client ignores LOOK for other characters on the PK-silver map (`PacketCmd_SC.cpp:1073`).
- **CHANGE (1), to self only.** Durability, energy and valid-flag refresh.
  - `SetEquipValid` with sync (`Character.cpp:811`)
  - `AddSkillState` with notice: `Character.cpp:4595` for the target, `:4601` for the source
  - `SkillTarEffect`: `FightAble.cpp:454` for the target main character, `:462` and `:467` for the source
  - `SkillExpend`, on every player cast (`FightAble.cpp:1365`)
  - `OnSkillState` ticks: `FightAble.cpp:2484` for the state source, `:2507` for self
  - `RemoveOtherSkillState`, when boarding or leaving a boat (`FightAble.cpp:2531`)
- **Boat branch:** it runs only when `SynLook` runs on a player boat. No C++ site that does this was confirmed.

**Our handling**

`LookInfo.Read` (`LookInfo.cs:73-97`) reads the layout above (`FullSync` = 0, `ItemDataSync` = 1).
`ActionNoticeDecoder.DecodeLook` (`ActionNoticeDecoder.cs:29-44`) picks one of three paths:

1. **Boat:** `ShipLookChangedEvent` (boatId, header, engine; `CharacterStateMapper.cs:74-82`). Subscriber
   `WorldScene.ChangeShipLook` (`WorldScene.cs:92, 361-367`), then `Character.ChangeShipLook` (`Character.cs:122-132`),
   which rebuilds the ship parts. If the character is not a boat model, it logs and ignores the change.
2. **CHANGE (`IsItemDataSync`):** `FairyMapper.ToFairyConditionChangedEvent` (`FairyMapper.cs:42-54`). It uses only
   link 16 (`enumEQUIP_FAIRY`), and only when that link is valid: stamina = `endure[0] / 50`, growth = `energy[0]`.
   All other slots are dropped. With no valid fairy it returns `null`. Subscriber `WorldScene.ChangeFairyCondition`
   (`WorldScene.cs:91, 346-359`), then `Character.ChangeFairyCondition` (`Character.cs:91-101`), which updates the
   fairy only if the item id matches. For the own character it also refreshes the HUD.
3. **SWITCH:** `CharacterLookChangedEvent(hairId, 34 equipment ids, fairy)` (`CharacterStateMapper.cs:52-72`).
   Subscriber `WorldScene.ChangeLook` (`WorldScene.cs:90, 331-344`), then `Character.ChangeLook` (`Character.cs:84-89`)
   and `CharacterBody.ChangeLook` (`CharacterBody.cs:157-179`), which dresses the model again and reloads clips if the
   stance changed. For the own character it also refreshes the HUD.

   The same event carries `Equipment`: the 34 links as `BagItem`s, from `EquipmentMapper.ToEquipment`
   (`EquipmentMapper.cs:10-27`). A link with id 9999 (`enumEQUIP_BOTH_HAND`, the other hand of a two-handed item) or
   9998 (`enumEQUIP_TOTEM`, a link a body item also takes) is no item and maps to `null`. The original equip form clears
   a 9999 link (`UIEquipForm.cpp:1055-1078`). For 9998 it finds no item record, logs and keeps the old cell content
   (`:1064-1068`); showing it empty is our choice. `WorldScene.ChangeLook` (`WorldScene.cs:369-372`) first gives the
   event to `CharacterInventory.ChangeEquipment` (`CharacterInventory.cs:55-61`). It replaces all 34 equipment slots
   when the world id is the player's, and ignores other characters. Then the Inventory window shows the change. This is
   the original's `g_stUIEquip.UpdataEquip`, which runs for the main player on SWITCH only
   (`NetProtocol.cpp:1468-1476`).

   Confirmed live on 2026-09-28: the use action (11) of the Newbie Vest from bag slot 2 gave a SWITCH look for `T1`,
   and the Inventory's body slot changed from 4308 to 289.

We do not apply the PK-silver filter of the original client.

**Gaps (equipment).**

- CHANGE does not reach the Inventory. The original copies endure[0], energy[0], valid, tradable and expiration into
  the equipped items (`UpdataEquipData`, `UIEquipForm.cpp:796-823`). Our equipped items keep the values of the last
  SWITCH, so a tooltip (ticket 05) would show old durability.
- The original equip form hides head, face, body, glove or shoes when the item id is the character's default skin part
  (`sSkinInfo`, `UIEquipForm.cpp:825-836`). We do not. The server does not write skin ids into the links (no
  `sSkinInfo` in `sources/Server`), and the capture of `T1` carried none.

### KITBAG (6), KITBAGTMP (28), BANK (26), GUILDBANK (30)

**Payload:** `WriteKitbag` (`CharacterPacket.cpp:168-233`). Original client: `ReadChaKitbagPacket`
(`PacketCmd_SC.cpp:3643-3733`). This is the same block as the EnterMap kitbag ([KitbagSlot](view.md#kitbagslot)).

- `u8 synType`: `ESynKitbagType` (`CompCommand.h:84-100`). INIT 0, EQUIP 1, UNFIX 2, PICK 3, THROW 4, SWITCH 5,
  TRADE 6, FROM_NPC 7, TO_NPC 8, SYSTEM 9, FORGES 10, FORGEF 11, BANK 12, ATTR 13.
- `i16 capacity` only if the type is INIT.
- Repeated `i16 gridId`, `i16 itemId`. If itemId is not 0: `u32 dbId`; `i16` needLv, num, endure[2], energy[2]; `u8`
  forgeLv, valid, tradable; `i32 expiration`; `u32 boatWorldId` for a boat item; `i32 forge, i32 instId`;
  `u8 hasAttr` and `{i16, i16}` × 5 if it is 1.
- The list ends with `i16 -1`.

The BANK body can hold several such blocks, one for each bank (`Player.cpp:863-869`). All four go to self.

**KITBAG** (`SynKitbagNew`, `Character.cpp:5274-5288`; it also sends `CMD_MC_APPEND_LOOK`):

| synType | Sites |
|---|---|
| EQUIP | `CharacterCmd.cpp:908, 2270, 2888, 3176, 3690`; `CharScript.cpp:5640` |
| SYSTEM | `CharacterCmd.cpp:1042`; `Character.cpp:3356, 3430, 3809, 6486-6487`; `CharBoat.cpp:1037`; `CharScript.cpp:6166` |
| UNFIX | `CharacterCmd.cpp:1246` |
| PICK | `CharacterCmd.cpp:1430`; `CharacterSuperCmd.cpp:1240` |
| SWITCH | `CharacterCmd.cpp:1459, 1607`; `CharacterPrl.cpp:1456`; `Character.cpp:7671`; `Expand.h:2374, 2380` |
| THROW | `CharacterCmd.cpp:1569, 1706` |
| BANK | `CharacterCmd.cpp:1849, 1858, 1996, 2005` |
| FORGEF / FORGES | `CharacterCmd.cpp:3283, 3285`; `CharForge.cpp:171, 180, 195`; `CharacterSuperCmd.cpp:910` |
| TRADE | `CharTrade.cpp:2112-2113`; `CharStall.cpp:652, 655, 864, 890` |
| FROM_NPC | `CharacterPrl.cpp:2683`; `Character.cpp:2740, 2904, 3091, 3207, 4043, 4098` |
| TO_NPC | `Expand.h:1704, 1779` |
| ATTR | `Character.cpp:862`; `CharacterRun.cpp:325`; `lua_gamectrl.h:2038` |
| any | Lua `SynChaKitbag` (`Expand.h:5727`) |

**KITBAGTMP** (`SynKitbagTmpNew`, `Character.cpp:5291-5305`): SWITCH at `CharacterCmd.cpp:1465, 1469` and
`Expand.h:2379, 2385`; SYSTEM at `Character.cpp:3504, 3655, 3720` and `CharScript.cpp:6163`.

**BANK** (`CPlayer::SynBank`, `Player.cpp:838-873`; worldId is the controlled character): bank open at
`Player.cpp:929` with INIT; bank operation at `CharacterCmd.cpp:1999, 2008` with BANK.

**GUILDBANK** (`CPlayer::SynGuildBank`, `Player.cpp:817-836`; worldId is the main character). Without view permission
it sends an empty bag with capacity 0. BANK at `CharacterPrl.cpp:217` and `CharacterCmd.cpp:1852, 1861`; INIT at
`Player.cpp:986` and `GameAppNet.cpp:1428`.

**Our reader:** `KitbagNotice` (6) and `TemporaryKitbagNotice` (28) read the body as a `KitbagSync`
(`src/Top.Legacy.Protocol/Messages/Characters/KitbagSync.cs`): the sync type, the capacity only for INIT, and the
[KitbagSlot](view.md#kitbagslot) list. A boat item is found with `KitbagSlot.DefaultBoatItemIds`. BANK and GUILDBANK
are not read.

**Our client:**

- **KITBAG:** `ActionNoticeDecoder.Decode` gives `BagChangedEvent(WorldId, BagSync)`. `BagSyncMapper.ToBagSync`
  (`src/Top.Client.LegacyBridge/Inventory/BagSyncMapper.cs`) keeps the capacity only for INIT (else `null`) and makes
  one `BagSlot(Index, Item)` per slot. An empty slot has a `null` item. An occupied slot keeps item id, need level,
  count, durability and energy (current and maximum), forge level, valid, tradable, dbId (as `IsLocked`: the server
  locks an item by setting it to a value that is not 0, `CharacterPrl.cpp:1451`, and reads nothing else from it),
  expiration, forge (`ForgeParameter`) and instanceId (`InstanceParameter`). Instance attributes 26-47 (the
  `ITEMATTR_VAL_*` ids, `ItemAttrType.h:39-60`) become `CharacterStat` values.
  The subscriber is `WorldScene.ChangeBag`, which calls `CharacterInventory.ChangeBag` (`src/Top.Client/Inventory/CharacterInventory.cs`). It
  drops a sync with another world id: a boat hold syncs with the boat's world id (`Character.cpp:3091, 3207, 6487`;
  `CharBoat.cpp:1037`). Otherwise it takes the capacity, if present, and replaces the named slots.
- **KITBAGTMP:** `TemporaryBagChangedEvent(BagSync)`. The subscriber is `WorldScene.ChangeTemporaryBag`, which calls
  `CharacterInventory.ChangeTemporaryBag`.
- **BANK, GUILDBANK:** not read, dropped.

Captured on 2026-09-28 from the local stack after an item move (ITEM_POS from slot 0 to slot 6): a KITBAG SWITCH with
slot 0 empty and slot 6 holding item 437. It is pinned in `ServerConnectionTests`.

Captured on 2026-09-29 from the local stack after a [KITBAGTMP_DRAG](#kitbagtmp_drag-29): a KITBAG SWITCH and a
KITBAGTMP SWITCH. Both are pinned in `ServerConnectionTests`.

**Gaps:**

- The sync type is dropped after the capacity is read. The original shows a system message for PICK, SYSTEM, TRADE,
  FORGES and FORGEF (`NetProtocol.cpp:2281-2286`). We show none.
- `boatWorldId` is dropped. A boat item is out of scope for the inventory feature.
- Instance attribute ids outside 26-47 are dropped.
- The KITBAGTMP world id is dropped. The server writes the temporary bag for the main character only.
- **The temporary bag has 32 slots on the server** (the captured [TemporaryKitbagSync](items.md#temporarykitbagsync-35--reply-554)
  reply has capacity 32). `CharacterInventory` holds 16, as the spec says, and drops a slot from 16 up with a `Log.Warning`.
  The capacity of the temporary bag is not kept.
- `CMD_MC_APPEND_LOOK`, which `SynKitbagNew` also sends, is not read.
- A later bag capacity change is not read. An inventory expansion item raises the capacity with
  `CMD_MC_KITBAG_CAPACITY` 551 (`SynKitbagCapacity`, `Character.cpp:6845-6852`, sent from `Character.cpp:933`), not with
  an INIT sync. `CharacterInventory.BagCapacity` keeps the enter-map value, so the new slots stay hatched until the next
  map entry.

### ITEM_FAILED (15)

**Payload:** `i16 failId` (`EItemOperateResult`, `CompCommand.h:255-276`). Server: `Character.cpp:5512-5521`. The
original client shows a system message (`PacketCmd_SC.cpp:1059-1065`).

**To:** self.

**Situations:**

- Any failed item BeginAction: pick, throw, use, unfix, item position, temp-bag drag, delete, bank
  (`CharacterPrl.cpp:2241, 2253, 2263, 2300, 2311, 2322, 2332, 2353`)
- Guild bank operation (`CharacterPrl.cpp:104`)
- NPC buy, sell or give-item failures:
  - KBLOCK: `Character.cpp:2907, 3210, 3336, 3402, 3478, 3629`
  - NONE: `Character.cpp:2912, 3215, 3341, 3407, 3483, 3634`
  - KBFULL: `Character.cpp:3421, 3496, 3647`
  - `enumKBACT_ERROR_FULL` = **-4**, a kitbag code, not an `EItemOperateResult`: `MakeItem` when the bag is full
    (`Character.cpp:3346`, `Libraries/common/include/Kitbag.h:45`). The original client shows "Unknown item usage
    failure code" for it.

**Failure codes** (`EItemOperateResult`). 0 SUCCESS is never sent. The original texts are in `g_GetUseItemFailedInfo`
(`Libraries/common/include/CommFunc.h:461-525`).

| Value | Name | `ItemActionFailure` |
|---|---|---|
| 1 | NONE | `ItemMissing`. Also every `Regroup` error that is not RANGE, for example a move from an empty slot |
| 2 | KBFULL | `BagFull` |
| 3 | UNUSE | `CannotUse` |
| 4 | UNPICKUP | `CannotPickUp` |
| 5 | UNTHROW | `CannotThrow` |
| 6 | UNDEL | `CannotDelete` |
| 7 | KBLOCK | `BagLocked`. The security-code lock, a trade or stall lock of the kitbag, or reading a book |
| 8 | DISTANCE | `TooFar` |
| 9 | EQUIPLV | `LevelTooLow` |
| 10 | EQUIPJOB | `JobMismatched` |
| 11 | STATE | `WrongState` |
| 12 | PROTECT | `Protected` |
| 13 | AREA | `WrongArea` |
| 14 | BODY | `WrongBody` |
| 15 | TYPE | `WrongType` |
| 16 | INVALID | `Invalid` |
| 17 | KBRANGE | `OutOfBagRange` |
| 18 | EXPIRATION | `Expired` |

**Our reader:** `ItemFailedNotice` (`src/Top.Legacy.Protocol/Messages/Play/ItemFailedNotice.cs`) reads `Result`.

**Our client:** `ActionNoticeDecoder.Decode` (`ActionNoticeDecoder.cs:29`) gives `ItemActionFailedEvent(Failure)`.
`ItemActionFailureMapper.ToFailure` (`src/Top.Client.LegacyBridge/Inventory/ItemActionFailureMapper.cs`) casts the code
to `ItemActionFailure` (`src/Top.Client.Messages/Inventory/Data/ItemActionFailure.cs`), and maps the -4 of `MakeItem` to
`BagFull`. The subscriber is
`WorldScene.ReportItemActionFailure` (`WorldScene.cs:114, 507-510`). It calls `HudScreen.ShowItemActionFailure`
(`HudScreen.cs:140-143`), which shows the text of `ItemActionFailureText.Describe`
(`src/Top.Client/Inventory/ItemActionFailureText.cs`) in the HUD toast. Each code has its own text. KBLOCK says that
the bag is locked and items can't be moved, in the words of the bag-lock strip.

Captured on 2026-09-28 from the local stack: an ITEM_POS in the locked bag of `Observer` gave
`00000002 00000001 0F 0007` (KBLOCK). It is pinned in `ActionNoticeTests` and `ServerConnectionTests`.

**Gaps:**

- worldId and packetId are dropped. The server sends the notice to self only, so every failure is shown, whatever
  the world id. A failure of a boat's item action shows the same way.
- Any other code outside 1-18 shows "The item action failed. Error N".

### LEAN (16)

**Payload:** `u8 state` (always 0 here), then `i32` pose, angle, x, y, height. Server: `CharacterPrl.cpp:2209-2220`.
Original client: `PacketCmd_SC.cpp:1036-1057`.

**Situation:** relay of the client's BeginAction LEAN (`CharacterPrl.cpp:2187-2232`). **To:** eyeshot. A lean state
that is not 0 is sent only in BeginSee (`Character.cpp:558`).

**Our client:** not read, dropped.

### CHANGE_CHA (17)

**Payload:** `i32 newControlledWorldId`. Server: `FightAble.cpp:1745-1759`. Original client:
`PacketCmd_SC.cpp:1113-1119`.

**To:** self.

**Situations:** board a boat, `SkillInBoat` (`FightAble.cpp:1645`); leave a boat, `SkillOutBoat` (`FightAble.cpp:1709`).

**Our client:** not read, dropped. This is one of the packets that tells the client which character it controls. See
[view.md](view.md#characterbeginsee-504).

### FACE (19) and SKILL_POSE (21)

**Payload:** `i16 angle, i16 pose`. Server: `CharacterPrl.cpp:2499-2506`, `:2528-2535`. Original client:
`PacketCmd_SC.cpp:1121-1150`.

**To:** eyeshot. Both are relays of the client's BeginAction. SKILL_POSE is relayed only when the character is not a
boat, does not move, does not fight, and can move (`CharacterPrl.cpp:2519-2522`).

**Our client:** not read, dropped.

### PK_CTRL (22)

**Payload:** `u8 bits`. Bit 0 in PK, bit 1 in gymkhana, bit 2 guild PK. Server: `CharacterPacket.cpp:91-94`. Original
client: `PacketCmd_SC.cpp:3580-3593`.

**To:** eyeshot (`SynPKCtrl`, `Character.cpp:5481-5491`).

**Situations:** enter map (`CharacterCmd.cpp:293`); `CMD_CM_PK_CTRL` (`CharacterPrl.cpp:1890`); BeginAction PK_CTRL
(`CharacterPrl.cpp:2575`); `SetEnterGymkhana` (`Character.cpp:5541`); board a boat (`FightAble.cpp:1662`); leave a boat
(`FightAble.cpp:1722`).

**Our client:** not read, dropped.

### LOOK_ENERGY (23)

**Payload:** 34 × `i16 energy[0]`, 0 for an empty slot. Server: `FightAble.cpp:1241-1256`. Original client:
`PacketCmd_SC.cpp:3571-3578`.

**To:** self, as the main character (`FightAble.cpp:1156-1168`).

**Situation:** only the Lua function `LookEnergy` (`Expand.h:7566-7599`).

**Our client:** not read, dropped.

### TEMP (24)

**Payload:** `i32 itemId, i32 partId`. Server: `CharacterPrl.cpp:2461-2476`.

**Situation:** relay of the client's BeginAction TEMP. **To:** eyeshot.

**Our client:** not read, dropped.

### REQUESTGUILDLOGS (32) and UPDATEGUILDLOGS (33)

**Payload:** up to 13 entries of `{i16 type, i64 time, i64 parameter, i16 quantity, i16 userId}`. If the list ends
early, a sentinel `i16 9` follows. UPDATE puts `i16 totalCount` first. Server: `CharacterPrl.cpp:2369-2430`.

**Situation:** replies to the BeginAction with the same number. **To:** self.

**Our client:** not read, dropped.

---

## BeginAction (6)

`CMD_CM_BASE + 6`. Client to server.

**Payload:** `u32 worldId, u32 packetId, u8 actionType`, then the body. Original client: `ProCirculateCS.cpp:23-384`.
The MOVE body is one sequence: `u16` byte length, then `{i32 x, i32 y}` × n in centimetres (`:52-57`).

**Our client**

- It sends **MOVE (1)**, **ITEM_THROW (9)**, **ITEM_UNFIX (10)**, **ITEM_USE (11)**, **ITEM_POS (12)**,
  **ITEM_DELETE (13)** and **KITBAGTMP_DRAG (29)**. `BeginActionRequest` (`Messages/Play/BeginActionRequest.cs`)
  writes the header, and each action type has its own subclass that writes the body: `MoveRequest`
  (`Messages/Play/MoveRequest.cs`) writes the path, `ItemThrowRequest` (`Messages/Play/ItemThrowRequest.cs`) the
  throw, `ItemUnfixRequest` (`Messages/Play/ItemUnfixRequest.cs`) the unequip, `ItemUseRequest`
  (`Messages/Play/ItemUseRequest.cs`) the equip, `ItemMoveRequest` (`Messages/Play/ItemMoveRequest.cs`) the item move,
  `ItemDeleteRequest` (`Messages/Play/ItemDeleteRequest.cs`) the delete, `TemporaryKitbagDragRequest`
  (`Messages/Play/TemporaryKitbagDragRequest.cs`) the take from the temporary bag. [ITEM_THROW](#item_throw-9),
  [ITEM_UNFIX](#item_unfix-10), [ITEM_USE](#item_use-11), [ITEM_POS](#item_pos-12), [ITEM_DELETE](#item_delete-13)
  and [KITBAGTMP_DRAG](#kitbagtmp_drag-29) have their own sections. The rest of this section is about MOVE.
- `MoveEncoder` (`MoveEncoder.cs:11-21`) takes `MoveCommand(worldId, path of 2 or more points)`. packetId is
  `++_packetId` (`ServerConnection.cs:176-179`), reset to 0 on connect (`:64`). Registered at
  `ServerConnection.Messages.cs:31-32`.
- The only sender is `PredictedMotion.SendMove` (`PredictedMotion.cs:247-283`, send at `:260`). The path goes from the
  last known server position to the target. It is called from:
  - `StartMove`, when no move waits for a server end (`:222-227`)
  - `AcceptServerEnd`, for the pending move after the server ends the current one (`:290-293`)
  - `AbandonMove`, then `StartMove`, when a move was pending (`:316-318`)
- `StartMove` comes from `MoveTo` (a click, `PlayerController.cs:128`) and `RunToward` (a held button follows the
  cursor, `PlayerController.cs:174`).

**Server reaction.** Dispatch at `CharacterPrl.cpp:297-310`. Handler `CCharacter::BeginAction` (`:1960-2581`).

- It applies the packet to the controlled character or to the main character whose id equals worldId. Any other id is
  ignored with no reply (`:304-307`).
- **Dropped with no reply:**
  - the character is not alive (`:1966-1973`)
  - the controlled character has no map (`:1974-1981`, `:1997-2004`)
  - `Cmd_BeginMove` finds the character not alive (`CharacterCmd.cpp:326-328`)
  - a move while already moving to the same end point (`CharacterCmd.cpp:347-353`)
- **FailedAction** with type 1:
  - EXISTACT: an action still runs (`CharacterPrl.cpp:2006-2015`), or the character sits (`:2017-2026`)
  - MOVEPATH: no path data (`:2033-2042`), or more than `defMOVE_INFLEXION_NUM` points (`:2043-2052`)
  - ACTFORBID: move is forbidden (`CharacterCmd.cpp:329-336`), the character is about to sleep (`:338-345`), or MSPD
    is 0 (`:355-362`)
  - MOVEPATH: a point is not a valid map position (`:364-372`)
- **Success:** `Cmd_BeginMove(ping, path, n)` (`CharacterPrl.cpp:2055`) adds the move to `m_CAction` and runs
  `DoNext` (`CharacterCmd.cpp:388-392`). `DesireMoveBegin` starts it at once or keeps it until the previous pre-move
  time ends (`MoveAble.cpp:389-415`). The MOVE NotiActions follow, as the MOVE table shows.
- **No reply:** our client waits `AnswerTimeout` = 3 s, then `AbandonMove` snaps it to the server position
  (`PredictedMotion.cs:139-144`).

### ITEM_THROW (9)

**Payload:** `i16 grid, i16 count, i32 x, i32 y` (`CharacterPrl.cpp:2244-2255`). x and y are the drop position in
centimetres. Original client: `ProCirculateCS.cpp:170-182`; `stNetItemThrow` keeps the count as a `long` and writes
it as `i16`.

**Original client.** A drag of a bag item out of the window (`CEquipMgr::evtThrowItemEvent`,
`UIEquipForm.cpp:1493-1549`) takes the mouse point on the map. `_GetThrowPos` (`:1479-1491`) moves it to 1 m from the
character in the direction of the mouse, or to the character's own position when that point cannot be walked on. A
stack of more than one opens a number box filled with the whole stack (`:1538-1546`). A Yes/No box follows only when
the "throw item hint" option is on (`_SendThrowData`, `:1577-1619`). A boat deed (type 43) asks first with a box of its
own. `ThrowSelectedItems` (`:51-92`) throws each selected item with its whole stack.

**Our client**

- `ThrowBagItemEncoder` (`src/Top.Client.LegacyBridge/Inventory/ThrowBagItemEncoder.cs`) takes
  `ThrowBagItemCommand(worldId, bagSlot, count, dropPosition)` and writes an `ItemThrowRequest`. The drop position is
  in metres; `MapPointMapper.ToPacketPoint` writes it in whole centimetres. The packet id comes from the same counter
  as MOVE. Registered at `ServerConnection.Messages.cs:53-54`.
- `CharacterInventory.ThrowBagItem(source, count, dropPosition, items)` (`CharacterInventory.cs:143-153`) gives the
  command when `CanThrowBagItem` is true and the count is 1 to the stack. `CanThrowBagItem` needs a bag that is not
  locked (also while an unlock waits, and during a sort), an item inside the capacity, an item in the item table, and
  no Throw restriction (`ItemDescriber.RestrictionsOf`, `ItemDescriber.cs:221-241`): the item table allows a throw,
  the item is tradable and it has no item lock (a database id). The tooltip's "Can't throw" chip uses the same rule.
  Else it gives `null` and nothing is sent. The bag does not change until the KITBAG THROW sync.
- The drop position is the player's own map position (`WorldScene.ThrowBagItem`, `WorldScene.cs:590-603`), not a
  point under the mouse.
- The senders are in `InventoryWindow`:
  - A drag of a bag item that ends outside every control, with no drop taken (`ThrowItemDraggedOut`,
    `InventoryWindow.cs:715-723`). A drop on the window itself or on another HUD control throws nothing.
  - Throw in the right-click menu of a bag item (`ClickBagItem`, `:635-648`). The entry is disabled when
    `CanThrowBagItem` is false.
  - Both open `ConfirmThrow` (`:725-746`): a single item asks "Throw <name> away?" in `ConfirmPopup` (Throw in a red
    pill, Cancel); a stack opens `QuantityPopup` "Throw how many <name>? 1 to N.", filled with the whole stack, with a
    red Throw pill. The confirm emits `BagItemThrowRequested`; `HudScreen` passes it on to `WorldScene.ThrowBagItem`.

**Server reaction.** `Cmd_ThrowItem` (`CharacterCmd.cpp:1476-1586`):

- ITEM_FAILED KBLOCK when the bag is locked with the security code, the character reads a book, or the kitbag is
  locked by a trade or stall (`:1478-1490`); STATE when item actions are not allowed (`:1487-1488`).
- ITEM_FAILED DISTANCE when the drop position is more than `defTHROW_DISTANCE` = 350 cm from the character
  (`:1494-1495`, `CompCommand.h:455`).
- ITEM_FAILED NONE for an empty slot or an unknown item (`:1500`).
- ITEM_FAILED UNTHROW when the server item table does not allow a throw, the item is not tradable, or it has a
  database id (`:1511-1517`); on a boat, also for an item that goes to the kitbag.
- A boat deed (type 43) takes its boat off the character (`BoatClear`, `:1521-1535`).
- ITEM_FAILED AREA when the drop position and the character are not both on land or both at sea (`:1543-1544`).
- A count of 0 throws the whole stack; a count above the stack is cut to the stack (`:1546-1552`).
- Success: KITBAG NotiAction with sync type THROW to self (`:1569`), then the item appears on the ground at the drop
  position (`ItemSpawn`, `:1581`, `CMD_MC_ITEMBEGINSEE` 506 to the eyeshot).

**Checked** on 2026-09-29 against the local stack, as `Observer`, in a windowed run of the client: the right-click
menu threw a Hunter Vest after the confirmation; a drag of 20 Apples out of the window opened the quantity popup, and
5 went to the ground (15 stayed). A console tool captured, pinned in `ServerConnectionTests`:

- A throw of bag slot 13 at the character's position: `00000002 00000001 06 04 000D 0000 FFFF` (KITBAG THROW, slot 13
  empty), then 506.
- A throw at a position 17.5 m away: `00000002 00000001 0F 0008` (ITEM_FAILED DISTANCE).

**Gaps:**

- The drop position is the character's own position. The original drops 1 m toward the mouse. The client does not
  read 506, so the thrown item is not shown on the ground, and nothing picks it up (ITEM_PICK (8) is out of scope).
- The client position is the predicted one. A throw sent while the character walks can be more than 3.5 m from the
  server position and gets DISTANCE. The first windowed run met this.
- A refused throw is shown in the HUD toast. A drag of an item that `CanThrowBagItem` refuses shows nothing.
- Boat deeds (type 43) have no throw and delete flags in our item table, because boat types are out of the content
  scope. The client disables both entries for them. The server allows both for the deed in its `ItemInfo.txt`; a
  throw of `Observer`'s Vessel Deed was accepted, and picking it up again reset the boat to level 1 with 0
  experience (system messages 517 "After trade, ship level becomes 1").
- An equipped item cannot be thrown. ITEM_UNFIX with grid -2 is not sent.
- AREA is not checked by the client: the drop position is the character's own, which is always the same area.

### ITEM_UNFIX (10)

**Payload:** `i8 linkId, i16 grid`, then `i32 x, i32 y` only when grid is -2 (`CharacterPrl.cpp:2266-2302`). The
original client writes x and y for every negative grid (`ProCirculateCS.cpp:195-209`); the server reads them only for
-2, so a -1 packet from the original client has 8 bytes the server does not read.

| grid | Meaning |
|---|---|
| 0 or more | into this bag slot |
| -1 | into the first free bag slot |
| -2 | onto the ground at x, y (centimetres) |

**Original client.** A drop of an equip slot onto any bag slot, empty or filled, sends that slot
(`UIGlobalVar.cpp:211-230`, `UIEquipForm.cpp:1733-1743`). A double-click on an equip slot sends -1
(`UIItemCommand.cpp:2081-2085`). A drop outside the window sends -2 with a position (`UIEquipForm.cpp:1303`).

**Our client**

- `UnequipItemEncoder` (`src/Top.Client.LegacyBridge/Inventory/UnequipItemEncoder.cs`) takes
  `UnequipItemCommand(worldId, slot, bagSlot)` and writes an `ItemUnfixRequest` with the `EquipmentSlot` value as the
  link. The packet id comes from the same counter as MOVE. Registered at `ServerConnection.Messages.cs:49-50`.
- `ItemUnfixRequest` reads and writes all three grid variants. The client sends only a bag slot of 0 or more.
- `CharacterInventory.UnequipItem(slot, destination)` (`CharacterInventory.cs:140-143`) gives the command when the bag
  is not locked (also while an unlock waits), the equip slot holds an item, and the destination is an **empty** bag
  slot inside the capacity. Else it gives `null` and nothing is sent. The inventory does not change until the LOOK and
  KITBAG syncs.
- The sender is `InventoryWindow`. A drag starts on an equip cell where `CanUnequipItemFrom` is true
  (`InventoryWindow.cs:468-480`). A drop on a bag cell where `CanUnequipItem` is true emits `ItemUnequipRequested`
  (`:543-550`). `HudScreen` passes it on to `WorldScene.UnequipItem` (`WorldScene.cs:531-537`).

**Server reaction.** `Cmd_UnfixItem` (`CharacterCmd.cpp:1065-1253`):

- ITEM_FAILED KBFULL when the bag has no free slot (`:1076-1080`), before any other check. A bicker notice "Please Make
  Sure You Have 5 slots Empty !" comes with it, though the check is for 0 free slots.
- ITEM_FAILED STATE when item actions are not allowed; KBLOCK when the kitbag is locked (trade or stall), locked with
  the security code, or the character reads a book.
- ITEM_FAILED NONE for a link outside 0-33. An empty link is a success with no sync.
- A link that holds 9999 (the other hand of a two-handed item) unfixes the other hand; 9998 unfixes the body.
- `CKitbag::Push` (`Libraries/common/src/Kitbag.cpp:622-790`) puts the item in the grid slot when it is empty, or
  onto a stack of the same item. **A filled grid slot does not swap:** the item goes to the first free slot.
- -2: ITEM_FAILED UNTHROW when the item cannot be thrown or is not tradable; the item appears on the ground.
- Success: LOOK NotiAction SWITCH to the eyeshot (`SynLook`, `:1230-1233`), KITBAG NotiAction with sync type UNFIX to
  self (`:1245-1246`), and the attributes.

**Gaps:**

- The client refuses a drop onto a filled bag slot. The original sends it, and the server puts the item in the first
  free slot, not in the slot under the cursor.
- A double-click on an equip slot does nothing. The original unequips it into the first free slot.
- Unfix to the ground (-2) is not sent. A drag of an equip slot out of the window does nothing; only bag items are
  thrown ([ITEM_THROW](#item_throw-9)).

### ITEM_USE (11)

**Payload:** `i16 grid, i16 targetGrid` (`CharacterPrl.cpp:2256-2264`). Original client: `ProCirculateCS.cpp:183-193`.

For an equipment item, targetGrid is not a link:

| targetGrid | Meaning (`Cmd_UseItem`, `CharacterCmd.cpp:725-726`) |
|---|---|
| -2 | the second link: a type 1 item (sword) goes to link 6, a type 26 item (ring) to link 8, when the item fits there |
| anything else | the first free link the item fits, or the first link it fits when all are full |

For a consumable, targetGrid is the bag slot of the target item (pet food, `UIItemCommand.cpp:1860-1896`), or -1
for no target item (`ItemUseRequest.NoTargetItem`).

**Original client.** `stNetUseItem` sets targetGrid to -1 (`NetProtocol.h:321-326`). A drop of a bag item onto any
equip cell sends -1, whichever cell it is (`UIEquipForm.cpp:1231-1253`). A double-click sends -1, a right-click -2
(`UIItemCommand.cpp:2048-2076`).

**Our client**

- `UseBagItemEncoder` (`src/Top.Client.LegacyBridge/Inventory/UseBagItemEncoder.cs`) takes
  `UseBagItemCommand(worldId, bagSlot)` and writes an `ItemUseRequest` with target -1. The packet id comes from the
  same counter as MOVE. Registered at `ServerConnection.Messages.cs:49-50`.
- `EquipBagItemEncoder` (`src/Top.Client.LegacyBridge/Inventory/EquipBagItemEncoder.cs`) takes
  `EquipBagItemCommand(worldId, bagSlot, isSecondSlot)` and writes an `ItemUseRequest` with target -2 when
  `isSecondSlot`, else -1. The packet id comes from the same counter as MOVE. Registered at
  `ServerConnection.Messages.cs:47-48`.
- The two commands write the same bytes when the use has no target item and the equip has no second slot: target
  -1. The server selects use or equip from the item's links.
- `Top.Client.Cli` sends `UseBagItemCommand` for the `use <bag slot> [timeout]` step, for any item. The step ends at
  the next KITBAG NotiAction of its own character or at an ITEM_FAILED, and fails at the time limit. Checked on
  2026-10-03 against the test server, as `Observer`: `&make 481,1` then `use` on its bag slot equipped the new glove,
  and the worn glove went to that bag slot.
- `CharacterInventory` (`CharacterInventory.cs:105-128`, rule at `:187-212`):
  - A bag item fits a link from its `equipLinks`, read up to the first negative value. The default slot is the first
    free fitting link, else the first fitting link. The second slot is link 6 for type 1 and link 8 for type 26.
  - `CanEquipBagItem(source, slot, items)` is true only for the default slot and the second slot: the two links the
    server can fill. A ring with ring 1 full and ring 2 empty fits ring 2 only; a bracelet fits one slot only.
  - `EquipBagItem(source, slot, items)` sends -2 for the second slot when it is not also the default slot, else -1.
    `EquipBagItem(source, items)` sends -1. `CanEquipBagItem(source, items)` is its rule.
  - Both give `null` when the bag is locked (also while an unlock waits), the source is empty or outside the
    capacity, or the item is not in the item table or fits no link.
  - The inventory does not change until the LOOK and KITBAG syncs.
- `CharacterInventory` use rule (`CharacterInventory.cs:257-294`):
  - `IsUsable(item)`: the item has a use script (`scriptEffect` not "0"), its first equip link is -1 (or it has no
    link list), which is the server's rule for a consumable, and it is not type 71 (skill item), type 66 or items
    864-866 (they ask for a target item).
  - `UseBagItem(source, items, now)` gives `null` when the bag is locked (also while an unlock waits) or sorts, the
    source is empty or outside the capacity, the item is not in the item table or not usable, or the item's
    cooldown runs. Else it starts the cooldown of the item id for `cooldown` seconds and gives the command.
  - `CooldownLeftOf(itemId, now)` gives the part of the cooldown that is left, 1 to 0. The cooldown belongs to the
    item id, so every bag slot with that item is blocked.
  - The inventory does not change until the KITBAG sync.
- The sender is `InventoryWindow`:
  - A bag drag switches the window to the tab that shows the item's first fitting link (`ShowFittingTab`,
    `InventoryWindow.cs:317-338`), and every visible equip cell shows the bag's drop ring (`CanEquipBagItem`) or
    dims to 35 % (`ShowEquipmentDropHints`, `:345-368`). A drag of an item that fits no link dims nothing.
  - A drop on a ringed equip cell emits `BagItemEquipToSlotRequested` (`DropOnEquipmentSlot`, `:796-802`).
  - A double-click on a bag cell (`ClickBagItem`, `:668-685`) emits `BagItemEquipRequested` for an item that fits a
    link, else `BagItemUseRequested`.
  - The right-click menu (`ShowItemMenuEntries`, `:705-728`) shows Use for a usable item and Equip for an item that
    fits a link. Use is disabled while `CanUseBagItem` is false (locked bag, cooldown), Equip while
    `CanEquipBagItem` is false (locked bag). The entries emit the same two signals.
  - Each frame, every bag cell shows the cooldown of its item id as a fill from the bottom of the cell, and an open
    menu updates its entries
    (`ShowCooldowns`, `:399-412`; `ItemCell.ShowCooldown`), `rgba(0,0,0,.55)`.
  - `HudScreen` passes the signals on to `WorldScene.UseBagItem`, `EquipBagItem` and `EquipBagItemToSlot`
    (`WorldScene.cs:535-557`).

**Server reaction.** `Cmd_UseItem` (`CharacterCmd.cpp:688-732`), then `Cmd_UseEquipItem` (`:737-922`) for an item
with links:

- ITEM_FAILED KBLOCK when the kitbag is locked (trade or stall), locked with the security code, the character reads a
  book, or it has a stall.
- ITEM_FAILED NONE for an empty source; EXPIRATION for an expired item.
- ITEM_FAILED STATE when item actions are not allowed; the `CanEquipItem` codes: level 9, job 10, body 14 (checked on
  `Observer`, ticket 04).
- ITEM_FAILED KBFULL when every fitting link is full and the bag has no free slot.
- The item leaves the bag; the item in the link, and the items in its need-links (the other hand of a two-handed
  item), go to the first free bag slots through `Cmd_UnfixItem`. On a failure there the item goes back to its bag slot.
- Success: LOOK NotiAction SWITCH to the eyeshot (`SynLook`, `:903-906`), KITBAG NotiAction with sync type EQUIP to
  self (`:908`), and the attributes.

For an item with no link (`szAbleLink[0] == -1`, `:723-724`), `Cmd_UseExpendItem` (`:927-1055`):

- No reply at all when any player used a consumable less than 200 ms before: one static time for the whole server,
  and the function gives SUCCESS.
- ITEM_FAILED STATE when item actions or item use are not allowed; NONE for an empty source, or a target item with
  a per-item lock (with a system notice).
- ITEM_FAILED UNUSE for a type 36 scroll on a boat, for `szAttrEffect` "0", and when the script calls
  `UseItemFailed`. The scripts (`resource/script/calculate/ItemEffect.lua`) run as `(role, item, targetItem)` and
  check level, HP, cooldowns of their own and the map; most send a system notice with the reason first.
- A script that gives a mission ends with SUCCESS and nothing more.
- Success: one item leaves the bag, KITBAG NotiAction with sync type SYSTEM to self, and the attributes with
  `ATTRSYN_ITEM_MEDICINE` (`AttributeChangeReason.ItemUse`). A type 36 scroll is not taken by C++: its script
  (`Ticket`, `ItemEffect.lua:914-954`) deletes it and calls `MoveCity`, which sends a new `EnterMap` (516).
- `CMD_MC_ITEM_USE_SUC` (550) to the eyeshot when the item record asks for it (`:728-729`, `SynItemUseSuc`).

**Checked** on 2026-09-28 against the local stack, as `Observer`: a drop of ring 2 onto an empty bag slot put it
there; a Gold Ring dropped on ring 2 (ring 1 full) went to link 8; a double-click of a ring with both ring links full
replaced ring 1; a ring dropped on ring 2 with both full (-2) replaced ring 2; an aura dragged from the Gear tab
switched to Apparel and went to the ringed link.

**Gaps:**

- The server takes no link. A drop on a fitting link the server would not fill cannot be sent, so that link has no
  ring: a bracelet with both links empty or both full fits bracelet 1 only.
- The client reads 9999 and 9998 links as empty (`EquipmentMapper`). The server counts them as filled. The default
  slot the client shows can differ from the one the server picks when a two-handed item or a body item holds a link.
- A right-click does not send -2. It opens the item menu (Use, Equip, Delete, Throw, Lock, Unlock).
- No target item is sent: type 66 and items 864-866 are not used. Type 71 does not cast its skill.
- The client cooldown starts when the use is sent, also when the server refuses it. A use inside the server's
  200 ms throttle gets no reply, and the cooldown runs all the same.
- The system notice a script sends with its refusal is not shown; the toast shows the ITEM_FAILED text only.
- A type 36 scroll is used up and the server moves the character, but the second `EnterMap` has no subscriber in
  the world scene and is dropped: the client stays on the old map.
- `CMD_MC_ITEM_USE_SUC` (550) is not decoded; no use effect plays.

### ITEM_POS (12)

**Payload:** `i16 srcGrid, i16 srcNum, i16 tarGrid` (`CharacterPrl.cpp:2303-2312`). Original client:
`ProCirculateCS.cpp:211-221`.

**Original client.** `CEquipMgr::evtSwapItemEvent` (`UIEquipForm.cpp:1417-1463`) sends a drag between two bag slots
with srcNum 0. With Shift held on a stack of more than one, and a target that is empty or holds the same item, it first
opens a number box (`:1446-1455`), filled with the whole stack. `_evtSplitItemEvent` (`:1465-1477`) sends the number
typed. The drag also works on a boat's hold, with the boat's world id.

**Our client**

- `MoveBagItemEncoder` (`src/Top.Client.LegacyBridge/Inventory/MoveBagItemEncoder.cs`) takes
  `MoveBagItemCommand(worldId, sourceSlot, count, destinationSlot)` and writes an `ItemMoveRequest`. The packet id comes
  from the same counter as MOVE. Registered at `ServerConnection.Messages.cs:45-46`.
- The commands come from `CharacterInventory` (`src/Top.Client/Inventory/CharacterInventory.cs:77-105`):
  - `MoveBagItem(source, destination)` sends the whole stack as the count. The server treats it the same as the
    original's 0.
  - `SplitBagItem(source, count, destination)` sends 1 to stack − 1, onto an empty slot or a stack of the same item.
  - Both give `null`, and nothing is sent, when the bag is locked (also while an unlock waits), the source is empty,
    source and destination are the same, or a slot is outside the bag capacity.
- The inventory does not change when the command is sent. It changes only on the KITBAG SWITCH that follows.
- The sender is `InventoryWindow` (`src/Top.Client/Inventory/InventoryWindow.cs:428-640`). Each bag cell forwards its
  drag and drop to the window. A drag starts only where `CanMoveBagItemFrom` is true: an item inside the capacity of an unlocked bag. A drop is allowed where
  `CanMoveBagItem` is true. A drop with Shift held, where `CanSplitBagItem` is true, opens `QuantityPopup`
  (`src/Top.Client/UI/QuantityPopup.cs`), filled with half the stack and limited to stack − 1. Any other drop sends a
  move. `HudScreen` passes the signals on to `WorldScene.MoveBagItem` and `SplitBagItem` (`WorldScene.cs:502-516`),
  which send the command if the inventory gives one.

**Server reaction.** `Cmd_ItemSwitchPos` (`CharacterCmd.cpp:1589-1616`):

- ITEM_FAILED STATE when item actions are not allowed (`GetActControl(enumACTCONTROL_ITEM_OPT)`).
- ITEM_FAILED KBLOCK when the kitbag is locked (trade or stall), locked with the security code, or the character reads
  a book.
- `CKitbag::Regroup` (`Libraries/common/src/Kitbag.cpp:465-567`):
  - a slot outside the capacity: ITEM_FAILED KBRANGE
  - an empty source: ITEM_FAILED NONE
  - srcNum 0, or more than the stack, means the whole stack
  - the same item in both slots, with free room, no expiration on either, and the same tradable flag: up to srcNum
    items move onto the target, up to its stack limit. Else the two slots swap.
  - an empty target: srcNum items move there
  - another item in the target: the two slots swap, whatever srcNum is
- Success: KITBAG NotiAction SWITCH to self with the changed slots (`CharacterCmd.cpp:1607`).

**Gaps:**

- A split of the whole stack is not offered: the popup stops at stack − 1. The original number box allows the whole
  stack.
- A Shift drop onto a stack of another item sends a plain move (a swap), the same as the original.
- A boat's hold cannot be used: the client has no boat hold window.

### ITEM_DELETE (13)

**Payload:** `i16 grid` (`CharacterPrl.cpp:2325-2334`). There is no count: the server always deletes the whole stack.
Original client: `ProCirculateCS.cpp:223-233`.

**Original client.** Delete in the right-click menu of a bag item opens a Yes/No box, then sends the slot
(`_evtDeleteItemYesNoEvent`, `UIEquipForm.cpp:1255-1262`). `DeleteSelectedItems` (`:125-156`) deletes each selected
item, and skips one whose item table does not allow a delete. The original has no Delete key.

**Our client**

- `DeleteBagItemEncoder` (`src/Top.Client.LegacyBridge/Inventory/DeleteBagItemEncoder.cs`) takes
  `DeleteBagItemCommand(worldId, bagSlot)` and writes an `ItemDeleteRequest`. The packet id comes from the same
  counter as MOVE. Registered at `ServerConnection.Messages.cs:55-56`.
- `CharacterInventory.DeleteBagItem(source, items)` (`CharacterInventory.cs:133-141`) gives the command when
  `CanDeleteBagItem` is true: a bag that is not locked (also while an unlock waits, and during a sort), an item inside
  the capacity, an item in the item table, and no Delete restriction (`ItemDescriber.RestrictionsOf`): the item table
  allows a delete and the item has no item lock. The tooltip's "Can't delete" chip uses the same rule. Else it gives
  `null` and nothing is sent. The bag does not change until the KITBAG THROW sync.
- The sender is Delete in the right-click menu of a bag item (`InventoryWindow.cs:635-713`). The entry is disabled
  when `CanDeleteBagItem` is false. It opens `ConfirmPopup` "Delete item", "Delete <name>? It will be gone for
  good." (with "N × " before the name for a stack), a red Delete pill and Cancel. The design says "Destroy" and
  "Keep"; the user chose "Delete" and "Cancel" (2026-09-29).
  Delete emits `BagItemDeleteRequested`; `HudScreen` passes it on to `WorldScene.DeleteBagItem`
  (`WorldScene.cs:582-588`).

**Server reaction.** `Cmd_DelItem` (`CharacterCmd.cpp:1618-1730`):

- ITEM_FAILED KBLOCK when the kitbag is locked by a trade or stall, locked with the security code, or the character
  reads a book (`:1624-1637`); STATE when item actions are not allowed (`:1631-1632`).
- ITEM_FAILED NONE for an empty slot or an unknown item (`:1645`).
- ITEM_FAILED UNDEL when the server item table does not allow a delete, or the item has a database id
  (`:1647-1655`, `:1689-1692`).
- A boat deed (type 43) removes its boat (`BoatClear`, `:1664-1672`), and the boat data is saved as deleted
  (`:1708-1715`).
- Success: the whole stack leaves the bag, KITBAG NotiAction with sync type THROW to self (`:1706`).

**Checked** on 2026-09-29 against the local stack, as `Observer`, in a windowed run of the client: the right-click
menu showed Delete and Throw enabled for a Country Cap, only Delete for an Exploring Chest (not tradable), and neither
for a Vessel Deed; Lock was enabled and Unlock disabled in the unlocked bag. Delete on the Country Cap opened
"Destroy Country Cap? It will be gone for good.", the words before the change to "Delete", and the confirm emptied its
slot. A console tool's delete of an empty slot gave `00000002 00000003 0F 0001` (ITEM_FAILED NONE).

**Gaps:**

- The Delete key does nothing (user decision, 2026-09-29, ticket 11): the bag has no selection, and the original has
  no Delete key.
- Delete in the menu while the bag is locked was not run live: the security codes of the test characters are not
  known. The unit tests cover it.

### KITBAGTMP_DRAG (29)

**Payload:** `i16 srcGrid, i16 srcNum, i16 tarGrid` (`CharacterPrl.cpp:2314-2324`). srcGrid is a temporary-bag slot,
tarGrid a bag slot. Original client: `ProCirculateCS.cpp:365-374`.

**Original client.** A drag from the temporary bag onto the bag grid calls `CStoreMgr::PopFromTempKitbag`
(`UIGlobalVar.cpp:406-411`, `UIStoreForm.cpp:905-933`). A single item goes at once with srcNum 1. A stack of more than
one opens a number box first; `_evtDragItemsEvent` (`:936-948`) sends the number typed. The original has no Take all,
and no drag in the other direction.

**Our client**

- `TakeTemporaryBagItemEncoder` (`src/Top.Client.LegacyBridge/Inventory/TakeTemporaryBagItemEncoder.cs`) takes
  `TakeTemporaryBagItemCommand(worldId, temporarySlot, count, bagSlot)` and writes a `TemporaryKitbagDragRequest`.
  A `null` bag slot is written as -1 (`FirstFreeGridId`). The packet id comes from the same counter as MOVE.
  Registered at `ServerConnection.Messages.cs:51-52`.
- The commands come from `CharacterInventory` (`src/Top.Client/Inventory/CharacterInventory.cs:107-144`):
  - `TakeTemporaryBagItem(source, destination, items)` sends the whole stack to one bag slot: an empty slot, or a stack
    that the server merges with (`StacksOnto`: the same item, room below the item's stack limit, no expiration on
    either, the same tradable flag).
  - `SplitTemporaryBagItem(source, count, destination, items)` sends 1 to stack − 1 items, to the same targets.
  - `TakeAllTemporaryBagItems()` sends one command for each filled temporary slot, the whole stack and bag slot -1.
  - All three send nothing while the bag is locked (also while an unlock waits). `CanTakeAllTemporaryBagItems` is false then,
    and when the temporary bag is empty.
- Neither bag changes when the command is sent. They change only on the KITBAG and KITBAGTMP syncs that follow.
- The senders are in `InventoryWindow` (`src/Top.Client/Inventory/InventoryWindow.cs`). Each temporary-bag cell
  forwards its drag to the window. A drag starts only where `CanTakeTemporaryBagItemFrom` is true; a bag cell accepts
  the drop, with the drop ring, where `CanTakeTemporaryBagItem` is true. A drop with Shift held, where
  `CanSplitTemporaryBagItem` is true, opens the same `QuantityPopup` as the bag split (half the stack, limited to
  stack − 1). A temporary-bag cell accepts no drop. The
  gold Take all pill in the temporary-bag header is disabled while `CanTakeAllTemporaryBagItems` is false. `HudScreen`
  passes the signals on to `WorldScene.TakeTemporaryBagItem`, `SplitTemporaryBagItem` and `TakeAllTemporaryBagItems`
  (`WorldScene.cs:542-564`), which send the commands the inventory gives.

**Server reaction.** `Cmd_DragItem` (`CharacterCmd.cpp:1438-1472`):

- ITEM_FAILED KBLOCK when the bag is locked with the security code (`:1440-1441`) or the character reads a book
  (`:1444-1445`). Unlike ITEM_POS, it does not check `GetActControl` or the trade and stall lock here.
- ITEM_FAILED NONE when the temporary slot is empty (`:1448-1449`) or holds an unknown item (`:1450-1452`).
- It pops srcNum items from the temporary slot; 0 or the whole stack means the whole stack (`CKitbag::Pop`,
  `Libraries/common/src/Kitbag.cpp:915-983`). It pushes them into the bag at tarGrid (`CKitbag::Push`,
  `Kitbag.cpp:622-827`):
  - an empty target takes the items
  - a target with the same item, room, no expiration on either and the same tradable flag takes as many as fit
  - the rest, and all items for any other target (another item, or -1), go first onto stacks of the same item, then
    into the first free slots
- Then KITBAG SWITCH to self (`:1459`). The server sends it only when a bag slot changed.
- If items are left over: a system message "Inventory is full!" (`GM_CHARACTERCMD_CPP_00007`), the rest go back to the
  temporary slot, KITBAGTMP SWITCH (`:1465`), and ITEM_FAILED KBFULL (`:1466`).
- Else KITBAGTMP SWITCH (`:1469`).

Captured on 2026-09-29 from the local stack as `T1`, pinned in `ServerConnectionTests`:

- Temporary slot 0 (3 Apples) to empty bag slot 6: KITBAG SWITCH with slot 6 holding 3 of item 1847, then KITBAGTMP
  SWITCH with slot 0 empty. Both carry the request's packet id.
- Temporary slot 1 (2 Elven Fruits) to -1 with no free bag slot: system message 517, KITBAGTMP SWITCH with slot 1
  still holding 2 of item 3116, then `0000000C 00000002 0F 0002` (ITEM_FAILED KBFULL). No KITBAG notice.

**Gaps:**

- A plain drag takes the whole stack; only a Shift drag asks how many. The original asks for every stack, and its
  number box allows the whole stack.
- The drop is allowed only where the item stays in the target slot. A drop on another item is refused, although the
  server would put the item in the first free slot.
- Take all sends one packet for each filled slot, also when the bag is full. Each one the server cannot place gives its
  own ITEM_FAILED KBFULL; the HUD toast shows the last one.
- The system message 517 is not read.
- Take all reaches temporary slots 0-15 only. The server holds 32 (see
  [KITBAGTMP](#kitbag-6-kitbagtmp-28-bank-26-guildbank-30)).
- **Server defect:** every push failure after the pop is reported as KBFULL. A trade or stall lock of the bag
  (`CKitbag::Push` fails with its own LOCK error) reads as "bag full". `Cmd_DragItem` also ignores the result of
  `Pop`: a srcNum larger than the stack pushes an empty grid. Our client always sends the stack it shows.

## EndAction (7)

`CMD_CM_BASE + 7`. Client to server.

**Payload:** empty. The original client writes only the command (`ProCirculateCS.cpp:387-392`).

**Our client**

- `EndActionRequest.Write` writes nothing (`EndActionRequest.cs:18-24`). `StopMoveEncoder` is registered at
  `ServerConnection.Messages.cs:33`.
- The only sender is `PredictedMotion.SendStop` (`PredictedMotion.cs:239-245`), from:
  - `StartMove`, when a new target arrives while the server still runs a move (`:229-234`). The original
    `COneMoveState::StartMove` does the same (`STMove.cpp:827-838`).
  - `Stop()` (`:118-126`), when the player presses another mouse button during cursor-follow
    (`PlayerController.cs:65-73`).
- It is sent at most once per move (`_isStopSent`).

**Server reaction.** `m_CAction.End()` (`Action.cpp:126-165`):

- A running move: `DesireMoveStop`, then `EndMove`, which sets `chRequestState = 1` (`MoveAble.h:78`,
  `MoveAble.cpp:537-544`). The next `OnMove` tick sends a MOVE NotiAction with CANCEL to the eyeshot
  (`MoveAble.cpp:564-590`). `SubsequenceMove` then starts a queued action (`Character.cpp:1622-1649`).
- A running skill: `DesireFightEnd`, which gives SKILL_SRC CANCEL (`FightAble.cpp:253-257`).
- No action runs: nothing happens and no reply is sent (`Action.cpp:128-131`).
- The character is not alive: the request is dropped (`Character.cpp:1685-1690`).
- EndAction never gives a FailedAction.

## FailedAction (520)

`CMD_MC_BASE + 20`. Server to client.

**Payload:** `u32 worldId, u8 actionType, u8 reason`. Server: `FailedActionNoti` (`Character.cpp:1672-1681`).

**To:** self. worldId is the character that ran the request.

**Reasons** (`EFailedActionReason`, `CompCommand.h:237-247`):

| Value | Name | Sent at |
|---|---|---|
| 0 | ACTFORBID | Move: `CharacterCmd.cpp:332, 341, 358`. Skill: `CharacterPrl.cpp:2064` (kitbag locked), `:2080` (bank open); `CharacterCmd.cpp:518` (skill not active or not allowed). Also with type **MOVE** from `Cmd_BeginSkill`: `CharacterCmd.cpp:434, 442` |
| 1 | EXISTACT | Move: `CharacterPrl.cpp:2009, 2020`. Skill: `:2086, 2096` |
| 2 | MOVEPATH | Move: `CharacterPrl.cpp:2036, 2046`; `CharacterCmd.cpp:368, 528` (the last with type MOVE from `Cmd_BeginSkill`). Skill: `CharacterPrl.cpp:2116, 2126` |
| 3 | CANTMOVE | never sent |
| 4 | NOSKILL | Skill: `CharacterPrl.cpp:2146`; `CharacterCmd.cpp:496, 507` |
| 5 | NOOBJECT | Skill: `CharacterCmd.cpp:543` |
| 6 | ITEM_INEXIST | never sent |
| 7 | SKILL_NOTMATCH | Skill: `CharacterCmd.cpp:450` |

The original client ignores the type and calls `GetActor()->FailedAction()` (`PacketCmd_SC.cpp:1204-1213`,
`NetProtocol.cpp:2682-2694`).

**Our handling**

- `FailedActionNotice.Read` (`FailedActionNotice.cs:21-32`) and `FailedActionDecoder` (`FailedActionDecoder.cs:12-22`)
  make `ActionFailedEvent(worldId, actionType, (ActionFailureReason)reason)`.
- `ActionFailureReason` (`src/Top.Client.Messages/Play/Data/ActionFailureReason.cs`) names only 0-3. Values 4-7 arrive
  as unnamed enum values. We send only MOVE, so only 0, 1 and 2 can arrive today.
- The subscriber is `WorldScene.RecoverFromFailedAction` (`WorldScene.cs:87, 284-290`). For the own character it calls
  `PredictedMotion.RecoverFromFailedAction` (`PredictedMotion.cs:176-179`) for every type and reason. That calls
  `AbandonMove`: it snaps to the server position and sends a pending move again.

## PreMoveTime (536)

`CMD_MC_BASE + 36`. Server to client.

**Payload:** `u32 milliseconds`. The value is the GM `setping` value if one is set, else the last measured ping
(`Character.cpp:5470-5479`).

**To:** self.

**When sent:**

- On enter map (`CharacterCmd.cpp:296`)
- After each CheckPing reply (`CharacterPrl.cpp:266-290`). CheckPing goes out every `defPING_INTERVAL` = 20 s
  (`Character.h:26`, `CharacterRun.cpp:170-171`). See [session.md](session.md#checkping-537-server-to-client--checkpingreply-17-client-to-server).
- On the GM command `setping` (`CharacterSuperCmd.cpp:975-986`)

The server also adds the same ping to the first step of every move (`CharacterPrl.cpp:2055`, `MoveAble.cpp:509`). The
original client stores the value with `CWaitMoveState::SetPreMoveTime` and uses it as a pre-move distance
(`NetProtocol.cpp:1518-1521`, `STMove.cpp:886`).

**Our handling**

- `PreMoveTime.Read` (`PreMoveTime.cs:15-23`) and `PreMoveTimeDecoder` (`PreMoveTimeDecoder.cs:11-19`) make
  `PreMoveTimeChangedEvent(ms)`.
- The subscriber is `WorldScene.ChangePreMoveTime` (`WorldScene.cs:88, 292-298`). It sets `PredictedMotion.PreMoveTime`
  in seconds. The default is 0.5 (`PredictedMotion.cs:73`).
- The value is used only in `MeasurePathLengthGap` (`PredictedMotion.cs:349-352`): `max(10, speed × (2 + preMoveTime))`.
  This is the tolerance before the client snaps to the server path when it sends a move.
