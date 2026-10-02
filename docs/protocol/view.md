# Map and view packets

See [README.md](README.md) for the file path conventions. The original server files are GBK-encoded, so some comments
show as unreadable text. The code reads clearly.

## Summary

1. **Only the first EnterMap is handled.** `WorldScene` does not subscribe to `MapEnteredEvent`, and the
   character-select screen is gone after the scene change. The event is dropped (`ServerConnection.cs:183`). These
   situations do not work while the player is in the world:
   - map switch (portals, a boat that sails past a sea-map edge)
   - teleports (GM, NPC scripts, script `MoveCity`)
   - relive in town or relive at the same place
   - a map copy that closes
   - GM level change and transform (sent as CARRY)

   Before each of these, the server sends EndSee for every visible character, and we remove them. After the dropped
   EnterMap, BeginSee packets add characters at positions on the new map, but the client still shows the old map. Our
   own character stays at its old position.
2. **The client does not track which character the player controls.** The last field of EnterMap (the controlled
   character id), the `Boats` list and SeeType Switch are all dropped.
   - Boat launch: the boat is added as an "other" character. The human stays and stays under control.
   - Dock: the boat is removed.
   - Login at sea: the human is placed, and the boat is never shown.
3. **Kitbag readers find boat items with a fixed id list `{ 3988 }`** (`KitbagSlot.cs:9`). The server uses item type
   43 (`CharacterPacket.cpp:216`). Today 3988 "Vessel Deed" is the only type-43 item in
   `server/GameServer/resource/ItemInfo.txt`. Any other type-43 item makes the rest of the packet read
   wrong. This also goes against per-server content packs.
4. **The refused EnterMap branch never runs with this gate.** The gate disconnects and does not forward the packet
   (`ToGameServer.cpp:511-526`).

---

## EnterMap (516)

`CMD_MC_ENTERMAP` (`NetCommand.h:305`). Server to client.

**Reader:** `EnterMapReply.Read` (`src/Top.Legacy.Protocol/Messages/Play/EnterMapReply.cs:123-168`). Decoder
`EnterMapDecoder`, registered at `ServerConnection.Messages.cs:23`.

