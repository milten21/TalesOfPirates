# AttributeSync (509)

`CMD_MC_SYNATTR` = `CMD_MC_BASE + 9` (`NetCommand.h:301`). Server to client. Our constant is `Opcode.AttributeSync`
(`src/Top.Legacy.Protocol/Packets/Opcode.cs:41`).

See [README.md](README.md) for the file path conventions.

The server sends this packet from 72 live call sites. It carries a sync type and the attributes that changed. Our
client maps 11 of the 74 attribute ids and ignores the sync type.

## Payload

| Field | Server write | Original client read | Our read |
|---|---|---|---|
| worldId | `WRITE_LONG(m_ID)` (`FightAble.cpp:933`) | `ReadLong` (`PacketCmd_SC.cpp:1218`) | `ReadUInt` (`AttributeSync.cs:30`) |
| syncType | `WRITE_CHAR(sSynType)` (`FightAble.cpp:1196`) | `ReadChar` (`:3466`) | `ReadByte` (`AttributeSync.cs:31`) |
| count | `WRITE_SHORT(changeNumClient)` (`FightAble.cpp:1197`) | `ReadShort` (`:3467`) | `ReadShort`; a count below 0 is rejected (`CharacterAttribute.cs:40-45`) |
| id, value (repeated) | `WRITE_CHAR(i)`, `WRITE_LONG(GetAttr(i))` for each changed `i < ATTR_CLIENT_MAX` (`FightAble.cpp:1200-1206`) | `ReadChar`, `(long)ReadLong` (`:3470-3471`) | `ReadByte`, then `ReadUInt` for ids 15-24 and `ReadInt` for others (`CharacterAttribute.cs:51-52`) |

- `ATTR_CLIENT_MAX = ATTR_ITEMC_STR = 74` (`ChaAttrType.h:255`). Only ids 0-73 go out.
- The server treats ids 15-24 as unsigned (`g_IsNosignChaAttr`, `ChaAttrType.h:260-266`). Our split at
  `LastExperienceId = 24` matches.
- If no attribute changed, the server sends nothing (`FightAble.cpp:927-929`).
- The range form `WriteAttr(start, end, type)` (`FightAble.cpp:1228-1239`) writes every id in the range, changed or
  not.

**Original client path.** `NetIF.cpp:87` calls `SC_SynAttribute` (`PacketCmd_SC.cpp:1216`), then `ReadChaAttrPacket`
(`:3463`). `NetSynAttr` (`NetProtocol.cpp:1920`) finds the character and makes a `CAttribSynchro`, which runs `_Exec`
(`HMAttack.cpp:399-603`).

- `CSynchroManage::FrameMove` runs it on the next frame. It waits behind earlier packets for the same character that
  did not run yet, for example pending damage (`HMSynchroObj.cpp:54-90`).
- An INIT packet runs at once.
- If the world id is not in the scene, the packet is dropped (`NetProtocol.cpp:1941-1946`).

## Sync types

`EAttrSynType`, `CompCommand.h:62-75`. There is no boat sync type. Boat attributes use the normal types.

| Value | Server name | Our `AttributeChangeReason` |
|---|---|---|
| 0 | `enumATTRSYN_INIT` | `Initial` |
| 1 | `enumATTRSYN_ITEM_EQUIP` | `Equipment` |
| 2 | `enumATTRSYN_ITEM_MEDICINE` | `ItemUse` |
| 3 | `enumATTRSYN_ATTACK` | `Combat` |
| 4 | `enumATTRSYN_TASK` | `Quest` |
| 5 | `enumATTRSYN_TRADE` | `Trade` |
| 6 | `enumATTRSYN_REASSIGN` | `PointReassign` |
| 7 | `enumATTRSYN_SKILL_STATE` | `SkillState` |
| 8 | `enumATTRSYN_AUTO_RESUME` | `Regeneration` |
| 9 | `enumATTRSYN_CHANGE_JOB` | `JobChange` |
| 10 | `enumATTRSYN_RECTIFY` | `Correction` |

The numbers match. `AttributeSyncDecoder.cs:27` casts the byte to the enum.

## Attribute ids

`ChaAttrType.h:11-100`. Ids 0-73 can go to the client.

