# Opcode inventory

This file lists every opcode that the original client sends or receives, with the server status and our status. It is a
map for planning, not deep research: each row gives one or two sites, not all situations. The deep research for the
implemented opcodes is in the files that [README.md](README.md) lists.

The inventory was made on 2026-09-27 against `sources/` and `server/` of the reference repo and the working tree
of `prototype/godot`.

## How to read the tables

- File paths follow the conventions in [README.md](README.md#conventions). In addition:
  - All client file names (`NetIF.cpp`, `PacketCmd_CS.cpp`, `NetPkChat.cpp`, `NetPkGuild.cpp` and the others) are under
    `sources/Client/src/`.
  - `GateServer/X.cpp` is under `sources/Server/GateServer/src/`, and `GroupServer/X.cpp` is under
    `sources/Server/GroupServer/src/`.
  - `MisSdk/*.lua`, `shop/init.lua` and `initial.lua` are under `server/GameServer/resource/script/`.
    `Addons/*.lua` is under `server/GameServer/`. The game server runs these Lua scripts, so a Lua site
    counts as a game server site.
- **Opcode** is the value that goes on the wire. It is the base plus the offset from `NetCommand.h`.
- **Direction** gives the process that builds the packet (S→C) or handles it (C→S). "gate → group" means the gate
  forwards the packet to the group server. "S→C unknown" means that no sender was found, so the process is not known.
- **Server**: for S→C, "sent" gives a `WRITE_CMD` / `WriteCmd` site. For C→S, "handled" gives a `case` site. "No sender
  found" and "no handler found" mean that a search of `sources/Server` and the server Lua scripts found no site. A
  site that exists is not proof that the server calls it.
- **Original client**: for S→C, "reads" gives the `case` in the client dispatch. For C→S, "sends" gives the site that
  writes the opcode.
- **Ours**: "yes" links to the section that documents our reader or writer
  (`src/Top.Legacy.Protocol/Packets/Opcode.cs`).
- A sub-command is an opcode value that goes inside the payload of another packet, not as the packet opcode. 302, 303,
  322 go inside 301. 309 and 51 go inside 308. 812-819 go inside 811.

## Totals

| Direction | Opcodes | Names | Implemented | No sender or handler |
|---|---|---|---|---|
| Client → game (`CMD_CM_*`, 0-499) | 129 | 131 | 15 | 13 (no handler found) |
| Game → client (`CMD_MC_*`, 500-999) | 182 | 183 | 18 | 30 (no sender found) |
| Client → group (`CMD_CP_*`, 6000-6499) | 34 | 34 | 1 | 4 (no handler found) |
| Group → client (`CMD_PC_*`, 5000-5499) | 37 | 37 | 1 | 4 (no sender found) |
| Gate → client (`CMD_TC_*`, 7500-7999) | 1 | 1 | 0 | 0 (no sender found) |
| **All** | **383** | **386** | **35** | **51** |

"Opcodes" counts values. "Names" counts defines. Three values have two names each (97, 301 and 600).

| Feature group | Opcodes | Implemented | No sender or handler |
|---|---|---|---|
| [Session](#session) | 35 | 21 | 2 |
| [Map and view](#map-and-view) | 14 | 3 | 2 |
| [Actions and movement](#actions-and-movement) | 5 | 5 | 0 |
| [Combat](#combat) | 4 | 0 | 0 |
| [Character: attributes, skills, look](#character-attributes-skills-look) | 16 | 1 | 2 |
| [Items and kitbag](#items-and-kitbag) | 15 | 5 | 1 |
| [Crafting: forge, repair, life skills](#crafting-forge-repair-life-skills) | 27 | 0 | 1 |
| [Item mall](#item-mall) | 16 | 0 | 2 |
| [NPC trade](#npc-trade) | 14 | 0 | 3 |
| [Player trade](#player-trade) | 17 | 0 | 0 |
| [Stall](#stall) | 13 | 0 | 1 |
| [Quests and NPC dialogue](#quests-and-npc-dialogue) | 25 | 0 | 5 |
| [Boat](#boat) | 18 | 0 | 3 |
| [Chat](#chat) | 27 | 0 | 2 |
| [Notices](#notices) | 8 | 0 | 1 |
| [Team and volunteer list](#team-and-volunteer-list) | 25 | 0 | 2 |
| [Guild](#guild) | 28 | 0 | 2 |
| [Friends](#friends) | 21 | 0 | 8 |
| [Master and apprentice](#master-and-apprentice) | 14 | 0 | 4 |
| [GM mail](#gm-mail) | 5 | 0 | 0 |
| [GM tools](#gm-tools) | 7 | 0 | 4 |
| [Other](#other) | 29 | 0 | 6 |

## Findings

- Three values have two names. The server reads each value as one name only:
  - 97: `CMD_CM_RANK` and `CMD_CM_CAPTAIN_CONFIRM_ASR`. The client sends both. The server `case CMD_CM_RANK`
    (`CharacterPrl.cpp:33`) receives the captain answer too.
  - 301: `CMD_CM_REQUESTNPC` and `CMD_CM_REQUESTTALK`. Only `REQUESTTALK` is used.
  - 600: `CMD_CM_ITEM_UNLOCK_ASR` and `CMD_MC_LIFESKILL_BGING`. Only `LIFESKILL_BGING` is used. The item unlock result
    goes as 595 (`CMD_MC_ITEM_UNLOCK_ASR`).
- `CMD_CM_ITEM_LOCK_ASR` (599) has a `CM` name but a value in the server-to-client range. The server sends it and the
  client reads it. The table lists it as S→C.
- The game server sends many NPC, quest, shop and black market packets from Lua only (`MisSdk/NpcSdk.lua`,
  `MisSdk/MissionSdk.lua`, `shop/init.lua`). A search of the C++ code alone reports them as dead.
- The handler for `CMD_CM_CHEAT_CHECK` (52) exists, but its body is commented out (`CharacterPrl.cpp:1894`).
- The server sends these opcodes, but the original client does not read them: 604, 843, 844, 852, 879, 5015.

---

## Session

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 15 | `CMD_CM_PING` | C→S game | Answer to game ping 515. | handled: `CharacterPrl.cpp:229` | sends: `PacketCmd_SC.cpp:451` | [yes](session.md#ping-515-server-to-client--pingreply-15-client-to-server) |
| 17 | `CMD_CM_CHECK_PING` | C→S game | Answer to check ping 537. | handled: `CharacterPrl.cpp:266` | sends: `PacketCmd_SC.cpp:470` | [yes](session.md#checkping-537-server-to-client--checkpingreply-17-client-to-server) |
| 101 | `CMD_CM_GAME_REQUEST_PIN` | C→S game | Send the PIN that 613 asks for. | handled: `CharacterPrl.cpp:1467` | sends: `PacketCmd_CS.cpp:1360` | no |
| 346 | `CMD_CM_CREATE_PASSWORD2` | C→S gate → group | Create the security code. | handled: `ToClient.cpp:452`, `ToClient.cpp:715` | sends: `PacketCmd_CS.cpp:214` | [yes](session.md#createsecondpassword-346--reply-941) |
| 347 | `CMD_CM_UPDATE_PASSWORD2` | C→S gate → group | Change the security code. | handled: `ToClient.cpp:453`, `ToClient.cpp:718` | sends: `PacketCmd_CS.cpp:224` | [yes](session.md#changesecondpassword-347--reply-942) |
| 355 | `CMD_CM_SEND_PRIVATE_KEY` | C→S gate | Send the encrypted AES key. | handled: `ToClient.cpp:405` | sends: `ProCirculateCS.cpp:424` | [yes](session.md#privatekey-355-client-to-server) |
| 431 | `CMD_CM_LOGIN` | C→S gate → group | Log in with account and password. | handled: `ToClient.cpp:446`, `ToClient.cpp:696` | sends: `ProCirculateCS.cpp:447` | [yes](session.md#login-431--loginreply-931) |
| 432 | `CMD_CM_LOGOUT` | C→S gate → group | Log out. | handled: `ToClient.cpp:284`, `ToClient.cpp:447` | sends: `ProCirculateCS.cpp:468` | [yes](session.md#logout-432-client-to-server) |
| 433 | `CMD_CM_BGNPLAY` | C→S gate → group | Enter the world with the selected character. | handled: `ToClient.cpp:448`, `ToClient.cpp:703` | sends: `ProCirculateCS.cpp:475` | [yes](session.md#beginplay-433--beginplayreply-933) |
| 434 | `CMD_CM_ENDPLAY` | C→S gate → group | Leave the world to character selection. | handled: `ToClient.cpp:449`, `ToClient.cpp:706` | sends: `ProCirculateCS.cpp:484` | [yes](session.md#endplay-434--endplayreply-934) |
| 435 | `CMD_CM_NEWCHA` | C→S gate → group | Create a character. | handled: `ToClient.cpp:450`, `ToClient.cpp:709` | sends: `ProCirculateCS.cpp:491` | no |
| 436 | `CMD_CM_DELCHA` | C→S gate → group | Delete a character. | handled: `ToClient.cpp:451`, `ToClient.cpp:712` | sends: `ProCirculateCS.cpp:503` | [yes](session.md#deletecharacter-436--deletecharacterreply-936) |
| 437 | `CMD_CM_CANCELEXIT` | C→S game | Stop the exit countdown. | handled: `CharacterPrl.cpp:292` | sends: `PacketCmd_CS.cpp:82` | no |
| 438 | `CMD_CM_REGISTER` | C→S gate → group | Register a new account. | handled: `ToClient.cpp:454`, `ToClient.cpp:721` | never sends | no |
| 515 | `CMD_MC_PING` | S→C game | Game ping. | sent: `GameAppNet.cpp:1604` | reads: `NetIF.cpp:452` | [yes](session.md#ping-515-server-to-client--pingreply-15-client-to-server) |
| 537 | `CMD_MC_CHECK_PING` | S→C game | Check ping. | sent: `Character.cpp:5462` | reads: `NetIF.cpp:456` | [yes](session.md#checkping-537-server-to-client--checkpingreply-17-client-to-server) |
| 613 | `CMD_MC_REQUESTPIN` | S→C game | Ask the player for a PIN. | sent: `CharScript.cpp:5856` | reads: `NetIF.cpp:141` | no |
| 931 | `CMD_MC_LOGIN` | S→C gate | Login result and character list. | sent: `ToClient.cpp:365`, `ToClient.cpp:774` | reads: `NetIF.cpp:56` | [yes](session.md#login-431--loginreply-931) |
| 932 | `CMD_MC_LOGOUT` | S→C unknown | Logout result. | no sender found | does not read | no |
| 933 | `CMD_MC_BGNPLAY` | S→C gate | Result of entering the world. | sent: `ToClient.cpp:975`, `ToClient.cpp:993` | reads: `NetIF.cpp:58` | [yes](session.md#beginplay-433--beginplayreply-933) |
| 934 | `CMD_MC_ENDPLAY` | S→C gate | Result of leaving the world, with the character list. | sent: `ToClient.cpp:1074`, `ToClient.cpp:1108` | reads: `NetIF.cpp:59` | [yes](session.md#endplay-434--endplayreply-934) |
| 935 | `CMD_MC_NEWCHA` | S→C game, gate | Result of character creation. | sent: `ToClient.cpp:1172`, `ToClient.cpp:1195` | reads: `NetIF.cpp:60` | no |
| 936 | `CMD_MC_DELCHA` | S→C gate | Result of character deletion. | sent: `ToClient.cpp:1249`, `ToClient.cpp:1264` | reads: `NetIF.cpp:61` | [yes](session.md#deletecharacter-436--deletecharacterreply-936) |
| 937 | `CMD_MC_STARTEXIT` | S→C game | Start the exit countdown. | sent: `CharacterRun.cpp:210` | reads: `NetIF.cpp:133` | no |
| 938 | `CMD_MC_CANCELEXIT` | S→C game | Stop the exit countdown. | sent: `CharacterRun.cpp:228` | reads: `NetIF.cpp:134` | no |
| 941 | `CMD_MC_CREATE_PASSWORD2` | S→C gate | Result of security code creation. | sent: `ToClient.cpp:1331`, `ToClient.cpp:1338` | reads: `NetIF.cpp:62` | [yes](session.md#createsecondpassword-346--reply-941) |
| 942 | `CMD_MC_UPDATE_PASSWORD2` | S→C gate | Result of security code change. | sent: `ToClient.cpp:1386`, `ToClient.cpp:1393` | reads: `NetIF.cpp:63` | [yes](session.md#changesecondpassword-347--reply-942) |
| 943 | `CMD_MC_SEND_SERVER_PUBLIC_KEY` | S→C gate | Server RSA public key. | sent: `ToClient.cpp:236` | reads: `NetIF.cpp:198` | [yes](session.md#serverpublickey-943-server-to-client) |
| 944 | `CMD_MC_SEND_HANDSHAKE` | S→C unknown | Handshake. | no sender found | reads: `NetIF.cpp:199` | no |
| 5022 | `CMD_PC_PING` | S→C group | Group ping. | sent: `GroupServer/Player.cpp:358` | reads: `NetIF.cpp:448` | [yes](session.md#groupping-5022-server-to-client--grouppingreply-6022-client-to-server) |
| 5045 | `CMD_PC_REGISTER` | S→C gate | Result of an account registration. | sent: `ToClient.cpp:1164` | reads: `NetIF.cpp:168` | no |
| 5418 | `CMD_PC_REFRESH_SELECT` | S→C group | Refresh the character list on other connections of the same account. | sent: `GroupServerAppServ.cpp:1591` | reads: `NetIF.cpp:271` | no |
| 6022 | `CMD_CP_PING` | C→S gate → group | Answer to group ping 5022. | handled: `ToClient.cpp:469`, `GroupServerAppServ.cpp:421` | sends: `PacketCmd_SC.cpp:440` | [yes](session.md#groupping-5022-server-to-client--grouppingreply-6022-client-to-server) |
| 6415 | `CMD_CP_CHANGEPASS` | C→S gate → group | Change the account password. | handled: `ToClient.cpp:455`, `ToClient.cpp:724` | sends: `PacketCmd_CS.cpp:174` | no |
| 7502 | `CMD_TC_DISCONNECT` | S→C gate | The gate tells the client why it closes the connection. | sent: `GateServer/GateServer.cpp:625` | reads: `NetIF.cpp:270` | no |

## Map and view

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 18 | `CMD_CM_MAP_MASK` | C→S game | Ask for the explored-area mask of a map. | handled: `CharacterPrl.cpp:849` | sends: `PacketCmd_CS.cpp:316` | no |
| 345 | `CMD_CM_ENTITY_EVENT` | C→S game | Use a map event entity, for example a resource or a trigger. | handled: `CharacterPrl.cpp:612` | sends: `PacketCmd_CS.cpp:696` | no |
| 504 | `CMD_MC_CHABEGINSEE` | S→C game | A character comes into view. | sent: `Character.cpp:516` | reads: `NetIF.cpp:64` | [yes](view.md#characterbeginsee-504) |
| 505 | `CMD_MC_CHAENDSEE` | S→C game | A character goes out of view. | sent: `Character.cpp:600` | reads: `NetIF.cpp:65` | [yes](view.md#characterendsee-505) |
| 506 | `CMD_MC_ITEMBEGINSEE` | S→C game | An item on the ground comes into view. | sent: `Item.cpp:51` | reads: `NetIF.cpp:75` | no |
| 507 | `CMD_MC_ITEMENDSEE` | S→C game | An item on the ground goes out of view. | sent: `Item.cpp:72` | reads: `NetIF.cpp:76` | no |
| 516 | `CMD_MC_ENTERMAP` | S→C game | Enter a map with the full state of the own character. | sent: `CharacterCmd.cpp:36`, `GameAppNet.cpp:514` | reads: `NetIF.cpp:57` | [yes](view.md#entermap-516) |
| 522 | `CMD_MC_ASTATEBEGINSEE` | S→C game | An area state (map region effect) comes into view. | sent: `MgrUnitEx.h:313`, `StateCell.cpp:366` | reads: `NetIF.cpp:190` | no |
| 523 | `CMD_MC_ASTATEENDSEE` | S→C game | An area state goes out of view. | sent: `MgrUnitEx.h:334`, `StateCell.cpp:387` | reads: `NetIF.cpp:191` | no |
| 538 | `CMD_MC_MAP_MASK` | S→C game | Explored-area mask of a map. | sent: `CharacterPrl.cpp:859` | reads: `NetIF.cpp:201` | no |
| 541 | `CMD_MC_EVENT_INFO` | S→C game | Event data of an entity in view. | sent: `Entity.cpp:757` | reads: `NetIF.cpp:206` | no |
| 850 | `CMD_MC_ENTITY_BEGINESEE` | S→C unknown | A map event entity comes into view. | no sender found | does not read | no |
| 851 | `CMD_MC_ENTITY_ENDSEE` | S→C unknown | A map event entity goes out of view. | no sender found | does not read | no |
| 852 | `CMD_MC_ENTITY_CHGSTATE` | S→C game | State of a map event entity changed. | sent: `Character.cpp:2158` | does not read | no |

## Actions and movement

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 6 | `CMD_CM_BEGINACTION` | C→S game | Start an action: move, skill, item use and more. | handled: `CharacterPrl.cpp:297` | sends: `ProCirculateCS.cpp:26` | [yes](actions.md#beginaction-6) |
| 7 | `CMD_CM_ENDACTION` | C→S game | Stop the current action. | handled: `CharacterPrl.cpp:311` | sends: `ProCirculateCS.cpp:391` | [yes](actions.md#endaction-7) |
| 508 | `CMD_MC_NOTIACTION` | S→C game | Action of a character in view: move, skill, look, kitbag and more. | sent: `Character.cpp:5280`, `Character.cpp:5297` | reads: `NetIF.cpp:68` | [yes](actions.md#notiaction-508) |
| 520 | `CMD_MC_FAILEDACTION` | S→C game | An action failed. | sent: `Character.cpp:1675` | reads: `NetIF.cpp:188` | [yes](actions.md#failedaction-520) |
| 536 | `CMD_MC_PREMOVE_TIME` | S→C game | Time the client may move before the server confirms. | sent: `Character.cpp:5473` | reads: `NetIF.cpp:200` | [yes](actions.md#premovetime-536) |

## Combat

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 10 | `CMD_CM_DIE_RETURN` | C→S game | After death, choose to revive or return to the town. | handled: `CharacterPrl.cpp:317` | sends: `PacketCmd_CS.cpp:286` | no |
| 94 | `CMD_CM_PK_CTRL` | C→S game | Turn PK mode on or off. | handled: `CharacterPrl.cpp:1882` | never sends | no |
| 535 | `CMD_MC_QUERY_RELIVE` | S→C game | Ask the dead player to accept a revive. | sent: `Character.cpp:671` | reads: `NetIF.cpp:197` | no |
| 542 | `CMD_MC_SIDE_INFO` | S→C game | Side (faction) of a character in view. | sent: `Character.cpp:5496` | reads: `NetIF.cpp:207` | no |

## Character: attributes, skills, look

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 8 | `CMD_CM_SYNATTR` | C→S game | Spend attribute points on the six base stats. | handled: `CharacterPrl.cpp:674` | sends: `ProCirculateCS.cpp:538` | no |
| 9 | `CMD_CM_SYNSKILLBAG` | C→S game | Sync the skill bag. | no handler found | never sends | no |
| 11 | `CMD_CM_SKILLUPGRADE` | C→S game | Upgrade a learned skill by one level. | handled: `CharacterPrl.cpp:679` | sends: `ProCirculateCS.cpp:577` | no |
| 16 | `CMD_CM_REFRESH_DATA` | C→S game | Ask the server to send the attributes of an own character again. | handled: `CharacterPrl.cpp:701` | sends: `ProCirculateCS.cpp:567` | no |
| 20 | `CMD_CM_UPDATEHAIR` | C→S game | Change the hair style at the barber. | handled: `CharacterPrl.cpp:874` | sends: `PacketCmd_CS.cpp:767` | no |
| 348 | `CMD_CM_READBOOK_START` | C→S game | Start to read a book (study state). | handled: `CharacterPrl.cpp:645` | sends: `PacketCmd_CS.cpp:752` | no |
| 349 | `CMD_CM_READBOOK_CLOSE` | C→S game | Stop reading. | handled: `CharacterPrl.cpp:660` | sends: `PacketCmd_CS.cpp:759` | no |
| 509 | `CMD_MC_SYNATTR` | S→C game | Attribute changes of a character. | sent: `FightAble.cpp:932`, `FightAble.cpp:961` | reads: `NetIF.cpp:87` | [yes](attributes.md) |
| 510 | `CMD_MC_SYNSKILLBAG` | S→C game | Skill bag content. | sent: `Character.cpp:5432` | reads: `NetIF.cpp:88` | no |
| 511 | `CMD_MC_SYNASKILLSTATE` | S→C game | Skill states (buffs and debuffs) of a character. | sent: `FightAble.cpp:1079`, `FightAble.cpp:1105` | reads: `NetIF.cpp:90` | no |
| 525 | `CMD_MC_CHA_EMOTION` | S→C game | A character shows an emotion. | sent: `EventHandler.cpp:15` | reads: `NetIF.cpp:192` | no |
| 531 | `CMD_MC_SYNDEFAULTSKILL` | S→C unknown | Default (normal attack) skill. | no sender found | reads: `NetIF.cpp:89` | no |
| 539 | `CMD_MC_OPENHAIR` | S→C game | Open the barber window. | sent: `CharacterPrl.cpp:2738` | reads: `NetIF.cpp:204` | no |
| 540 | `CMD_MC_UPDATEHAIR_RES` | S→C game | Result of a hair change. | sent: `CharacterPrl.cpp:2718` | reads: `NetIF.cpp:203` | no |
| 547 | `CMD_MC_APPEND_LOOK` | S→C game | Extra look parts of a character. | sent: `Character.cpp:6830` | reads: `NetIF.cpp:212` | no |
| 593 | `CMD_MC_CHAPLAYEFFECT` | S→C game | Play an effect on a character. | sent: `CharScript.cpp:1957`, `NpcScript.cpp:1104` | reads: `NetIF.cpp:258` | no |

## Items and kitbag

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 31 | `CMD_CM_KITBAG_LOCK` | C→S game | Lock the kitbag. | handled: `CharacterPrl.cpp:974` | sends: `PacketCmd_CS.cpp:236` | [yes](items.md#kitbaglock-31--kitbagunlock-32--lock-state-553) |
| 32 | `CMD_CM_KITBAG_UNLOCK` | C→S game | Unlock the kitbag with the security code. | handled: `CharacterPrl.cpp:1116` | sends: `PacketCmd_CS.cpp:245` | [yes](items.md#kitbaglock-31--kitbagunlock-32--lock-state-553) |
| 33 | `CMD_CM_KITBAG_CHECK` | C→S game | Ask for the kitbag lock state. | handled: `CharacterPrl.cpp:1122` | sends: `PacketCmd_CS.cpp:255` | no |
| 34 | `CMD_CM_KITBAG_AUTOLOCK` | C→S game | Turn the kitbag auto-lock on or off. | handled: `CharacterPrl.cpp:1127` | sends: `PacketCmd_CS.cpp:264` | no |
| 35 | `CMD_CM_KITBAGTEMP_SYNC` | C→S game | Ask for the temporary kitbag content. | handled: `CharacterPrl.cpp:1405` | sends: `PacketCmd_CS.cpp:1086` | [yes](items.md#temporarykitbagsync-35--reply-554) |
| 36 | `CMD_CM_KITBAGTEMPlocks` | C→S game | Lock the temporary kitbag. | no handler found | never sends | no |
| 99 | `CMD_CM_ITEM_LOCK_ASK` | C→S game | Lock one item. | handled: `CharacterPrl.cpp:1429` | sends: `PacketCmd_CS.cpp:1345` | no |
| 100 | `CMD_CM_ITEM_UNLOCK_ASK` | C→S game | Unlock one item. | handled: `CharacterPrl.cpp:1497` | sends: `PacketCmd_CS.cpp:1352` | no |
| 550 | `CMD_MC_ITEM_USE_SUC` | S→C game | A character used an item. | sent: `Character.cpp:6839` | reads: `NetIF.cpp:216` | no |
| 551 | `CMD_MC_KITBAG_CAPACITY` | S→C game | Kitbag capacity. | sent: `Character.cpp:6848` | reads: `NetIF.cpp:217` | no |
| 552 | `CMD_MC_ESPE_ITEM` | S→C game | Durability and energy of a special item (pet). | sent: `Character.cpp:6866` | reads: `NetIF.cpp:218` | no |
| 553 | `CMD_MC_KITBAG_CHECK_ASR` | S→C game | Kitbag lock state. | sent: `CharacterCmd.cpp:3446`, `CharacterCmd.cpp:3466`, `CharacterCmd.cpp:3478` | reads: `NetIF.cpp:220` | [yes](items.md#kitbaglock-31--kitbagunlock-32--lock-state-553) |
| 554 | `CMD_MC_KITBAGTEMP_SYNC` | S→C game | Temporary kitbag content. | sent: `CharacterPrl.cpp:1415` | reads: `NetIF.cpp:248` | [yes](items.md#temporarykitbagsync-35--reply-554) |
| 595 | `CMD_MC_ITEM_UNLOCK_ASR` | S→C game | Result of an item unlock. | sent: `Character.cpp:7624` | reads: `NetIF.cpp:242` | no |
| 599 | `CMD_CM_ITEM_LOCK_ASR` | S→C game | Result of an item lock. The name has `CM`, but the value is in the server-to-client range. | sent: `CharacterPrl.cpp:1431` | reads: `NetIF.cpp:241` | no |

## Crafting: forge, repair, life skills

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 23 | `CMD_CM_ITEM_REPAIR_ASK` | C→S game | Ask for the repair price of an item. | handled: `CharacterPrl.cpp:894` | sends: `PacketCmd_CS.cpp:800` | no |
| 24 | `CMD_CM_ITEM_REPAIR_ASR` | C→S game | Confirm or cancel an item repair. | handled: `CharacterPrl.cpp:904` | sends: `PacketCmd_CS.cpp:811` | no |
| 25 | `CMD_CM_ITEM_FORGE_ASK` | C→S game | Send a forge, fusion or other item process request. | handled: `CharacterPrl.cpp:916` | sends: `PacketCmd_CS.cpp:819` | no |
| 26 | `CMD_CM_ITEM_FORGE_ASR` | C→S game | Confirm or cancel an item process. | handled: `CharacterPrl.cpp:969` | sends: `PacketCmd_CS.cpp:845` | no |
| 29 | `CMD_CM_ITEM_FORGE_CANACTION` | C→S game | Tell the server that the item process animation is done. | handled: `CharacterPrl.cpp:909` | sends: `PacketCmd_CS.cpp:1321` | no |
| 80 | `CMD_CM_LIFESKILL_ASR` | C→S game | Do a life skill step: compose, break, cook and more. | handled: `CharacterPrl.cpp:1037` | sends: `PacketCmd_CS.cpp:1229` | no |
| 81 | `CMD_CM_LIFESKILL_ASK` | C→S game | Ask for the result or cost of a life skill step. | handled: `CharacterPrl.cpp:979` | sends: `PacketCmd_CS.cpp:1245` | no |
| 335 | `CMD_CM_FORGE` | C→S game | Forge an item at an NPC. | handled: `CharacterPrl.cpp:461` | sends: `PacketCmd_CS.cpp:607` | no |
| 544 | `CMD_MC_ITEM_REPAIR_ASK` | S→C game | Repair price; asks for a confirm. | sent: `CharacterCmd.cpp:2823` | reads: `NetIF.cpp:210` | no |
| 545 | `CMD_MC_ITEM_REPAIR_ASR` | S→C unknown | Result of an item repair. | no sender found | does not read | no |
| 546 | `CMD_MC_BEGIN_ITEM_REPAIR` | S→C game | Open the repair window. | sent: `Character.cpp:6715` | reads: `NetIF.cpp:211` | no |
| 548 | `CMD_MC_ITEM_FORGE_ASK` | S→C game | Item process cost; asks for a confirm. | sent: `CharacterCmd.cpp:3072` | reads: `NetIF.cpp:213` | no |
| 549 | `CMD_MC_ITEM_FORGE_ASR` | S→C game | Result of an item process. | sent: `CharacterCmd.cpp:3096`, `CharacterCmd.cpp:3697` | reads: `NetIF.cpp:214` | no |
| 600 | `CMD_CM_ITEM_UNLOCK_ASR` / `CMD_MC_LIFESKILL_BGING` (same value) | S→C game | `LIFESKILL_BGING`: open a life skill window. `CMD_CM_ITEM_UNLOCK_ASR` is not used; the item unlock result is 595. | sent: `CharScript.cpp:5488` | reads: `NetIF.cpp:238` | no |
| 601 | `CMD_MC_LIFESKILL_ASR` | S→C game | Result of a life skill step. | sent: `CharacterCmd.cpp:3422` | reads: `NetIF.cpp:240` | no |
| 602 | `CMD_MC_LIFESKILL_ASK` | S→C game | Life skill cost or preview; asks for a confirm. | sent: `CharacterCmd.cpp:3290` | reads: `NetIF.cpp:239` | no |
| 833 | `CMD_MC_BEGIN_ITEM_FUSION` | S→C game | Open the fusion window. | sent: `Character.cpp:6752` | reads: `NetIF.cpp:108` | no |
| 834 | `CMD_MC_BEGIN_ITEM_UPGRADE` | S→C game | Open the upgrade window. | sent: `Character.cpp:6759` | reads: `NetIF.cpp:109` | no |
| 835 | `CMD_MC_BEGIN_ITEM_FORGE` | S→C game | Open the forge window. | sent: `Character.cpp:6722` | reads: `NetIF.cpp:104` | no |
| 836 | `CMD_MC_BEGIN_ITEM_UNITE` | S→C game | Open the combine window. | sent: `Character.cpp:6738` | reads: `NetIF.cpp:106` | no |
| 837 | `CMD_MC_BEGIN_ITEM_MILLING` | S→C game | Open the milling window. | sent: `Character.cpp:6745` | reads: `NetIF.cpp:107` | no |
| 847 | `CMD_MC_BEGIN_ITEM_EIDOLON_METEMPSYCHOSIS` | S→C game | Open the fairy rebirth window. | sent: `Character.cpp:6766` | reads: `NetIF.cpp:110` | no |
| 848 | `CMD_MC_BEGIN_ITEM_EIDOLON_FUSION` | S→C game | Open the fairy fusion window. | sent: `Character.cpp:6773` | reads: `NetIF.cpp:111` | no |
| 849 | `CMD_MC_BEGIN_ITEM_PURIFY` | S→C game | Open the equipment purify window. | sent: `Character.cpp:6780` | reads: `NetIF.cpp:112` | no |
| 871 | `CMD_MC_BEGIN_ITEM_ENERGY` | S→C game | Open the energy charge window. | sent: `Character.cpp:6794` | reads: `NetIF.cpp:113` | no |
| 872 | `CMD_MC_BEGIN_GET_STONE` | S→C game | Open the gem extraction window. | sent: `Character.cpp:6816` | reads: `NetIF.cpp:114` | no |
| 876 | `CMD_MC_BEGIN_ITEM_FIX` | S→C game | Open the item fix window. | sent: `Character.cpp:6787` | reads: `NetIF.cpp:116` | no |

## Item mall

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 41 | `CMD_CM_STORE_OPEN_ASK` | C→S game | Open the item mall. | handled: `CharacterPrl.cpp:1133`, `shop/init.lua:75` | sends: `PacketCmd_CS.cpp:941` | no |
| 42 | `CMD_CM_STORE_LIST_ASK` | C→S game | Ask for one item mall page. | handled: `CharacterPrl.cpp:1165`, `shop/init.lua:80` | sends: `PacketCmd_CS.cpp:956` | no |
| 43 | `CMD_CM_STORE_BUY_ASK` | C→S game | Buy an item in the item mall. | handled: `CharacterPrl.cpp:1166`, `shop/init.lua:77` | sends: `PacketCmd_CS.cpp:966` | no |
| 44 | `CMD_CM_STORE_CHANGE_ASK` | C→S game | Exchange mall currency. | handled: `CharacterPrl.cpp:1167` | sends: `PacketCmd_CS.cpp:974` | no |
| 45 | `CMD_CM_STORE_QUERY` | C→S game | Ask for the item mall purchase history. | handled: `CharacterPrl.cpp:1168` | sends: `PacketCmd_CS.cpp:982` | no |
| 46 | `CMD_CM_STORE_VIP` | C→S game | Buy item mall VIP status. | handled: `CharacterPrl.cpp:1170` | never sends | no |
| 47 | `CMD_CM_STORE_AFFICHE` | C→S game | Ask for the item mall advertisement. | no handler found | never sends | no |
| 48 | `CMD_CM_STORE_CLOSE` | C→S game | Close the item mall. | handled: `CharacterPrl.cpp:1169`, `shop/init.lua:85` | sends: `PacketCmd_CS.cpp:949` | no |
| 561 | `CMD_MC_STORE_OPEN_ASR` | S→C game | Item mall opens: categories and account data. | sent: `CharTrade.cpp:3769`, `shop/init.lua:194` | reads: `NetIF.cpp:222` | no |
| 562 | `CMD_MC_STORE_LIST_ASR` | S→C game | One item mall page. | sent: `CharTrade.cpp:2756`, `CharTrade.cpp:2830` | reads: `NetIF.cpp:223` | no |
| 563 | `CMD_MC_STORE_BUY_ASR` | S→C game | Result of an item mall purchase. | sent: `CharTrade.cpp:2327`, `CharTrade.cpp:2341` | reads: `NetIF.cpp:224` | no |
| 564 | `CMD_MC_STORE_CHANGE_ASR` | S→C game | Result of a currency exchange. | sent: `CharTrade.cpp:2993`, `CharTrade.cpp:3008` | reads: `NetIF.cpp:225` | no |
| 565 | `CMD_MC_STORE_QUERY` | S→C game | Item mall purchase history. | sent: `CharTrade.cpp:3243`, `CharTrade.cpp:3258` | reads: `NetIF.cpp:226` | no |
| 566 | `CMD_MC_STORE_VIP` | S→C game | Result of a VIP purchase. | sent: `CharTrade.cpp:2864`, `CharTrade.cpp:2879` | reads: `NetIF.cpp:227` | no |
| 567 | `CMD_MC_STORE_AFFICHE` | S→C unknown | Item mall advertisement. | no sender found | does not read | no |
| 611 | `CMD_MC_UPDATEIMP` | S→C game | Item mall points (IMP) of the account. | sent: `Character.cpp:7613`, `CharTrade.cpp:2117` | reads: `NetIF.cpp:139` | no |

## NPC trade

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 51 | `CMD_CM_BLACKMARKET_EXCHANGE_REQ` | C→S game | Exchange items at a black market NPC. | handled inside 308: `MisSdk/NpcSdk.lua:1704` | sends inside 308: `PacketCmd_CS.cpp:423` | no |
| 308 | `CMD_CM_REQUESTTRADE` | C→S game | Trade with an NPC. The packet carries a sub-command (309 or 51). | handled: `CharacterPrl.cpp:387` | sends: `PacketCmd_CS.cpp:351` | no |
| 309 | `CMD_CM_TRADEITEM` | C→S game | Sub-command of 308: buy, sell or exchange an item. | handled inside 308: `MisSdk/NpcSdk.lua:1725` | sends inside 308: `PacketCmd_CS.cpp:353` | no |
| 571 | `CMD_MC_BLACKMARKET_EXCHANGEDATA` | S→C game | Black market exchange list. | sent: `MisSdk/NpcSdk.lua:105` | reads: `NetIF.cpp:229` | no |
| 572 | `CMD_MC_BLACKMARKET_EXCHANGE_ASR` | S→C game | Result of a black market exchange. | sent: `Character.cpp:2763`, `Character.cpp:2791` | reads: `NetIF.cpp:230` | no |
| 573 | `CMD_MC_BLACKMARKET_EXCHANGEUPDATE` | S→C game | Black market exchange list update. | sent: `MisSdk/NpcSdk.lua:150` | reads: `NetIF.cpp:231` | no |
| 574 | `CMD_MC_BLACKMARKET_TRADEUPDATE` | S→C game | Black market trade list update. | sent: `MisSdk/NpcSdk.lua:738` | reads: `NetIF.cpp:82` | no |
| 575 | `CMD_MC_EXCHANGEDATA` | S→C game | Normal NPC exchange list. | sent: `MisSdk/NpcSdk.lua:128` | reads: `NetIF.cpp:232` | no |
| 805 | `CMD_MC_TRADEPAGE` | S→C game | Open the NPC shop page. | sent: `MisSdk/NpcSdk.lua:873` | reads: `NetIF.cpp:81` | no |
| 806 | `CMD_MC_TRADERESULT` | S→C unknown | Result of an NPC trade. | no sender found | reads: `NetIF.cpp:85` | no |
| 807 | `CMD_MC_TRADE_DATA` | S→C game | One NPC shop slot changed. | sent: `MisSdk/NpcSdk.lua:1127` | reads: `NetIF.cpp:83` | no |
| 808 | `CMD_MC_TRADE_ALLDATA` | S→C game | All NPC shop goods. | sent: `MisSdk/NpcSdk.lua:1147` | reads: `NetIF.cpp:84` | no |
| 809 | `CMD_MC_TRADE_UPDATE` | S→C unknown | Trade goods update. | no sender found | does not read | no |
| 810 | `CMD_MC_EXCHANGE_UPDATE` | S→C unknown | Exchange goods update. | no sender found | does not read | no |

## Player trade

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 312 | `CMD_CM_CHARTRADE_REQUEST` | C→S game | Ask a player to trade. | handled: `CharacterPrl.cpp:467` | sends: `PacketCmd_CS.cpp:437` | no |
| 313 | `CMD_CM_CHARTRADE_ACCEPT` | C→S game | Accept a trade request. | handled: `CharacterPrl.cpp:474` | sends: `PacketCmd_CS.cpp:447` | no |
| 314 | `CMD_CM_CHARTRADE_REJECT` | C→S game | Refuse a trade request. | handled: `CharacterPrl.cpp:481` | never sends | no |
| 315 | `CMD_CM_CHARTRADE_CANCEL` | C→S game | Cancel the trade. | handled: `CharacterPrl.cpp:485` | sends: `PacketCmd_CS.cpp:497` | no |
| 316 | `CMD_CM_CHARTRADE_ITEM` | C→S game | Put an item into the trade or take it out. | handled: `CharacterPrl.cpp:492` | sends: `PacketCmd_CS.cpp:457` | no |
| 317 | `CMD_CM_CHARTRADE_VALIDATEDATA` | C→S game | Lock the trade content. | handled: `CharacterPrl.cpp:521` | sends: `PacketCmd_CS.cpp:507` | no |
| 318 | `CMD_CM_CHARTRADE_VALIDATE` | C→S game | Confirm the trade. | handled: `CharacterPrl.cpp:528` | sends: `PacketCmd_CS.cpp:517` | no |
| 319 | `CMD_CM_CHARTRADE_MONEY` | C→S game | Set the gold in the trade. | handled: `CharacterPrl.cpp:503` | sends: `PacketCmd_CS.cpp:471` | no |
| 811 | `CMD_MC_CHARTRADE` | S→C game | Player trade packet. The payload carries a sub-command (812-819). | sent: `CharTrade.cpp:224`, `CharTrade.cpp:475` | reads: `NetIF.cpp:86` | no |
| 812 | `CMD_MC_CHARTRADE_REQUEST` | S→C game | Sub-command of 811: a player asks you to trade. | sent inside 811: `CharTrade.cpp:225` | reads: `PacketCmd_SC.cpp:1557` | no |
| 813 | `CMD_MC_CHARTRADE_CANCEL` | S→C game | Sub-command of 811: the trade is cancelled. | sent inside 811: `CharTrade.cpp:561`, `CharTrade.cpp:611` | reads: `PacketCmd_SC.cpp:1564` | no |
| 814 | `CMD_MC_CHARTRADE_ITEM` | S→C game | Sub-command of 811: an item goes into or out of the trade. | sent inside 811: `CharTrade.cpp:940`, `CharTrade.cpp:1105` | reads: `PacketCmd_SC.cpp:1583` | no |
| 815 | `CMD_MC_CHARTRADE_PAGE` | S→C game | Sub-command of 811: open the trade window. | sent inside 811: `CharTrade.cpp:476` | reads: `PacketCmd_SC.cpp:1678` | no |
| 816 | `CMD_MC_CHARTRADE_VALIDATEDATA` | S→C game | Sub-command of 811: a side locked the trade content. | sent inside 811: `CharTrade.cpp:1267` | reads: `PacketCmd_SC.cpp:1686` | no |
| 817 | `CMD_MC_CHARTRADE_VALIDATE` | S→C game | Sub-command of 811: a side confirmed the trade. | sent inside 811: `CharTrade.cpp:2144` | reads: `PacketCmd_SC.cpp:1692` | no |
| 818 | `CMD_MC_CHARTRADE_RESULT` | S→C game | Sub-command of 811: trade result. | sent inside 811: `CharTrade.cpp:1472`, `CharTrade.cpp:2010` | reads: `PacketCmd_SC.cpp:1698` | no |
| 819 | `CMD_MC_CHARTRADE_MONEY` | S→C game | Sub-command of 811: gold in the trade changed. | sent inside 811: `CharTrade.cpp:725`, `CharTrade.cpp:807` | reads: `PacketCmd_SC.cpp:1570` | no |

## Stall

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 87 | `CMD_CM_STALLSEARCH` | C→S game | Search the stalls for an item. | handled: `CharacterPrl.cpp:43` | sends: `PacketCmd_CS.cpp:204` | no |
| 330 | `CMD_CM_STALL_ALLDATA` | C→S game | Open the stall of another player. | handled: `CharacterPrl.cpp:625` | sends: `PacketCmd_CS.cpp:709` | no |
| 331 | `CMD_CM_STALL_OPEN` | C→S game | Open own stall with name and goods. | handled: `CharacterPrl.cpp:630` | sends: `PacketCmd_CS.cpp:725` | no |
| 332 | `CMD_CM_STALL_BUY` | C→S game | Buy from a stall. | handled: `CharacterPrl.cpp:635` | sends: `PacketCmd_CS.cpp:733` | no |
| 333 | `CMD_CM_STALL_CLOSE` | C→S game | Close own stall. | handled: `CharacterPrl.cpp:640` | sends: `PacketCmd_CS.cpp:744` | no |
| 358 | `CMD_CM_OFFLINE_MODE` | C→S gate | Keep the stall open after the player goes offline. | handled: `ToClient.cpp:532` | sends: `PacketCmd_CS.cpp:73` | no |
| 588 | `CMD_MC_STALLSEARCH` | S→C game | Stall search result. | sent: `CharStall.cpp:467` | reads: `NetIF.cpp:136` | no |
| 854 | `CMD_MC_STALL_ALLDATA` | S→C game | Name and goods of a stall. | sent: `CharStall.cpp:942` | reads: `NetIF.cpp:128` | no |
| 855 | `CMD_MC_STALL_UPDATE` | S→C unknown | Stall goods update. | no sender found | does not read | no |
| 856 | `CMD_MC_STALL_DELGOODS` | S→C game | Remove goods from a stall slot. | sent: `CharStall.cpp:932` | reads: `NetIF.cpp:130` | no |
| 857 | `CMD_MC_STALL_CLOSE` | S→C game | A stall closed. | sent: `CharStall.cpp:419` | reads: `NetIF.cpp:131` | no |
| 858 | `CMD_MC_STALL_START` | S→C game | Own stall opened. | sent: `CharStall.cpp:401` | reads: `NetIF.cpp:132` | no |
| 859 | `CMD_MC_STALL_NAME` | S→C game | Stall name of a character in view. | sent: `Character.cpp:6706` | reads: `NetIF.cpp:205` | no |

## Quests and NPC dialogue

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 301 | `CMD_CM_REQUESTNPC` / `CMD_CM_REQUESTTALK` (same value) | C→S game | Talk to an NPC. The packet carries a sub-command (302, 303 or 322). | handled: `CharacterPrl.cpp:386` | sends: `PacketCmd_CS.cpp:328` | no |
| 302 | `CMD_CM_TALKPAGE` | C→S game | Sub-command of 301: open a dialogue page. | handled inside 301: `MisSdk/NpcSdk.lua:1366` | sends inside 301: `PacketCmd_CS.cpp:330` | no |
| 303 | `CMD_CM_FUNCITEM` | C→S game | Sub-command of 301: select an NPC menu item. | handled inside 301: `MisSdk/NpcSdk.lua:1425` | sends inside 301: `PacketCmd_CS.cpp:341` | no |
| 310 | `CMD_CM_REQUESTAGENCY` | C→S game | Ask for an NPC agency service. | no handler found | never sends | no |
| 322 | `CMD_CM_MISSION` | C→S game | Sub-command of 301: quest page action. | handled inside 301: `MisSdk/NpcSdk.lua:1804` | sends inside 301: `PacketCmd_CS.cpp:529` | no |
| 323 | `CMD_CM_MISSIONLIST` | C→S game | Ask for the NPC quest list. | no handler found | never sends | no |
| 324 | `CMD_CM_MISSIONTALK` | C→S game | Ask for a quest dialogue. | no handler found | never sends | no |
| 325 | `CMD_CM_MISLOG` | C→S game | Ask for the quest log. | handled: `CharacterPrl.cpp:444` | sends: `PacketCmd_CS.cpp:579` | no |
| 326 | `CMD_CM_MISLOGINFO` | C→S game | Ask for the details of one quest. | handled: `CharacterPrl.cpp:449` | sends: `PacketCmd_CS.cpp:589` | no |
| 327 | `CMD_CM_MISLOG_CLEAR` | C→S game | Abandon one quest. | handled: `CharacterPrl.cpp:455` | sends: `PacketCmd_CS.cpp:599` | no |
| 801 | `CMD_MC_TALKPAGE` | S→C game | NPC dialogue page. | sent: `MisSdk/NpcSdk.lua:95`, `MisSdk/NpcSdk.lua:173` | reads: `NetIF.cpp:77` | no |
| 802 | `CMD_MC_FUNCPAGE` | S→C game | NPC menu page. | sent: `MisSdk/NpcSdk.lua:198`, `MisSdk/NpcSdk.lua:216` | reads: `NetIF.cpp:78` | no |
| 803 | `CMD_MC_CLOSETALK` | S→C game | Close the NPC dialogue. | sent: `MisSdk/NpcSdk.lua:262` | reads: `NetIF.cpp:79` | no |
| 804 | `CMD_MC_HELPINFO` | S→C game | Help text from an NPC. | sent: `MisSdk/NpcSdk.lua:184` | reads: `NetIF.cpp:80` | no |
| 822 | `CMD_MC_MISSION` | S→C game | NPC quest list page. | sent: `MisSdk/NpcSdk.lua:333` | reads: `NetIF.cpp:91` | no |
| 823 | `CMD_MC_MISSIONLIST` | S→C unknown | Quest list. | no sender found | does not read | no |
| 824 | `CMD_MC_MISSIONTALK` | S→C unknown | Quest dialogue. | no sender found | does not read | no |
| 825 | `CMD_MC_NPCSTATECHG` | S→C game | Quest marker state of an NPC. | sent: `Character.cpp:2177` | reads: `NetIF.cpp:98` | no |
| 826 | `CMD_MC_TRIGGER_ACTION` | S→C game | Quest trigger progress, for example a kill count. | sent: `Mission.cpp:661`, `Mission.cpp:808` | reads: `NetIF.cpp:103` | no |
| 827 | `CMD_MC_MISPAGE` | S→C game | Quest page: text, needs and rewards. | sent: `MisSdk/MissionSdk.lua:1924`, `MisSdk/MissionSdk.lua:2062` | reads: `NetIF.cpp:92` | no |
| 828 | `CMD_MC_MISLOG` | S→C game | Quest log. | sent: `Mission.cpp:1625` | reads: `NetIF.cpp:93` | no |
| 829 | `CMD_MC_MISLOGINFO` | S→C game | Details of one quest in the log. | sent: `MisSdk/MissionSdk.lua:1843` | reads: `NetIF.cpp:94` | no |
| 830 | `CMD_MC_MISLOG_CHANGE` | S→C game | State of a quest in the log changed. | sent: `Mission.cpp:1760`, `Mission.cpp:1780` | reads: `NetIF.cpp:97` | no |
| 831 | `CMD_MC_MISLOG_CLEAR` | S→C game | Remove a quest from the log. | sent: `Mission.cpp:1679` | reads: `NetIF.cpp:95` | no |
| 832 | `CMD_MC_MISLOG_ADD` | S→C game | Add a quest to the log. | sent: `Mission.cpp:1688` | reads: `NetIF.cpp:96` | no |

## Boat

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 338 | `CMD_CM_CREATE_BOAT` | C→S game | Build a new boat at the shipyard. | handled: `CharacterPrl.cpp:535` | sends: `PacketCmd_CS.cpp:616` | no |
| 339 | `CMD_CM_UPDATEBOAT_PART` | C→S game | Change a part of the boat under construction. | handled: `CharacterPrl.cpp:540` | sends: `PacketCmd_CS.cpp:668` | no |
| 340 | `CMD_CM_BOAT_CANCEL` | C→S game | Cancel the boat construction. | handled: `CharacterPrl.cpp:558` | sends: `PacketCmd_CS.cpp:680` | no |
| 341 | `CMD_CM_BOAT_LUANCH` | C→S game | Launch a boat from the harbour. | handled: `CharacterPrl.cpp:563` | sends: `PacketCmd_CS.cpp:648` | no |
| 342 | `CMD_CM_BOAT_BAGSEL` | C→S game | Select a boat whose hold receives goods. | handled: `CharacterPrl.cpp:598` | sends: `PacketCmd_CS.cpp:658` | no |
| 343 | `CMD_CM_BOAT_SELECT` | C→S game | Select a boat in a berth list. | handled: `CharacterPrl.cpp:581` | sends: `PacketCmd_CS.cpp:636` | no |
| 344 | `CMD_CM_BOAT_GETINFO` | C→S game | Ask for the attributes of a boat. | handled: `CharacterPrl.cpp:545` | sends: `PacketCmd_CS.cpp:688` | no |
| 532 | `CMD_MC_ADD_ITEM_CHA` | S→C game | Add a boat to the own boat list. | sent: `Character.cpp:5442` | reads: `NetIF.cpp:66` | no |
| 533 | `CMD_MC_DEL_ITEM_CHA` | S→C game | Remove a boat from the own boat list. | sent: `Character.cpp:5452` | reads: `NetIF.cpp:67` | no |
| 838 | `CMD_MC_CREATEBOAT` | S→C game | Boat construction started, with its attributes. | sent: `CharBoat.cpp:503` | reads: `NetIF.cpp:120` | no |
| 839 | `CMD_MC_UPDATEBOAT` | S→C game | Attributes of the boat under construction changed. | sent: `CharBoat.cpp:676` | reads: `NetIF.cpp:121` | no |
| 840 | `CMD_MC_UPDATEBOAT_PART` | S→C unknown | A part of the boat under construction changed. | no sender found | reads: `NetIF.cpp:122` | no |
| 841 | `CMD_MC_BERTH_LIST` | S→C game | List of boats in a berth, for launch or other actions. | sent: `Character.cpp:6280`, `Character.cpp:6364` | reads: `NetIF.cpp:123` | no |
| 842 | `CMD_MC_BOAT_LIST` | S→C unknown | All boats of the character. | no sender found | does not read | no |
| 843 | `CMD_MC_BOAT_ADD` | S→C game | Add a boat. | sent: `Character.cpp:6300` | does not read | no |
| 844 | `CMD_MC_BOAT_CLEAR` | S→C game | Remove a boat. | sent: `Character.cpp:6310` | does not read | no |
| 845 | `CMD_MC_BOATINFO` | S→C game | Attributes of a boat. | sent: `CharBoat.cpp:541`, `CharBoat.cpp:570` | reads: `NetIF.cpp:124` | no |
| 846 | `CMD_MC_BOAT_BAGLIST` | S→C unknown | Boats whose hold can receive goods. | no sender found | does not read | no |

## Chat

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 1 | `CMD_CM_SAY` | C→S gate → game | Say a line in local chat. The gate first checks for `&` GM commands. | handled: `CharacterPrl.cpp:325`, `ToClient.cpp:485` | sends: `ProCirculateCS.cpp:521` | no |
| 91 | `CMD_CM_SAY2CAMP` | C→S game | Say a line in the camp (side) channel. | no handler found | sends: `PacketCmd_CS.cpp:1176` | no |
| 501 | `CMD_MC_SAY` | S→C game | Local chat line from a player in view. | sent: `CharacterPrl.cpp:378`, `CharacterSuperCmd.cpp:1288` | reads: `NetIF.cpp:69` | no |
| 596 | `CMD_MC_SAY2CAMP` | S→C unknown | Camp channel chat line. | no sender found | reads: `NetIF.cpp:260` | no |
| 5400 | `CMD_PC_GM1SAY` | S→C group | GM message to all players. | sent: `GroupServer/GroupServerAppChat.cpp:26` | reads: `NetIF.cpp:154` | no |
| 5401 | `CMD_PC_SAY2TRADE` | S→C group | Trade channel line. | sent: `GroupServer/GroupServerAppChat.cpp:133`, `GroupServer/GroupServerAppChat.cpp:249` | reads: `NetIF.cpp:153` | no |
| 5402 | `CMD_PC_SAY2ALL` | S→C group | World channel line. | sent: `GroupServer/GroupServerAppChat.cpp:200`, `GroupServer/GroupServerAppChat.cpp:287` | reads: `NetIF.cpp:152` | no |
| 5403 | `CMD_PC_SAY2YOU` | S→C group | Private message. | sent: `GroupServer/GroupServerAppChat.cpp:404` | reads: `NetIF.cpp:149` | no |
| 5404 | `CMD_PC_SAY2TEM` | S→C group | Team channel line. | sent: `GroupServer/GroupServerAppChat.cpp:447` | reads: `NetIF.cpp:150` | no |
| 5405 | `CMD_PC_SAY2GUD` | S→C group | Guild channel line. | sent: `GroupServer/GroupServerAppChat.cpp:492` | reads: `NetIF.cpp:151` | no |
| 5406 | `CMD_PC_SESS_CREATE` | S→C group | A chat session was created. | sent: `GroupServer/GroupServerAppChat.cpp:509`, `GroupServer/GroupServerAppChat.cpp:593` | reads: `NetIF.cpp:157` | no |
| 5407 | `CMD_PC_SESS_SAY` | S→C group | Line in a chat session. | sent: `GroupServer/GroupServerAppChat.cpp:847` | reads: `NetIF.cpp:160` | no |
| 5408 | `CMD_PC_SESS_ADD` | S→C group | A player joined a chat session. | sent: `GroupServer/GroupServerAppChat.cpp:769` | reads: `NetIF.cpp:158` | no |
| 5409 | `CMD_PC_SESS_LEAVE` | S→C group | A player left a chat session. | sent: `GroupServer/GroupServerAppChat.cpp:797` | reads: `NetIF.cpp:159` | no |
| 5411 | `CMD_PC_GM1SAY1` | S→C group | GM scroll message. | sent: `GroupServer/GroupServerAppChat.cpp:75` | reads: `NetIF.cpp:155` | no |
| 6400 | `CMD_CP_GM1SAY` | C→S group | GM message to all players. | handled: `GroupServerAppServ.cpp:457` | sends: `NetPkChat.cpp:27` | no |
| 6401 | `CMD_CP_SAY2TRADE` | C→S group | Trade channel line. | handled: `GroupServerAppServ.cpp:463` | sends: `NetPkChat.cpp:46` | no |
| 6402 | `CMD_CP_SAY2ALL` | C→S group | World channel line. | handled: `GroupServerAppServ.cpp:466` | sends: `NetPkChat.cpp:54` | no |
| 6403 | `CMD_CP_SAY2YOU` | C→S group | Private message. | handled: `GroupServerAppServ.cpp:469` | sends: `NetPkChat.cpp:70` | no |
| 6404 | `CMD_CP_SAY2TEM` | C→S group | Team channel line. | handled: `GroupServerAppServ.cpp:472` | sends: `NetPkChat.cpp:79` | no |
| 6405 | `CMD_CP_SAY2GUD` | C→S group | Guild channel line. | handled: `GroupServerAppServ.cpp:475` | sends: `NetPkChat.cpp:87` | no |
| 6406 | `CMD_CP_SESS_CREATE` | C→S group | Create a chat session (group chat window). | handled: `GroupServerAppServ.cpp:535` | sends: `NetPkChat.cpp:189` | no |
| 6407 | `CMD_CP_SESS_SAY` | C→S group | Line in a chat session. | handled: `GroupServerAppServ.cpp:538` | sends: `NetPkChat.cpp:218` | no |
| 6408 | `CMD_CP_SESS_ADD` | C→S group | Add a player to a chat session. | handled: `GroupServerAppServ.cpp:541` | sends: `NetPkChat.cpp:201` | no |
| 6409 | `CMD_CP_SESS_LEAVE` | C→S group | Leave a chat session. | handled: `GroupServerAppServ.cpp:544` | sends: `NetPkChat.cpp:210` | no |
| 6410 | `CMD_CP_REFUSETOME` | C→S group | Block or allow private messages. | handled: `GroupServerAppServ.cpp:454` | sends: `NetPkChat.cpp:62` | no |
| 6411 | `CMD_CP_GM1SAY1` | C→S group | GM scroll message to all players. | handled: `GroupServerAppServ.cpp:460` | sends: `NetPkChat.cpp:37` | no |

## Notices

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 503 | `CMD_MC_MAPCRASH` | S→C game, gate | Show an error text in a box. The gate uses it for login errors. | sent: `ToClient.cpp:757`, `ToClient.cpp:1456` | reads: `NetIF.cpp:101` | no |
| 517 | `CMD_MC_SYSINFO` | S→C game, gate, group | System text in the chat window. | sent: `Character.cpp:2016`, `CharacterCmd.cpp:1423` | reads: `NetIF.cpp:70` | no |
| 518 | `CMD_MC_ALARM` | S→C unknown | Alarm text. | no sender found | does not read | no |
| 521 | `CMD_MC_MESSAGE` | S→C game | Message box text. | sent: `Character.cpp:5505` | reads: `NetIF.cpp:189` | no |
| 568 | `CMD_MC_POPUP_NOTICE` | S→C game | Pop-up notice text. | sent: `Character.cpp:2035` | reads: `NetIF.cpp:74` | no |
| 860 | `CMD_MC_BICKER_NOTICE` | S→C game | Notice text. | sent: `Character.cpp:1980` | reads: `NetIF.cpp:126` | no |
| 892 | `CMD_MC_COLOUR_NOTICE` | S→C game | Notice text with a colour. | sent: `Character.cpp:1996` | reads: `NetIF.cpp:127` | no |
| 5414 | `CMD_PC_ERRMSG` | S→C group | Error text from the group server. | sent: `GroupServerAppServ.cpp:159`, `GroupServerAppServ.cpp:333` | reads: `NetIF.cpp:100` | no |

## Team and volunteer list

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 21 | `CMD_CM_TEAM_FIGHT_ASK` | C→S game | Challenge another team to a team fight. | handled: `CharacterPrl.cpp:880` | sends: `PacketCmd_CS.cpp:779` | no |
| 22 | `CMD_CM_TEAM_FIGHT_ASR` | C→S game | Accept or refuse a team fight. | handled: `CharacterPrl.cpp:888` | sends: `PacketCmd_CS.cpp:789` | no |
| 61 | `CMD_CM_VOLUNTER_LIST` | C→S game | Ask for one page of the volunteer (party search) list. | handled: `CharacterPrl.cpp:1287` | sends: `PacketCmd_CS.cpp:1035` | no |
| 62 | `CMD_CM_VOLUNTER_ADD` | C→S game | Add self to the volunteer list. | handled: `CharacterPrl.cpp:1316` | sends: `PacketCmd_CS.cpp:1044` | no |
| 63 | `CMD_CM_VOLUNTER_DEL` | C→S game | Remove self from the volunteer list. | handled: `CharacterPrl.cpp:1323` | sends: `PacketCmd_CS.cpp:1053` | no |
| 64 | `CMD_CM_VOLUNTER_SEL` | C→S game | Invite a player from the volunteer list. | handled: `CharacterPrl.cpp:1330` | sends: `PacketCmd_CS.cpp:1060` | no |
| 65 | `CMD_CM_VOLUNTER_OPEN` | C→S game | Open the volunteer list. | handled: `CharacterPrl.cpp:1258` | sends: `PacketCmd_CS.cpp:1068` | no |
| 66 | `CMD_CM_VOLUNTER_ASR` | C→S game | Accept or refuse a volunteer invite. | handled: `CharacterPrl.cpp:1378` | sends: `PacketCmd_CS.cpp:1077` | no |
| 519 | `CMD_MC_TEAM` | S→C game | HP, SP and look of a team member. | sent: `Player.cpp:420` | reads: `NetIF.cpp:185` | [yes](party.md#party-member-vitals-519) |
| 524 | `CMD_MC_TLEADER_ID` | S→C game | Team leader id of a character. | sent: `Player.cpp:441` | reads: `NetIF.cpp:186` | [yes](party.md#party-leader-notice-524) |
| 543 | `CMD_MC_TEAM_FIGHT_ASK` | S→C game | Another team challenges you to a team fight. | sent: `CharacterCmd.cpp:2384` | reads: `NetIF.cpp:209` | no |
| 581 | `CMD_MC_VOLUNTER_LIST` | S→C game | One page of the volunteer list. | sent: `CharacterPrl.cpp:1301` | reads: `NetIF.cpp:243` | no |
| 582 | `CMD_MC_VOLUNTER_STATE` | S→C game | Own volunteer state. | sent: `Character.cpp:6887` | reads: `NetIF.cpp:244` | no |
| 583 | `CMD_MC_VOLUNTER_SEL` | S→C unknown | Result of a volunteer selection. | no sender found | does not read | no |
| 584 | `CMD_MC_VOLUNTER_OPEN` | S→C game | Open the volunteer list. | sent: `CharacterPrl.cpp:1272` | reads: `NetIF.cpp:245` | no |
| 585 | `CMD_MC_VOLUNTER_ASK` | S→C game | A player invites you from the volunteer list. | sent: `CharacterPrl.cpp:1373` | reads: `NetIF.cpp:246` | no |
| 605 | `CMD_MC_CAPTAIN_ASK` | S→C unknown | Ask the team leader to confirm. | no sender found | does not read | no |
| 5001 | `CMD_PC_TEAM_INVITE` | S→C group | A player invites you to a team. | sent: `GroupServer/GroupServerAppTeam.cpp:78` | reads: `NetIF.cpp:182` | [yes](party.md#party-invite-5001--invite-cancel-5003--accept-6002--refuse-6003) |
| 5002 | `CMD_PC_TEAM_REFRESH` | S→C group | Team members changed. | sent: `GroupServer/GroupServerAppTeam.cpp:193`, `GroupServer/GroupServerAppTeam.cpp:293` | reads: `NetIF.cpp:184` | [yes](party.md#party-refresh-5002) |
| 5003 | `CMD_PC_TEAM_CANCEL` | S→C group | A team invite is cancelled or timed out. | sent: `GroupServer/GroupServerAppTeam.cpp:72`, `GroupServer/GroupServerAppTeam.cpp:578` | reads: `NetIF.cpp:183` | [yes](party.md#party-invite-5001--invite-cancel-5003--accept-6002--refuse-6003) |
| 6001 | `CMD_CP_TEAM_INVITE` | C→S group | Invite a player to the team. | handled: `GroupServerAppServ.cpp:478` | sends: `NetPkChat.cpp:97` | no |
| 6002 | `CMD_CP_TEAM_ACCEPT` | C→S group | Accept a team invite. | handled: `GroupServerAppServ.cpp:481` | sends: `NetPkChat.cpp:115` | [yes](party.md#party-invite-5001--invite-cancel-5003--accept-6002--refuse-6003) |
| 6003 | `CMD_CP_TEAM_REFUSE` | C→S group | Refuse a team invite. | handled: `GroupServerAppServ.cpp:484` | sends: `NetPkChat.cpp:105` | [yes](party.md#party-invite-5001--invite-cancel-5003--accept-6002--refuse-6003) |
| 6004 | `CMD_CP_TEAM_LEAVE` | C→S group | Leave the team. | handled: `GroupServerAppServ.cpp:487` | sends: `NetPkChat.cpp:135` | no |
| 6005 | `CMD_CP_TEAM_KICK` | C→S group | Kick a member from the team. | handled: `GroupServerAppServ.cpp:490` | sends: `NetPkChat.cpp:125` | no |

## Guild

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 401 | `CMD_CM_GUILD_PUTNAME` | C→S game | Send the name for a new guild. | handled: `CharacterPrl.cpp:751` | sends: `NetPkGuild.cpp:18` | no |
| 402 | `CMD_CM_GUILD_TRYFOR` | C→S game | Apply to join a guild. | handled: `CharacterPrl.cpp:767` | sends: `NetPkGuild.cpp:54` | no |
| 403 | `CMD_CM_GUILD_TRYFORCFM` | C→S game | Confirm that a new application replaces the old one. | handled: `CharacterPrl.cpp:772` | sends: `NetPkGuild.cpp:67` | no |
| 404 | `CMD_CM_GUILD_LISTTRYPLAYER` | C→S game | Ask for the list of applicants. | handled: `CharacterPrl.cpp:777` | sends: `NetPkGuild.cpp:75` | no |
| 405 | `CMD_CM_GUILD_APPROVE` | C→S game | Approve an applicant. | handled: `CharacterPrl.cpp:782` | sends: `NetPkGuild.cpp:244` | no |
| 406 | `CMD_CM_GUILD_REJECT` | C→S game | Reject an applicant. | handled: `CharacterPrl.cpp:787` | sends: `NetPkGuild.cpp:252` | no |
| 407 | `CMD_CM_GUILD_KICK` | C→S game | Kick a member. | handled: `CharacterPrl.cpp:792` | sends: `NetPkGuild.cpp:260` | no |
| 408 | `CMD_CM_GUILD_LEAVE` | C→S game | Leave the guild. | handled: `CharacterPrl.cpp:797` | sends: `NetPkGuild.cpp:268` | no |
| 409 | `CMD_CM_GUILD_DISBAND` | C→S game | Disband the guild. | handled: `CharacterPrl.cpp:809` | sends: `NetPkGuild.cpp:275` | no |
| 410 | `CMD_CM_GUILD_MOTTO` | C→S game | Change the guild motto. | handled: `CharacterPrl.cpp:819` | sends: `NetPkGuild.cpp:283` | no |
| 411 | `CMD_CM_GUILD_CHALLENGE` | C→S game | Challenge another guild. | handled: `CharacterPrl.cpp:835` | sends: `NetPkGuild.cpp:291` | no |
| 412 | `CMD_CM_GUILD_LEIZHU` | C→S game | Challenge the arena holder guild. | handled: `CharacterPrl.cpp:842` | sends: `NetPkGuild.cpp:300` | no |
| 413 | `CMD_CM_GUILD_PERM` | C→S game | Change the permissions of a member. | handled: `CharacterPrl.cpp:722` | sends: `PacketCmd_CS.cpp:1368` | no |
| 612 | `CMD_MC_GUILDNOTICE` | S→C unknown | Guild notice text. | no sender found | does not read | no |
| 893 | `CMD_MC_UPDATEGUILDBANKGOLD` | S→C game | Gold in the guild bank. | sent: `GameAppNet.cpp:1444`, `Player.cpp:949` | reads: `NetIF.cpp:137` | no |
| 901 | `CMD_MC_GUILD_GETNAME` | S→C game | Ask the player for a new guild name. | sent: `Guild.cpp:18` | reads: `NetIF.cpp:171` | no |
| 902 | `CMD_MC_LISTGUILD` | S→C game | Guild list for the application window. | sent: `GameDB.cpp:4329` | reads: `NetIF.cpp:172` | no |
| 903 | `CMD_MC_GUILD_TRYFORCFM` | S→C game | Ask if a new application replaces the old one. | sent: `GameDB.cpp:4463` | reads: `NetIF.cpp:173` | no |
| 904 | `CMD_MC_GUILD_LISTTRYPLAYER` | S→C game | List of applicants. | sent: `GameDB.cpp:4740` | reads: `NetIF.cpp:174` | no |
| 905 | `CMD_MC_GUILD_LISTCHALL` | S→C game | Guild challenge list. | sent: `GameDB.cpp:5679`, `Addons/packet.lua:77` | reads: `NetIF.cpp:181` | no |
| 910 | `CMD_MC_GUILD_MOTTO` | S→C unknown | Guild motto changed. | no sender found | reads: `NetIF.cpp:177` | no |
| 911 | `CMD_MC_GUILD_LEAVE` | S→C game | You left the guild. | sent: `GameDB.cpp:5165` | reads: `NetIF.cpp:178` | no |
| 912 | `CMD_MC_GUILD_KICK` | S→C game | You were kicked from the guild. | sent: `GameDB.cpp:5096` | reads: `NetIF.cpp:179` | no |
| 913 | `CMD_MC_GUILD_INFO` | S→C game | Guild id, name and motto of a character. | sent: `Character.cpp:6694` | reads: `NetIF.cpp:180` | no |
| 5030 | `CMD_PC_GUILD` | S→C group | Guild member list and member changes. | sent: `GroupServer/DBConnect.cpp:1231`, `GroupServer/DBConnect.cpp:1283` | reads: `NetIF.cpp:175` | no |
| 5031 | `CMD_PC_GUILD_PERM` | S→C group | Permissions of a guild member changed. | sent: `GroupServerAppServ.cpp:404` | reads: `NetIF.cpp:176` | no |
| 5417 | `CMD_PC_GUILDNOTICE` | S→C group | Guild notice text. | sent: `GroupServer/GroupServerAppChat.cpp:317` | reads: `NetIF.cpp:72` | no |
| 6414 | `CMD_CP_GUILDBANK` | C→S group | Guild bank action. | handled: `GroupServerAppServ.cpp:298` | sends: `PacketCmd_CS.cpp:135` | no |

## Friends

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 5011 | `CMD_PC_FRND_INVITE` | S→C group | A player asks you to be a friend. | sent: `GroupServer/GroupServerAppFrnd.cpp:78` | reads: `NetIF.cpp:162` | no |
| 5012 | `CMD_PC_FRND_REFRESH` | S→C group | Friend list change: add, delete, online, offline. | sent: `GroupServer/GroupServerAppFrnd.cpp:140`, `GroupServer/GroupServerAppFrnd.cpp:171` | reads: `NetIF.cpp:164` | no |
| 5013 | `CMD_PC_FRND_CANCEL` | S→C group | A friend request is cancelled or timed out. | sent: `GroupServer/GroupServerAppFrnd.cpp:72`, `GroupServer/GroupServerAppFrnd.cpp:243` | reads: `NetIF.cpp:163` | no |
| 5015 | `CMD_PC_FRND_CHANGE_GROUP` | S→C group | A friend group was renamed. | sent: `GroupServer/GroupServerAppFrnd.cpp:225` | does not read | no |
| 5016 | `CMD_PC_FRND_REFRESH_INFO` | S→C group | Personal data of a friend. | sent: `GroupServer/GroupServerAppPrsn.cpp:49` | reads: `NetIF.cpp:165` | no |
| 5017 | `CMD_PC_CHANGE_PERSONINFO` | S→C group | Own motto and icon changed. | sent: `GroupServer/GroupServerAppPrsn.cpp:30` | reads: `NetIF.cpp:167` | no |
| 5018 | `CMD_PC_FRND_DEL_GROUP` | S→C unknown | A friend group was deleted. | no sender found | does not read | no |
| 5019 | `CMD_PC_FRND_ADD_GROUP` | S→C unknown | A friend group was added. | no sender found | does not read | no |
| 5020 | `CMD_PC_FRND_MOVE_GROUP` | S→C unknown | A friend moved to another group. | no sender found | does not read | no |
| 5021 | `CMD_PC_QUERY_PERSONINFO` | S→C unknown | Personal data of a player. | no sender found | does not read | no |
| 6011 | `CMD_CP_FRND_INVITE` | C→S group | Ask a player to be a friend. | handled: `GroupServerAppServ.cpp:514` | sends: `NetPkChat.cpp:142` | no |
| 6012 | `CMD_CP_FRND_ACCEPT` | C→S group | Accept a friend request. | handled: `GroupServerAppServ.cpp:517` | sends: `NetPkChat.cpp:156` | no |
| 6013 | `CMD_CP_FRND_REFUSE` | C→S group | Refuse a friend request. | handled: `GroupServerAppServ.cpp:520` | sends: `NetPkChat.cpp:149` | no |
| 6014 | `CMD_CP_FRND_DELETE` | C→S group | Delete a friend. | handled: `GroupServerAppServ.cpp:523` | sends: `NetPkChat.cpp:163` | no |
| 6015 | `CMD_CP_FRND_CHANGE_GROUP` | C→S group | Rename a friend group. | handled: `GroupServerAppServ.cpp:526` | never sends | no |
| 6016 | `CMD_CP_FRND_REFRESH_INFO` | C→S group | Ask for the personal data of a friend. | handled: `GroupServerAppServ.cpp:529` | sends: `NetPkChat.cpp:171` | no |
| 6017 | `CMD_CP_CHANGE_PERSONINFO` | C→S group | Change own motto and icon. | handled: `GroupServerAppServ.cpp:532` | sends: `NetPkChat.cpp:179` | no |
| 6018 | `CMD_CP_FRND_DEL_GROUP` | C→S group | Delete a friend group. | no handler found | never sends | no |
| 6019 | `CMD_CP_FRND_ADD_GROUP` | C→S group | Add a friend group. | no handler found | never sends | no |
| 6020 | `CMD_CP_FRND_MOVE_GROUP` | C→S group | Move a friend to another group. | no handler found | never sends | no |
| 6021 | `CMD_CP_QUERY_PERSONINFO` | C→S group | Ask for the personal data of a player. | no handler found | never sends | no |

## Master and apprentice

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 71 | `CMD_CM_MASTER_INVITE` | C→S game | Ask a player to become your master. | handled: `CharacterPrl.cpp:1501` | sends: `PacketCmd_CS.cpp:1104` | no |
| 72 | `CMD_CM_MASTER_ASR` | C→S game | Accept or refuse a master request. | handled: `CharacterPrl.cpp:1564` | sends: `PacketCmd_CS.cpp:1113` | no |
| 73 | `CMD_CM_MASTER_DEL` | C→S game | Leave your master. | handled: `CharacterPrl.cpp:1622` | sends: `PacketCmd_CS.cpp:1142` | no |
| 74 | `CMD_CM_PRENTICE_DEL` | C→S game | Remove an apprentice. | no handler found | sends: `PacketCmd_CS.cpp:1151` | no |
| 75 | `CMD_CM_PRENTICE_INVITE` | C→S game | Ask a player to become your apprentice. | no handler found | sends: `PacketCmd_CS.cpp:1123` | no |
| 76 | `CMD_CM_PRENTICE_ASR` | C→S game | Accept or refuse an apprentice request. | no handler found | sends: `PacketCmd_CS.cpp:1132` | no |
| 591 | `CMD_MC_MASTER_ASK` | S→C game | A player asks you to be the master. | sent: `CharacterPrl.cpp:1558` | reads: `NetIF.cpp:250` | no |
| 592 | `CMD_MC_PRENTICE_ASK` | S→C unknown | A player asks you to be the apprentice. | no sender found | reads: `NetIF.cpp:255` | no |
| 5041 | `CMD_PC_MASTER_REFRESH` | S→C group | Master and apprentice list change. | sent: `GroupServer/GroupServerAppMaster.cpp:183`, `GroupServer/GroupServerAppMaster.cpp:232` | reads: `NetIF.cpp:251` | no |
| 5042 | `CMD_PC_MASTER_CANCEL` | S→C group | A master request is cancelled or timed out. | sent: `GroupServer/GroupServerAppMaster.cpp:281`, `GroupServer/GroupServerAppMaster.cpp:289` | reads: `NetIF.cpp:252` | no |
| 5043 | `CMD_PC_MASTER_REFRESH_INFO` | S→C group | Personal data of the master. | sent: `GroupServer/GroupServerAppMaster.cpp:24` | reads: `NetIF.cpp:253` | no |
| 5044 | `CMD_PC_PRENTICE_REFRESH_INFO` | S→C group | Personal data of an apprentice. | sent: `GroupServer/GroupServerAppMaster.cpp:49` | reads: `NetIF.cpp:254` | no |
| 6031 | `CMD_CP_MASTER_REFRESH_INFO` | C→S group | Ask for the personal data of the master. | handled: `GroupServerAppServ.cpp:508` | sends: `PacketCmd_CS.cpp:1160` | no |
| 6032 | `CMD_CP_PRENTICE_REFRESH_INFO` | C→S group | Ask for the personal data of an apprentice. | handled: `GroupServerAppServ.cpp:511` | sends: `PacketCmd_CS.cpp:1168` | no |

## GM mail

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 92 | `CMD_CM_GM_SEND` | C→S game | Send a mail to the GM. | handled: `CharacterPrl.cpp:1850` | sends: `PacketCmd_CS.cpp:1184` | no |
| 93 | `CMD_CM_GM_RECV` | C→S game | Get the GM answer mail. | handled: `CharacterPrl.cpp:1870` | sends: `PacketCmd_CS.cpp:1194` | no |
| 597 | `CMD_MC_GM_MAIL` | S→C game | GM answer mail. | sent: `CharTrade.cpp:3517`, `CharTrade.cpp:3554` | reads: `NetIF.cpp:262` | no |
| 877 | `CMD_MC_BEGIN_GM_SEND` | S→C game | Open the GM mail window. | sent: `Character.cpp:6801` | reads: `NetIF.cpp:117` | no |
| 878 | `CMD_MC_BEGIN_GM_RECV` | S→C game | Open the GM answer window. | sent: `Character.cpp:6808` | reads: `NetIF.cpp:118` | no |

## GM tools

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 526 | `CMD_MC_QUERY_CHA` | S→C game | Result of a GM character query. | sent: `GameAppNet.cpp:1623`, `GameAppNet.cpp:1646` | reads: `NetIF.cpp:194` | no |
| 527 | `CMD_MC_QUERY_CHAITEM` | S→C unknown | Result of a GM item query. | no sender found | does not read | no |
| 528 | `CMD_MC_CALL_CHA` | S→C unknown | GM calls a character to self. | no sender found | does not read | no |
| 529 | `CMD_MC_GOTO_CHA` | S→C unknown | GM goes to a character. | no sender found | does not read | no |
| 530 | `CMD_MC_KICK_CHA` | S→C unknown | GM kicks a character. | no sender found | does not read | no |
| 534 | `CMD_MC_QUERY_CHAPING` | S→C game | Ping of a character to the game server, for a GM query. | sent: `CharacterPrl.cpp:254` | reads: `NetIF.cpp:196` | no |
| 5032 | `CMD_PC_GM_INFO` | S→C group | List of GMs, shown in the friend window. | sent: `GroupServer/GroupServerAppFrnd.cpp:305`, `GroupServer/GroupServerAppFrnd.cpp:327` | reads: `NetIF.cpp:169` | no |

## Other

| Opcode | Name | Direction | Purpose | Server | Original client | Ours |
|---|---|---|---|---|---|---|
| 27 | `CMD_CM_TIGER_START` | C→S game | Start the slot machine. | handled: `CharacterPrl.cpp:1196` | sends: `PacketCmd_CS.cpp:1007` | no |
| 28 | `CMD_CM_TIGER_STOP` | C→S game | Stop the slot machine. | handled: `CharacterPrl.cpp:1214` | sends: `PacketCmd_CS.cpp:1018` | no |
| 52 | `CMD_CM_CHEAT_CHECK` | C→S game | Answer to the anti-bot question 598. | handled: `CharacterPrl.cpp:1894` | sends: `PacketCmd_CS.cpp:1210` | no |
| 86 | `CMD_CM_BIDUP` | C→S game | Bid in an auction. | handled: `CharacterPrl.cpp:1903` | sends: `PacketCmd_CS.cpp:1329` | no |
| 95 | `CMD_CM_ITEM_LOTTERY_ASK` | C→S game | Set the numbers of a lottery ticket. | handled: `CharacterPrl.cpp:943` | sends: `PacketCmd_CS.cpp:862` | no |
| 96 | `CMD_CM_ITEM_LOTTERY_ASR` | C→S game | Confirm or cancel a lottery ticket. | no handler found | sends: `PacketCmd_CS.cpp:854` | no |
| 97 | `CMD_CM_RANK` / `CMD_CM_CAPTAIN_CONFIRM_ASR` (same value) | C→S game | `RANK`: ask for the rank list. `CAPTAIN_CONFIRM_ASR`: answer to 605. The server reads every 97 as `RANK`. | handled: `CharacterPrl.cpp:33` | sends: `ProCirculateCS.cpp:514` | no |
| 98 | `CMD_CM_ITEM_AMPHITHEATER_ASK` | C→S game | Vote in the arena ballot. | no handler found | sends: `PacketCmd_CS.cpp:889` | no |
| 110 | `CMD_CM_DailyBuffRequest` | C→S game | Ask for the daily buff information. | handled: `CharacterPrl.cpp:425` | sends: `PacketCmd_CS.cpp:1027` | no |
| 356 | `CMD_CM_REQUEST_DROP_RATE` | C→S game | Ask for the current drop rate. | handled: `CharacterPrl.cpp:1933` | sends: `PacketCmd_CS.cpp:1376` | no |
| 357 | `CMD_CM_REQUEST_EXP_RATE` | C→S game | Ask for the current experience rate. | handled: `CharacterPrl.cpp:1944` | sends: `PacketCmd_CS.cpp:1382` | no |
| 371 | `CMD_CM_GARNER2_REORDER` | C→S game | Ask the chaos arena rank list to sort again. | no handler found | never sends | no |
| 372 | `CMD_CM_ANTIINDULGENCE` | C→S game | Anti-addiction play time report. | handled: `CharacterPrl.cpp:1928` | sends: `PacketCmd_CS.cpp:1339` | no |
| 586 | `CMD_MC_LISTAUCTION` | S→C game | Auction list. | sent: `Auction.cpp:133`, `Auction.cpp:158` | reads: `NetIF.cpp:265` | no |
| 587 | `CMD_MC_RANK` | S→C game | Player rank list. | sent: `GameDB.cpp:228` | reads: `NetIF.cpp:135` | no |
| 598 | `CMD_MC_CHEAT_CHECK` | S→C game | Anti-bot question. | sent: `Character.cpp:7334` | reads: `NetIF.cpp:264` | no |
| 603 | `CMD_MC_ITEM_LOTTERY_ASK` | S→C unknown | Ask for the lottery numbers. | no sender found | does not read | no |
| 604 | `CMD_MC_ITEM_LOTTERY_ASR` | S→C game | Result of a lottery ticket. | sent: `CharacterCmd.cpp:3168`, `CharacterCmd.cpp:3180` | does not read | no |
| 606 | `CMD_MC_ITEM_AMPHITHEATER_ASR` | S→C unknown | Result of an arena vote. | no sender found | does not read | no |
| 615 | `CMD_MC_RecDailyBuffInfo` | S→C game | Daily buff information. | sent: `CharScript.cpp:6118` | reads: `NetIF.cpp:266` | no |
| 623 | `CMD_MC_REQUEST_DROP_RATE` | S→C game | Current drop rate. | sent: `CharacterPrl.cpp:1938` | reads: `NetIF.cpp:268` | no |
| 624 | `CMD_MC_REQUEST_EXP_RATE` | S→C game | Current experience rate. | sent: `CharacterPrl.cpp:1949` | reads: `NetIF.cpp:269` | no |
| 873 | `CMD_MC_BEGIN_TIGER` | S→C game | Open the slot machine window. | sent: `Character.cpp:6823` | reads: `NetIF.cpp:115` | no |
| 874 | `CMD_MC_TIGER_ITEM_ID` | S→C game | Slot machine result. | sent: `CharacterPrl.cpp:1235` | reads: `NetIF.cpp:234` | no |
| 875 | `CMD_MC_TIGER_STOP` | S→C game | Slot machine stopped. | sent: `Character.cpp:6897` | reads: `NetIF.cpp:256` | no |
| 879 | `CMD_MC_BEGIN_ITEM_LOTTERY` | S→C game | Open the lottery window. | sent: `Character.cpp:6730` | does not read | no |
| 880 | `CMD_MC_BEGIN_ITEM_AMPHITHEATER` | S→C unknown | Open the arena ballot window. | no sender found | does not read | no |
| 5101 | `CMD_PC_GARNER2_ORDER` | S→C group | Chaos arena rank list. | sent: `GroupServerAppServ.cpp:2168` | reads: `NetIF.cpp:236` | no |
| 6023 | `CMD_CP_REPORT_WG` | C→S group | Report the use of a cheat program. | handled: `GroupServerAppServ.cpp:424` | sends: `PacketCmd_CS.cpp:999` | no |

## No sender found

These opcodes look dead in this server build: no site sends them (S→C), or no site handles them (C→S). Other server
builds can use them.

Server to client, no sender found:

- 518 `CMD_MC_ALARM`: the original client does not read it.
- 527 `CMD_MC_QUERY_CHAITEM`: the original client does not read it.
- 528 `CMD_MC_CALL_CHA`: the original client does not read it.
- 529 `CMD_MC_GOTO_CHA`: the original client does not read it.
- 530 `CMD_MC_KICK_CHA`: the original client does not read it.
- 531 `CMD_MC_SYNDEFAULTSKILL`: the original client reads it.
- 545 `CMD_MC_ITEM_REPAIR_ASR`: the original client does not read it.
- 567 `CMD_MC_STORE_AFFICHE`: the original client does not read it.
- 583 `CMD_MC_VOLUNTER_SEL`: the original client does not read it.
- 592 `CMD_MC_PRENTICE_ASK`: the original client reads it.
- 596 `CMD_MC_SAY2CAMP`: the original client reads it.
- 603 `CMD_MC_ITEM_LOTTERY_ASK`: the original client does not read it.
- 605 `CMD_MC_CAPTAIN_ASK`: the original client does not read it.
- 606 `CMD_MC_ITEM_AMPHITHEATER_ASR`: the original client does not read it.
- 612 `CMD_MC_GUILDNOTICE`: the original client does not read it.
- 806 `CMD_MC_TRADERESULT`: the original client reads it.
- 809 `CMD_MC_TRADE_UPDATE`: the original client does not read it.
- 810 `CMD_MC_EXCHANGE_UPDATE`: the original client does not read it.
- 823 `CMD_MC_MISSIONLIST`: the original client does not read it.
- 824 `CMD_MC_MISSIONTALK`: the original client does not read it.
- 840 `CMD_MC_UPDATEBOAT_PART`: the original client reads it.
- 842 `CMD_MC_BOAT_LIST`: the original client does not read it.
- 846 `CMD_MC_BOAT_BAGLIST`: the original client does not read it.
- 850 `CMD_MC_ENTITY_BEGINESEE`: the original client does not read it.
- 851 `CMD_MC_ENTITY_ENDSEE`: the original client does not read it.
- 855 `CMD_MC_STALL_UPDATE`: the original client does not read it.
- 880 `CMD_MC_BEGIN_ITEM_AMPHITHEATER`: the original client does not read it.
- 910 `CMD_MC_GUILD_MOTTO`: the original client reads it.
- 932 `CMD_MC_LOGOUT`: the original client does not read it.
- 944 `CMD_MC_SEND_HANDSHAKE`: the original client reads it.
- 5018 `CMD_PC_FRND_DEL_GROUP`: the original client does not read it.
- 5019 `CMD_PC_FRND_ADD_GROUP`: the original client does not read it.
- 5020 `CMD_PC_FRND_MOVE_GROUP`: the original client does not read it.
- 5021 `CMD_PC_QUERY_PERSONINFO`: the original client does not read it.

Client to server, no handler found:

- 9 `CMD_CM_SYNSKILLBAG`: the original client never sends it.
- 36 `CMD_CM_KITBAGTEMPlocks`: the original client never sends it.
- 47 `CMD_CM_STORE_AFFICHE`: the original client never sends it.
- 74 `CMD_CM_PRENTICE_DEL`: the original client sends it.
- 75 `CMD_CM_PRENTICE_INVITE`: the original client sends it.
- 76 `CMD_CM_PRENTICE_ASR`: the original client sends it.
- 91 `CMD_CM_SAY2CAMP`: the original client sends it.
- 96 `CMD_CM_ITEM_LOTTERY_ASR`: the original client sends it.
- 98 `CMD_CM_ITEM_AMPHITHEATER_ASK`: the original client sends it.
- 310 `CMD_CM_REQUESTAGENCY`: the original client never sends it.
- 323 `CMD_CM_MISSIONLIST`: the original client never sends it.
- 324 `CMD_CM_MISSIONTALK`: the original client never sends it.
- 371 `CMD_CM_GARNER2_REORDER`: the original client never sends it.
- 6018 `CMD_CP_FRND_DEL_GROUP`: the original client never sends it.
- 6019 `CMD_CP_FRND_ADD_GROUP`: the original client never sends it.
- 6020 `CMD_CP_FRND_MOVE_GROUP`: the original client never sends it.
- 6021 `CMD_CP_QUERY_PERSONINFO`: the original client never sends it.