### Payload

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | code | ushort | Not 0: we return `Refused(code)` and stop (`:142-145`) |
| 2 | isAutoLocked | byte | |
| 3 | isKitbagLocked | byte | |
| 4 | SwitchMap body | — | `SwitchMap.ReadBody` (`SwitchMap.cs:54-66`): enterType byte (0 Edge, 1 Carry), isNewCharacter byte, mapName string, canTeam byte |
| 5 | imps | int | |
| 6 | actor | [ActorCreate](#actorcreate) | The player's **main** (human) character, look type LOOK_SELF |
| 7 | defaultSkillId | short | |
| 8 | skillBagType | byte | The server writes `enumSYN_SKILLBAG_INIT` |
| 9 | skillBag | [SkillSlot](#skillslot) list | |
| 10 | serverTime | int | Starts the skill-state block |
| 11 | skillStates | [SkillState](#skillstate) list | |
| 12 | attributeType | byte | |
| 13 | attributes | [CharacterAttribute](#characterattribute) list | All ids 0-73 |
| 14 | kitbagType | byte | Read into `EnterMapReply.Kitbag`, a [KitbagSync](#kitbagsync) |
| 15 | kitbagCapacity | short | Only when kitbagType is 0 |
| 16 | kitbag | [KitbagSlot](#kitbagslot) list | |
| 17 | shortcuts | 36 × [ShortcutSlot](#shortcutslot) | |
| 18 | boats | byte count × [Boat](#boat) | `EnterMapReply.cs:204-215` |
| 19 | mainCharacterId | uint | The **controlled** character id (`pCCtrlCha->GetID()`) |

The server write order (`CharacterCmd.cpp:194-278`) matches field for field. The server also appends chLogin, player
count and player address (`:282-284`). The gate strips them (`ToGameServer.cpp:529-540`).

- **Map name:** the packet carries the `l_map` argument (`:212`). For a new character, the map that loads comes from a
  random birth point (`:84-90`), so the two names can differ.
- **Refused variant:** written at `CharacterCmd.cpp:305-312` and at `GameAppNet.cpp:512-519` (player creation fails).
  The gate `MC_ENTERMAP` sees retcode not 0 and calls `Disconnect(..., -33)`. The forward call is commented out
  (`ToGameServer.cpp:511-526`). **The client never receives this variant.**

### When the server sends it

The gate forwards every EnterMap through `ToGameServer::MC_ENTERMAP` (`:489`). `chLogin` sets the enter type: 0 gives
Carry (default, `:32`), not 0 gives Edge (`:116`). The default `chLogin` is 1 (`Character.h:167`).

| Situation | Path | enterType | Other values |
|---|---|---|---|
| First login (character select to world) | Gate `ToClient.cpp:1058` `EnterMap(...,0,...)`, then `GameAppNet.cpp:535` `Cmd_EnterMap(chLogin=0)` | **Carry (1)** | isNewCharacter = 1 when LV is 0 (`CharacterCmd.cpp:50-52`). If the player logged out on a boat, `BoatEnterMap` runs (`:60-75`) and mainCharacterId is the boat |
| Map switch to another map (same or other game server) | `SwitchMap` different-map branch sends `CMD_MT_SWITCHMAP` (`Character.cpp:441`). Gate `MT_SWITCHMAP` (`ToGameServer.cpp:413-465`) sends `EnterMap(...,1,...)` | **Edge (0)** | Before it, `GoOut` on the old map (`Character.cpp:363`) sends EndSee |
| Teleport on the same map | `SwitchMap` same-map player branch calls `Cmd_EnterMap` with chLogin 1 (`Character.cpp:390`) | Edge | |
| Target map not reachable, not a death switch | Gate sends `CMD_MC_SYSINFO` and re-enters the **source** map (`ToGameServer.cpp:451-462`) | Edge | Map name is the source map |
| Target map not reachable, death switch | Gate disconnects (`:463-466`) | — | No packet |
| Relive at origin | `OnDie` runs `GoOut` (`Character.cpp:1521`), then `SwitchMap` on the same map (`:1532`) | Edge | |
| Relive in city or on the relive map | `OnDie` calls `BackToCity(true)` (`:1491, 1504, 1542`). `BackToCity` (`:1906`) runs `GoOut` and `SetToMainCha`, then `SwitchMap` (`:1928`) | Edge | Cross-server: die flag 1 (`:451`) |
| Map copy closes | `SubMap::ClearPlayerCha` / `ClearAllCha`, then `BackToCityEx` (`SubMap.cpp:1586-1589, 1718-1721`, `Character.cpp:1930-1948`) | Edge | |
| GM level set, transform | `lua_SetGmLv` (`CharScript.cpp:5835`), `lua_TransformCha` (`:5971`): `Cmd_EnterMap(...,0)` | **Carry** | Sent while the character is in the world, with no `GoOut` before it |

Callers of `SwitchMap`: the map portal event `ExecuteEvent` (`Character.cpp:4801`); a boat that sails past a sea-map
edge, `AfterStepMove` (`:1617`); `MoveCity` (`:1832`, called by `lua_MoveCity` `NpcScript.cpp:508` and
`ProcessTeamMsg` `GameAppNet.cpp:1196`); NPC script `lua_MoveTo` (`NpcScript.cpp:469`); GM command
(`CharacterSuperCmd.cpp:100`); GM summon and goto (`GameAppNet.cpp:1669, 1713`).

### Our handling

`EnterMapDecoder.Decode` (`src/Top.Client.LegacyBridge/Play/EnterMapDecoder.cs:19-27`):

- **Refused:** `BeginPlayFailedEvent`. Only `CharacterSelectScreen` subscribes (`CharacterSelectScreen.cs:168`, handler
  `:513`). This never runs with this gate.
- **Success:** `MapEnteredEvent(MapName, (EnterType)EnterType, CharacterStateMapper.ToCharacterState(Actor, Attributes),
  Bag, IsBagLocked, Equipment)`. `IsBagLocked` is `IsKitbagLocked`, the start state of the Inventory lock (see
  [KitbagLock](items.md#kitbaglock-31--kitbagunlock-32--lock-state-553)). `Bag` is `BagSyncMapper.ToBagSync(Kitbag)` (see
  [KITBAG](actions.md#kitbag-6-kitbagtmp-28-bank-26-guildbank-30)). `Equipment` is
  `EquipmentMapper.ToEquipment(Actor.Look)`: the 34 look links as `BagItem`s, with the 9999 and 9998 link markers as
  `null` (see [LOOK](actions.md#look-5)). `WorldScene.Setup` makes the `CharacterInventory` from the event, so the
  Inventory's equipment slots start from the actor's look.
  1. The only subscriber is `CharacterSelectScreen.CompleteBeginPlay` (`:167`, `:499-511`). It pauses the drain,
     stores `EnteredMap` and emits `MapEntered`.
  2. `CharacterSelectScene.GoToWorld` (`CharacterSelectScene.cs:216-224`) calls `router.GoToWorld(new WorldPayload(entered))`.
  3. `WorldScene.EnterMap` (`WorldScene.cs:156-186`) shows the loading screen only for Carry. `LoadMap` (`:188-239`)
     loads the map by name, places the player at `Character.Position`, and sets up the HUD, camera and movement. Then
     it resumes the drain.
- **Every later EnterMap** gives a `MapEnteredEvent` with no subscriber, and it is dropped.

`CharacterStateMapper` (`CharacterStateMapper.cs:25-49`) keeps:

- WorldId, ControlType, TypeId (from `ActorCreate.CharacterId`, which is the server's `GetCat()`), Name, Position,
  Angle, ExistState, TeamLeaderId, guild id, name, motto and permission
- Level, HP, MaxHP, MP, MaxMP, Job, Exp, the two level thresholds, Gold and MoveSpeed (`ChangedAttributesMapper.cs`)
- the character look (hair, 34 item ids, fairy) or the ship look (BoatId, Header, Engine)

### Gaps

- Every EnterMap after the first is dropped. After it, the map and the player position in our client do not match the
  server.
- Read and then dropped (each has a TODO): IsAutoLocked, Imps, IsNewCharacter, CanTeam,
  DefaultSkillId, SkillBagType, SkillBag, ServerTime, SkillStates, AttributeType, Shortcuts, **Boats**,
  **MainCharacterId**.
- Imps is the second amount of the original inventory form (`UIEquipForm.cpp:1626-1633`), which the design calls
  crystal. The inventory shows gold only for now. The original also updates the count from `CMD_MC_UPDATEIMP` 611
  (sent at `Character.cpp:7613` and `CharTrade.cpp:2117, 2122`; read at `NetIF.cpp:139`) and from the item-mall buy
  reply (`PacketCmd_SC.cpp:2754`).
- The kitbag sync type is always INIT here and is dropped. The kitbag gaps are under
  [KITBAG](actions.md#kitbag-6-kitbagtmp-28-bank-26-guildbank-30).
- The mapper drops all attributes except the 11 it maps. It also drops these actor fields: CommunityId and
  CommunityName (really the main character id and name), GmLevel, Handle, Motto, Icon, StallName, Radius, IsPlayer,
  Side, EntityEvent, PkControl, AppendLooks, and for boats BoatLook.PoseId, Body, Cannon and Equipment.
- `ExistState` is in `CharacterState`, but nothing in `Top.Client` reads it.
- **Login at sea:** the controlled character is the boat in `Boats` that matches `MainCharacterId`. We create the
  human from `Actor`, and the boat is never created.
- The fixed boat item id list, and the `WriteBoat` count bug under [Boat](#boat).

---

## CharacterBeginSee (504)

`CMD_MC_CHABEGINSEE` (`NetCommand.h:277`). Server to client.

**Reader:** `CharacterBeginSee.Read` (`Messages/Play/CharacterBeginSee.cs:66-85`). Decoder
`CharacterBeginSeeDecoder`, registered at `ServerConnection.Messages.cs:27`.

### Payload

| # | Field | Type | Condition |
|---|---|---|---|
| 1 | seeType | byte | 0 New, 1 Switch |
| 2 | actor | [ActorCreate](#actorcreate) | Written with LOOK_OTHER |
| 3 | npcShow | [NpcShow](#npcshow-leaninfo-face) | Always present (0/0 for a non-NPC) |
| 4 | poseState | short | |
| 5 | lean | [LeanInfo](#npcshow-leaninfo-face) | Only when poseState is 1 |
| 6 | seat | [Face](#npcshow-leaninfo-face) | Only when poseState is 2 |
| 7 | attributeType | byte | INIT |
| 8 | attributes | [CharacterAttribute](#characterattribute) list | **Player:** all ids 0-73 (`FightAble.cpp:1228-1239`). **Monster or NPC:** 5 ids: LV, HP, MXHP, ASPD, MSPD (`FightAble.cpp:1211-1226`) |
| 9 | serverTime | int | |
| 10 | skillStates | [SkillState](#skillstate) list | |

Server writer: `CCharacter::OnBeginSeen` (`Character.cpp:507-588`).

- seeType is Switch when both characters belong to the same player (`:517-520`).
- An event entity puts its state in the top 4 bits of eventId (`:522-532`).
- On the "blind chaos" PK map (`g_Config.m_bBlindChaos`), the name, motto and guild strings are empty and the gear is
  replaced (`CharacterPacket.cpp:19-49, 339-418`). The layout does not change.

### When the server sends it

The server sends it only to the character that the player controls (`Character.cpp:468-474, 509-510`), and only if
`CanSeen` passes (`EyeshotCell.cpp:62-68`).

| Situation | Path | seeType |
|---|---|---|
| We enter a map (login, switch, relive, teleport). We see everyone in the 3×3 eyeshot cells | `SubMap::Enter` (`SubMap.cpp:754-766`), from `Cmd_EnterMap` (`CharacterCmd.cpp:291`), after EnterMap | New |
| Another character enters the map near us: monster or NPC spawn, monster respawn, a player who logs in or teleports, an event entity | `SubMap::ChaSpawn` (`SubMap.cpp:211`), `CNpcSpawn::Load` (`EntitySpawn.cpp:328`), `CEventEntity` (`EventEntity.cpp:63`), monster respawn through `COutMapCha::ExecTimeCha` step 2 and `SwitchMap` non-player branch (`SubMap.cpp:2097`, `Character.cpp:376-382`) | New |
| We or they move into a new eyeshot cell | `SubMap::MoveTo` (`SubMap.cpp:1459-1463`) | New |
| A hide or show state changes visibility (stealth) | `AddSkillState` / `DelSkillState`, `SubMap::RefreshEyeshot`, `EyeshotCell::RefreshEyeshot` (`Character.cpp:4559, 4565, 4688`; `EyeshotCell.cpp:132-157`) | New |
| A team member joins while one of us is hidden | `CGameApp::RefreshTeamEyeshot` (`GameAppNet.cpp:1239-1320`) | New |
| **We launch a boat.** We see our own boat | `BoatLaunch` (`Character.cpp:5733`), `BoatEnterMap` (`:5708`), `SkillPopBoat`, then `SubMap::Enter(boat)` (`FightAble.cpp:1624`) | **Switch** |
| **We dock.** We see our own human again | `BoatBerth` (`Character.cpp:5676`), `SkillOutBoat`, then `Enter(mainCha)` (`FightAble.cpp:1702`) | **Switch** |
| Other players see our boat launch or our dock | Same paths as the two rows above | New |

### Our handling

`CharacterBeginSeeDecoder` (`Play/CharacterBeginSeeDecoder.cs:19-23`) returns
`CharacterAppearedEvent(ToCharacterState(Actor, Attributes), (SeeType)SeeType)`.

The subscriber is `WorldScene.AddSeenCharacter` (`WorldScene.cs:84, 246-257`):

- It ignores the event if the world is not ready, or if the id is our own.
- Otherwise it calls `CharacterRegistry.Remove(worldId)`, then `Add(state, player)`. `Add` builds a model with a
  nameplate and a `FollowedMotion` (`CharacterRegistry.cs:31-50`).
- Events that arrive while the map loads wait in the queue: `PauseDrain` at `CharacterSelectScreen.cs:506`,
  `ResumeDrain` at `WorldScene.cs:184`.
- New and Switch get the same handling. Nothing reads `SeeType`.

### Gaps

- **SeeType is not used.** The original client uses Switch to show again the player's own known character
  (`NetProtocol.cpp:505-521`).
  - Boat launch: we add the boat as a second character with `FollowedMotion`. Our human stays under control.
  - Dock: the Switch BeginSee has our own id, and we ignore it.
- Read and dropped (each has a TODO): NpcShow, PoseState, Lean, Seat, AttributeType, ServerTime, SkillStates.
- The same actor fields and attributes are dropped as in EnterMap. A monster sends no Mana, MaxMana or Job, so the
  mapper sets them to 0. ASPD is dropped.
- ExistState is not used. A dead character (WITHERING = 5) that comes into view looks alive.

---

## CharacterEndSee (505)

`CMD_MC_CHAENDSEE` (`NetCommand.h:278`). Server to client.

**Reader:** `CharacterEndSee.Read` (`Messages/Play/CharacterEndSee.cs:18-28`). Decoder `CharacterEndSeeDecoder`,
registered at `ServerConnection.Messages.cs:28`.

**Payload:** seeType byte (0 New, 1 Switch), worldId uint. There are no conditional fields.

Server writer: `CCharacter::OnEndSeen` (`Character.cpp:590-624`). seeType is Switch only when both characters belong
to the same player **and** the removed character's `ATTR_CHATYPE` is PLAYER (`:601-604`). The server also clears the
talk-NPC mission state for the viewer (`:609-618`).

### When the server sends it

The same control and `CanSeen` rules apply (`EyeshotCell.cpp:103-114`).

| Situation | Path | seeType |
|---|---|---|
| A character moves out of our eyeshot cells, or we move away from it | `SubMap::MoveTo` (`SubMap.cpp:1453-1457`) | New |
| **We leave the map.** We get EndSee for every character in view before the next EnterMap: map switch, teleport, relive, map copy clear | `SubMap::GoOut` (`SubMap.cpp:851-874`) from `SwitchMap` (`Character.cpp:363`), `OnDie` (`:1521`), `BackToCity` (`:1909`), `BackToCityEx` (`:1933`) | New |
| Another player near us leaves the map (switch, teleport, relive) | Same paths as the row above | New |
| A monster dies. After the wither time (default 10 s, `Entity.cpp:26`) it leaves the map. Later it respawns with BeginSee | `CCharacter::Die` queues it (`Character.cpp:1810`), then `COutMapCha::ExecTimeCha` step 1 runs `GoOut` (`SubMap.cpp:2062`) | New |
| A player dies | No EndSee. The body stays until relive, then `GoOut` sends EndSee as above | — |
| A character is freed: player logout or kick, summon lifetime ends, NPC deleted, GM kill | `CCharacter::Finally` runs `GoOut` (`Character.cpp:226`). Paths: `GoOutGame`, then `CPlayer::Finally` frees the main character and boats (`GameApp.cpp:1065-1083`, `Player.cpp:98-116`); `CharacterRun.cpp:46`; `Character.cpp:2253`; `CharacterSuperCmd.cpp:631, 647`. In offline mode (`m_bOfflineMode`) no EndSee is sent (`GameApp.cpp:1070-1073`) | New |
| We send EndPlay. We get one EndSee for each character in view before EndPlayReply | `TM_GOOUTMAP` from the gate `CM_ENDPLAY` | New |
| Summons and passengers are cleared | `CConjureMgr::FreeAll` / `CPassengerMgr::FreeAll` (`AttachManage.cpp:128, 280`) | New |
| A hide state starts, or a team member leaves while one of us is hidden | `EyeshotCell::RefreshEyeshot` (`EyeshotCell.cpp:150-152`), `RefreshTeamEyeshot` (`GameAppNet.cpp:1266-1272, 1311-1317`) | New |
| **We launch a boat.** Our own human leaves the map | `SkillInBoat`, then `GoOut(mainCha)` after `SetCtrlCha(boat)` (`FightAble.cpp:1646-1655`) | **Switch** to us, New to others |
| **We dock.** Our own boat leaves the map | `SkillPushBoat`, then `GoOut(boat)` (`FightAble.cpp:1736`), from `BoatBerth` (`Character.cpp:5699`) | **Switch** to us, New to others |

### Our handling

`CharacterEndSeeDecoder` (`Play/CharacterEndSeeDecoder.cs:19-21`) returns
`CharacterDisappearedEvent(WorldId, (SeeType)SeeType)`.

The subscriber is `WorldScene.RemoveCharacter` (`WorldScene.cs:85, 259-265`). If the world is ready and the id is not
our own, it calls `CharacterRegistry.Remove` (`CharacterRegistry.cs:65-76`), which frees the node and its
`FollowedMotion`. SeeType is not read.

### Gaps

- **Switch is treated as destroy.** The original client only hides the character (`NetProtocol.cpp:677-689`).
  - Dock: our own boat is freed. This is correct on screen, but control does not go back to the human.
  - Launch: the Switch EndSee for our own human is ignored. The human stays on screen and stays in control.
- Before a map switch or relive we remove every character. The EnterMap after it is dropped (see EnterMap).
- The original client also closes an open booth and clears the target frame when the target leaves view
  (`PacketCmd_SC.cpp:606-620`). We have no booth or target UI yet. New UI must handle this.
- No field is skipped.

---

## Embedded blocks

### ActorCreate

`Messages/Characters/ActorCreate.cs:121-159`. Server writer: `CCharacter::WriteBaseInfo` (`CharacterPacket.cpp:12-89`).

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | characterId | uint | Server `GetCat()`, the character type id |
| 2 | worldId | uint | |
| 3 | communityId | uint | Main character id, or its own id for a non-player |
| 4 | communityName | string | Main character name, or "" |
| 5 | gmLevel | byte | |
| 6 | handle | int | |
| 7 | controlType | byte | `ATTR_CHATYPE` |
| 8 | name | string | |
| 9 | motto | string | |
| 10 | icon | short | |
| 11 | guildId | int | |
| 12 | guildName | string | |
| 13 | guildMotto | string | |
| 14 | guildPermission | int | |
| 15 | stallName | string | |
| 16 | existState | short | |
| 17 | x, y | int, int | centimetres |
| 18 | radius | int | |
| 19 | angle | short | |
| 20 | teamLeaderId | uint | |
| 21 | isPlayer | byte | |
| 22 | side | byte | |
| 23 | event | [EntityEvent](#entityevent) | |
| 24 | look | [LookInfo](#lookinfo) | |
| 25 | pkControl | byte | |
| 26 | appendLooks | [AppendLook](#appendlook) | |

### EntityEvent

`EntityEvent.cs:24-36`. entityId int, entityType byte (1 character, 2 item), eventId ushort (the top 4 bits carry the
event state for event entities), name string. Server: `Entity::WriteEventInfo` (`Entity.cpp:744-751`) and
`CEvent::WriteInfo` (`Event.h:53-57`).

### LookInfo

`LookInfo.cs:73-97`. syncType byte (0 full, 1 item data), typeId short, isBoat byte.

- isBoat is 1: [BoatLook](#boatlook) follows.
- Otherwise: hairId short, then 34 × [ItemGrid as a look link](#itemgrid-as-a-look-link).

Server: `WriteLookData` (`CharacterPacket.cpp:302-487`). isBoat is 1 only when `ATTR_CHATYPE == PLAYER && IsBoat()`.
A character with no look writes zero ids (`:305-322`), and our reader handles that. In EnterMap and BeginSee the sync
type is always full. The same block in NotiAction LOOK can have sync type 1: see
[actions.md](actions.md#look-5) for a length defect in that case.

### BoatLook

`BoatLook.cs:34-49`. Seven shorts: poseId, boatId, header, body, engine, cannon, equipment.

### ItemGrid as a look link

`ItemGrid.cs:107-148`. id short, dbId uint, needLevel short.

- id is 0: the grid stops here.
- syncType is 1: endure short, energy short, isValid byte, tradable byte, expiration int.
- Otherwise: count, endure ×2, energy ×2 (shorts), forgeLevel byte, isValid byte, tradable byte, expiration int, flag
  byte. If the flag is not 0: forge int, instanceId int, [ItemInstanceAttribute](#iteminstanceattribute).

This matches the server at `CharacterPacket.cpp:425-484`. In the blind-chaos branch, the flag is 0 for LOOK_OTHER
(`:391-395`).

The id is not always an item. The server writes 9999 (`enumEQUIP_BOTH_HAND`) into the other hand links of a
two-handed item, and 9998 (`enumEQUIP_TOTEM`) into the other links a body item takes (`CharacterCmd.cpp:870-884`,
`CompCommand.h:160-161`). The rest of the grid follows as for an item. The capture of `T1` on 2026-09-28 has 9999 in
link 6 while it holds a Newbie Sword (id 9) in link 9.

### ItemGrid as kitbag content

`ItemGrid.cs:192-227`. id short. id ≤ 0: the slot is empty. Otherwise: dbId, needLevel, the same state fields as the
look link, then **boatWorldId uint only if the id is in the boat item id list**, then forge int, instanceId int,
[ItemInstanceAttribute](#iteminstanceattribute).

Server: `WriteKitbag` (`CharacterPacket.cpp:168-231`). The server writes boatWorldId when the item **type** is 43
(`:216-223`). Our reader uses the fixed id list `{ 3988 }` (`KitbagSlot.cs:9`). See the summary.

### ItemInstanceAttribute

`ItemInstanceAttribute.cs:27-43`. A flag byte (read in `ItemGrid.cs:300-302`). If not 0: 5 × (id short, value short).
This matches `defITEM_INSTANCE_ATTR_NUM` = 5.

### KitbagSync

`KitbagSync.cs`. The `WriteKitbag` block (`CharacterPacket.cpp:168-233`): type byte, capacity short only when the type
is 0 (INIT), then the [KitbagSlot](#kitbagslot) list. EnterMap, [Boat](#boat), NotiAction KITBAG and KITBAGTMP, and the
[TemporaryKitbagSync](items.md#temporarykitbagsync-35--reply-554) reply all carry it.

### KitbagSlot

`KitbagSlot.cs`. Repeated (gridId short, content) until gridId < 0. The server ends the list with -1
(`CharacterPacket.cpp:230`).

### AppendLook

`AppendLook.cs:21-37`. 4 × (lookId short, then isValid byte only if lookId is not 0). Server: `WriteAppendLook`
(`CharacterPacket.cpp:489-509`), `defESPE_KBGRID_NUM` = 4.

### CharacterAttribute

`CharacterAttribute.cs:33-56`. count short, then count × (id byte, value). The value is uint for ids 15-24 and int for
others. The server writes every value as a 4-byte LONG (`FightAble.cpp:1192-1239`). See [attributes.md](attributes.md)
for the ids.

### SkillSlot

`SkillSlot.cs:69-143`. count short. count ≤ 0: the list is empty. Each entry: id short, state byte, level byte, useSp
short, useEndure short, useEnergy short, resumeTime int, rangeType short. If rangeType is not 0, 3 more shorts follow.

Server: `WriteSkillbag` (`CharacterPacket.cpp:101-166`). If a skill grid or its table data is missing, the server stops
writing early (`:148-153`), and the rest of the packet is wrong. This is a server bug.

### SkillState

`SkillState.cs:26-45`. The caller reads the serverTime int. Then count byte, and count × (id byte, level byte, duration
int, startTick int). Server: `WriteSkillState` (`FightAble.cpp:1170-1190`).

### ShortcutSlot

`ShortcutSlot.cs:21-37`. 36 × (type byte, gridId short). Server: `WriteShortcut` (`CharacterPacket.cpp:511-518`).
`SHORT_CUT_NUM` = 3 × 12.

### Boat

`Messages/Play/Boat.cs:40-57`. [ActorCreate](#actorcreate), attributeType byte, [CharacterAttribute](#characterattribute)
list, [KitbagSync](#kitbagsync), serverTime int,
[SkillState](#skillstate) list.

Server: `WriteItemChaBoat` (`CharacterPacket.cpp:538-545`). The order matches.

Server bug: `WriteBoat` (`:520-536`) writes `GetNumBoat()` as the count, but skips a null boat with `continue`. The
count then does not match the boats that follow, and our reader goes out of alignment.

### NpcShow, LeanInfo, Face

Used only by BeginSee. All three match `Character.cpp:543-573`.

- NpcShow (`NpcShow.cs:18-28`): showType byte, missionState byte.
- LeanInfo (`LeanInfo.cs:27-41`): state byte, pose int, angle int, x int, y int, height int.
- Face (`Face.cs:18-28`): angle short, pose short.