| Id | Name | Id | Name |
|---|---|---|---|
| 0 | `ATTR_LV` | 25-30 | STR, DEX, AGI, CON, STA, LUK |
| 1 | `ATTR_HP` (boat: durability) | 31 | `ATTR_MXHP` |
| 2 | `ATTR_SP` (boat: supply) | 32 | `ATTR_MXSP` |
| 3 | `ATTR_TITLE` | 33, 34 | MNATK, MXATK |
| 4 | `ATTR_JOB` | 35-39 | DEF, HIT, FLEE, MF, CRT |
| 5 | `ATTR_FAME` (credit) | 40, 41 | HREC, SREC |
| 6 | `ATTR_AP` | 42 | ASPD |
| 7 | `ATTR_TP` | 43 | ADIS |
| 8 | `ATTR_GD` (gold) | 44 | `ATTR_MSPD` |
| 9 | `ATTR_SPRI` | 45 | COL |
| 10 | `ATTR_CHATYPE` | 46 | PDEF |
| 11 | `ATTR_SAILLV` | 47, 48, 49 | BOAT_CRANGE, BOAT_CSPD, BOAT_PRICE |
| 12, 13 | LIFELV, LIFETP | 50-73 | base values BSTR … BOAT_BCSPD |
| 14 | BOAT_BERTH | | |
| 15 | `ATTR_CEXP` | | |
| 16 | `ATTR_NLEXP` (EXP for the next level) | | |
| 17 | `ATTR_CLEXP` (EXP at the start of the current level) | | |
| 18-24 | sail and life EXP values | | |

## Receivers

The server does not filter attributes by viewer. It has no "visible to others" mask. The send function alone sets who
gets the packet.

| Function | Receivers | File |
|---|---|---|
| `SynAttr(type)` | Eyeshot with the owner (`NotiChgToEyeshot(pk, true)`). The only limit is `CanSeen`. All changed ids go to everyone. | `FightAble.cpp:925`, `Entity.cpp:176-272` |
| `SynAttrToSelf(type)` | Self | `FightAble.cpp:954` |
| `SyncBoatAttr(type, all)` | For each boat of the player: the boat's attributes, with the boat's world id, to self | `Character.cpp:6902-6921` |
| `SynAttrToEyeshot`, `SynAttrToUnit` (×2) | No callers | `FightAble.cpp:983, 1013, 1046` |

The only subset rule is in BeginSee: a non-player sends only LV, HP, MXHP, ASPD and MSPD (`FightAble.cpp:1211-1226`).

## Situations

"view" is `SynAttr` (eyeshot with the owner). "self" is `SynAttrToSelf` or `SyncBoatAttr`.

| Situation | Function | Type | To | Usual attributes |
|---|---|---|---|---|
| Kill EXP shared to other party members (not the attacker) | `Event_ChaDie` (`EventCalculate.cpp:153`) | ATTACK | view | CEXP; on level up also LV, CLEXP, NLEXP, AP, TP, MXHP, HP and more |
| Skill hit when the attacker is a boat: the human main character | `SkillTarEffect` (`FightAble.cpp:459`) | ATTACK | view | CEXP, level set |
| HP or SP above maximum after a hit or state | `RectifyAttr` (`FightAble.cpp:472-492`), called at `:453, 456, 461, 2487, 2508` | RECTIFY | view | HP, SP. Server defect: it sends only when HP is clamped. An SP-only clamp sets the value and sends nothing. |
| Skill cost when the caster is a boat: the main character | `SkillExpend` (`FightAble.cpp:1364`) | ATTACK | self | SP and other costs |
| Script EXP grant, GM `addexp` | `AddExpAndNotic` (`FightAble.cpp:2014-2020`), called at `CharScript.cpp:766, 822`, `CharacterSuperCmd.cpp:691` | TASK | view | CEXP, level set |
| Periodic state tick (poison, regen buff) on the holder | `OnSkillState` (`FightAble.cpp:2506`) | SKILL_STATE | view | HP, SP, MSPD, MXHP and more |
| The same tick: the state source and its main character | `FightAble.cpp:2486, 2491` | ATTACK | view | CEXP and others |
| Timed states removed | `RemoveOtherSkillState` (`FightAble.cpp:2530`) | SKILL_STATE | view | stats that the states changed, for example MSPD |
| State added: target | `AddSkillState` (`Character.cpp:4597`) | SKILL_STATE | view | MSPD, MXHP, DEF, HP and more |
| State added: source | `Character.cpp:4603` | ATTACK | view | source changes |
| State removed | `DelSkillState` (`Character.cpp:4707`) | SKILL_STATE | view | stats go back |
| Area state on enter or leave | `MgrUnit.cpp:106, 203` | SKILL_STATE | view | state stats |
| Ground state cell on enter or leave | `StateCell.cpp:44, 88` | SKILL_STATE | view | state stats |
| HP and SP regen, boat supply drain. `cha_timer` runs every 1 s; `Resume` every 5 ticks (`functions.lua:4100-4119`) | `OnScriptTimer` (`CharacterRun.cpp:323`) | AUTO_RESUME | view | HP, SP |
| Equip or unequip | `Cmd_UseEquipItem` (`CharacterCmd.cpp:914/916`) | ITEM_EQUIP | self + boats | MXHP, MXSP, ATK, DEF, MSPD, ASPD and more |
| Unfix item | `Cmd_UnfixItem` (`CharacterCmd.cpp:1240/1242`) | ITEM_EQUIP | self + boats | as equip |
| Remove item | `Cmd_RemoveItem` (`CharacterCmd.cpp:2265/2267`) | ITEM_EQUIP | self + boats | as equip |
| Equipment becomes valid or not valid | `SetEquipValid` (`Character.cpp:808/809`) | ITEM_EQUIP | self + boats | derived stats |
| Repair | `Cmd_ItemRepairAnswer` (`CharacterCmd.cpp:2889, 2899/2900`) | ITEM_EQUIP | self | derived stats |
| Lottery | `Cmd_ItemLotteryAnswer` (`CharacterCmd.cpp:3177`) | ITEM_EQUIP | self | gold, stats |
| Forge | `Cmd_ItemForgeAnswer` (`CharacterCmd.cpp:3691`) | ITEM_EQUIP | self | gold, stats |
| Client asks for a refresh (`CMD_CM_REFRESH_DATA`, own characters only) | `CharacterPrl.cpp:711` | ITEM_EQUIP | view | the flagged ids |
| Use a consumable (potion) | `Cmd_UseExpendItem` (`CharacterCmd.cpp:1049/1051`) | ITEM_MEDICINE | self + boats | HP, SP |
| Lua `RefreshCha` | `CharScript.cpp:5646/5648` | ITEM_MEDICINE | self + boats | check result |
| NPC shop sell | `SafeSale` (`Character.cpp:2727/2728`) | TRADE | view (+ boats self) | GD |
| NPC shop buy | `SafeBuy` (`Character.cpp:2937/2938`) | TRADE | view (+ boats self) | GD |
| Boat cargo sell | `SafeSaleGoods` (`Character.cpp:3099/3100`) | TRADE | view (+ boats self) | GD |
| Boat cargo buy | `SafeBuyGoods` (`Character.cpp:3239/3240`) | TRADE | view (+ boats self) | GD |
| Player-to-player trade | `CharTrade.cpp:2107-2110` | TRADE | view (+ boats self) | GD |
| Stall buy: buyer and seller | `CharStall.cpp:650-654, 842-843, 862-863` | TRADE | view (+ boats self) | GD |
| Guild bank gold | `CharacterPrl.cpp:175/176` | TRADE | view (+ boats self) | GD |
| Guild disband | `GameAppNet.cpp:1531` | TRADE | view | nothing: no attribute changed after `ResetChangeFlag`, so no packet goes out |
| Script gives or takes gold | `AddMoney` (`Character.cpp:3287`), `TakeMoney` (`:3303`) | TASK | view | GD |
| Script restores HP or SP | `RestoreHp` (`Character.cpp:4731`), `RestoreSp` (`:4747`), `RestoreAllHp` (`:4756`), `RestoreAllSp` (`:4765`), `RestoreAll` (`:4775`) | TASK | view | HP, SP |
| Script adds or takes an attribute | `AddAttr` (`Character.cpp:6947`), `TakeAttr` (`:6956`); sail EXP `CharScript.cpp:664, 831`; life EXP `:714, 840` | TASK | view | sail or life EXP and their level sets |
| Credit (FAME) | `AddMasterCredit` (`Character.cpp:7252`); `lua_AddCreditX` (`CharScript.cpp:5304`); `lua_DelCredit` (`:5354`); disciple delete (`CharacterPrl.cpp:1675`); `CMD_MM_ADDCREDIT` (`GameAppNet.cpp:1808`) | TASK | view | FAME |
| Paid world or trade chat | `GameAppNet.cpp:621, 657` | TASK | view | GD |
| GM set attribute | `DoGMCommand` (`CharacterSuperCmd.cpp:813/815`) | TASK | view (+ boats self) | any |
| Lua `SyncChar(cha, type)`. All 19 script calls use type 4, for example guild fee and boat level up (`exp_and_level.lua:60, 660`; `forge.lua:6853-6863`; `functions.lua:6626-7198`) | `lua_SyncChar` (`CharScript.cpp:4322`) | from script (4) | view | GD, FAME and others |
| Lua `SyncBoat(boat, 4)` | `CharScript.cpp:4297` | from script | self | boat LV, CEXP |
| Lua `SetMonsterAttr` (no script calls it) | `lua_gamectrl.h:2062` | TASK | view | any |
| Point reassign | `Cmd_ReassignAttr` (`CharacterCmd.cpp:2077/2080`) | REASSIGN | view (+ boats self) | AP, BSTR…BLUK, STR…LUK, derived stats |
| Learn skill | `LearnSkill` (`Character.cpp:4285/4287`) | REASSIGN | self + boats | TP, derived stats |
| Job change | `SetProfession` (`Character.cpp:5267/5269`); `lua_ChangeJob` (`CharScript.cpp:5677/5679`) | CHANGE_JOB | self + boats | JOB, derived stats |

Notes:

- A level up is not a separate send. `setAttr(ATTR_CEXP)` calls `CountLevel()` (`FightAble.cpp:510-511`). LV, CLEXP,
  NLEXP and the other level values go in the same packet as the EXP change.
- The `SynAttrToSelf` calls at `Character.cpp:856, 1024, 1066, 1109, 1157, 1214` are dead code inside
  `/* disabled pet slot */` blocks. `Guild.cpp:223` is a comment.
- When a player equips an item, uses a potion, changes job or learns a skill, the new MXHP, MSPD, HP or JOB go only to
  that player. Other clients do not get these values in 509.

## The same block in other packets

| Packet | Server writer | Content | Our reader |
|---|---|---|---|
| CharacterBeginSee (504) | `OnBeginSeen` (`Character.cpp:578-581`) | Player: ids 0-73. Monster or NPC: LV, HP, MXHP, ASPD, MSPD. Type INIT. To the one viewer. | `CharacterBeginSee.cs:79-80`, then `CharacterStateMapper.cs:23` |
| EnterMap (516) | `Cmd_EnterMap` (`CharacterCmd.cpp:266-267`) | `SetChangeFlag`, so ids 0-73. Type INIT. | `EnterMapReply.cs:158` |
| EnterMap boat list | `WriteItemChaBoat` (`CharacterPacket.cpp:542`, `Character.cpp:5444`) | Boat ids 0-73, INIT | `Boat.cs:48-49` |
| NotiAction SKILL_TAR (508) | `NotiSkillTarToEyeshot` (`FightAble.cpp:849, 862`) | Target block and source block, type ATTACK, to view. **Normal combat damage to HP reaches clients here, not in 509.** | Not read. See [actions.md](actions.md#skill_tar-4) |

The original client reads the block at `PacketCmd_SC.cpp:245, 276` (`SC_EnterMap`), `:593` (`SC_ChaBeginSee`) and
`:721` (`SC_AddItemCha`). All go through `NetSynAttr`.

## Our handling

**Decoder.** `AttributeSyncDecoder.Decode` (`src/Top.Client.LegacyBridge/Play/AttributeSyncDecoder.cs:14-29`) maps the
attributes. If no mapped id changed, it returns `null` and no event goes out (`:25`). Otherwise it publishes
`CharacterAttributesChangedEvent(WorldId, Reason, Attributes)`.

**Mapped ids** (`ChangedAttributesMapper.cs:52-66`, constants in `CharacterAttribute.cs:10-19`):

| Id | Field | Conversion |
|---|---|---|
| 0 LV | `Level` | int |
| 1 HP | `Health` | int |
| 2 SP | `Mana` | int |
| 4 JOB | `Job` | int |
| 8 GD | `Gold` | long |
| 15 CEXP | `Experience` | long (uint on the wire) |
| 16 NLEXP | `NextLevelThreshold` | long |
| 17 CLEXP | `LevelThreshold` | long |
| 31 MXHP | `MaxHealth` | int |
| 32 MXSP | `MaxMana` | int |
| 44 MSPD | `MoveSpeed` | `MoveSpeedMapper.ToMoveSpeed` (÷100, cm/s to m/s) |

**Subscriber.** `WorldScene.ChangeAttributes` (`WorldScene.cs:89, 300-328`):

1. If the player is not in the world yet (`IsStandingInWorld`, `:370-373`), it drops the event.
2. It finds the character with `_characterRegistry.Find(worldId)`. `Character.ChangeAttributes` (`Character.cs:103-120`)
   merges the fields that are not null into `State`. `Nameplate.ChangeCharacter` (`Nameplate.cs:58-84`) updates the
   name text (monster `Lv` or `???`) and the HP label, bar and colour.
3. For the own character: `Hud.ShowPlayer`, then `PlayerFrame.ShowCharacter` (`PlayerFrame.cs:51-95`) shows level, job
   name, HP numbers and bar, SP bar and the EXP line with percent. `FairyPlate.ShowFairy` also runs.
4. If `MoveSpeed` is present: `PredictedMotion.ChangeSpeed` (`PredictedMotion.cs:108`) for the own character,
   `CharacterMotion.ChangeSpeed` (`CharacterMotion.cs:117`) for others.

**The sync type is ignored.** No code in `src/Top.Client` reads `Reason`. Only tests read it
(`ServerConnectionTests.cs:1073, 1087, 1127-1139`).

**The original client handles types differently** (`HMAttack.cpp:399-603`):

| Condition | Original behaviour |
|---|---|
| SKILL_STATE | `CAttackEffect::ExecHarm` (`HMAttack.cpp:415-423`, `:135-260`): floating +/- HP numbers, the EXP message and the level-up effect. If HP ≤ 0, `ChaDied`. |
| Not INIT, main character | EXP gain system message with the level-80 multiplier (`:435-474`) |
| Not INIT, HP ≤ 0 | `CAttackEffect::ChaDied` (`:500-503`) |
| LV changed, not INIT | Sound 21, big "level up" text, effect 132, level help button. Others get effect 132. `RefreshLevel` runs for the level labels of others (`:486-489`, `:530-564`). |
| SAILLV changed, not INIT | "reborn level" text and effect (`:566-587`) |
| ITEM_MEDICINE, main character, HP or SP changed | Effect 133 and sound 23 (`:516-524`) |
| MSPD changed, main character | Camera follow speed update (`:506-509`) |
| JOB changed on a player | Team member job name update (`:488-499`) |
| Mine monster | Pose 1, 10 or 11 by HP fraction (`:591-601`) |

## Gaps

1. **Ignored ids.** The server sends these and the decoder drops them:
   - 3 TITLE, 5 FAME, 6 AP, 7 TP, 9 SPRI, 10 CHATYPE, 11 SAILLV, 12 LIFELV, 13 LIFETP, 14 BOAT_BERTH
   - 18-24 sail and life EXP
   - 25-30 STR…LUK, 33 MNATK, 34 MXATK, 35 DEF, 36 HIT, 37 FLEE, 38 MF, 39 CRT, 40 HREC, 41 SREC, 42 ASPD, 43 ADIS,
     45 COL, 46 PDEF, 47-49 boat values
   - 50-73 base values

   Credit and FAME syncs, and AP or TP after level up or reassign make no event. The
   TODO at `ChangedAttributesMapper.cs:67` records this.
2. **ASPD (42) is ignored**, but BeginSee sends it for every monster.
3. **The sync type is ignored.** None of the original behaviours in the table above exist: no floating numbers for
   SKILL_STATE HP changes, no death on HP ≤ 0, no level-up effect or text, no EXP message, no potion effect, no camera
   speed update, no team job update.
4. **No death from HP ≤ 0.** `DeathAction` is loaded (`Character.cs:23, 75`), but attribute changes never start it.
5. **Boat world ids.** `SyncBoatAttr` sends the attributes of each boat with the boat's world id, also for docked
   boats. `WorldScene` drops them when the boat is not in `_characterRegistry` (`:307-309`).
6. **Events before world entry are dropped** (`WorldScene.cs:302-305`). The original client also drops packets for
   unknown world ids.
7. **Nameplate level rule.** The monster "Lv / ???" rule uses the player level from nameplate setup. A player level up
   does not refresh other nameplates (TODO at `Nameplate.cs:67-68`). The original calls `RefreshLevel`.
8. **Queue order.** The original waits until earlier packets for the same character run (`HMSynchroObj.cpp:54-90`). We
   apply at once. This matters only when we add damage display.
9. **Combat HP is outside this packet.** HP from normal hits comes in NotiAction SKILL_TAR, which we do not read. In
   our client, HP changes only through RECTIFY, SKILL_STATE and AUTO_RESUME.
10. **Server behaviour to know** (not our defects): `RectifyAttr` does not send an SP-only clamp
    (`FightAble.cpp:472-492`). The guild disband sync sends nothing (`GameAppNet.cpp:1531`).
