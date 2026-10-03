# GM commands

A GM command is a chat line: `&<command> <arguments>`. Separate the arguments with commas, for example
`&make 481,1`. Send it with the `say` step of `Top.Client.Cli`. A refusal comes only as a system message, which the
client does not decode. `Observer` is a level 99 GM.

Source: `CCharacter::DoGMCommand`, `sources/Server/GameServer/src/CharacterSuperCmd.cpp:65-1249` in the reference
repo. A `cmd_<word>` key in the server config changes the word of a command (`Config.cpp:496-555`). `mute`, `scroll`,
`generatecharbag` and the words in quotes in `DoGMCommand` have no key.

Access, in table order:

- `move` to `goto`: GM level 1 or more.
- `mute` and `kick`: GM level 2 or more.
- All others: GM level 99, on a weekday in the `gmcmd` config list (0 is Sunday).
- `addmoney` and all commands after it: also `supercmd = 1` in the config. The test server has it.

| Command | Description |
|---|---|
| `&move <x>,<y>[,<map>]` | go to the point in meters, on the map or on the current map. With 4 values the server uses the current map and reads the copy number from `<y>` (defect) |
| `&notice <text>` | send a notice to all players |
| `&hide` | make the sender invisible |
| `&unhide` | make the sender visible |
| `&goto <character name>` | go to the character |
| `&mute <character name>,<minutes>` | stop the chat of the character. 0 or less removes the mute. After it works, the sender also gets a refusal message (defect) |
| `&kick <character name>[,<time>]` | disconnect the player. The server ignores `<time>` |
| `&reload <iteminfo, skillinfo or characterinfo>` | read the table file again |
| `&relive` | does nothing: the server only writes a log line |
| `&qcha <character name>` | show the map, position and id of the character |
| `&qitem <character name>` | show the items of the character |
| `&call <character name>` | bring the character to the sender |
| `&gamesvrstop <seconds>` | stop the game server after the time |
| `&updateall` | read the Lua scripts and the NPC scripts again |
| `&reloadcfg <file>` | read the config file again |
| `&harmlog=1` | start the damage log |
| `&harmlog=0` | stop the damage log |
| `&misreload` | read the Lua scripts and the NPC scripts again, the same as `updateall` |
| `&reload_ai` | read the AI scripts again |
| `&setrecord <mission id>` | set the history record of the mission |
| `&clearrecord <mission id>` | clear the history record of the mission |
| `&setflag <mission id>,<flag>` | set the mission flag |
| `&clearflag <mission id>,<flag>` | clear the mission flag |
| `&addmission <mission id>,<script id>` | give the mission to the sender |
| `&clearmission <mission id>` | cancel the mission |
| `&delmission <mission id>` | delete the mission |
| `&missdk` | read the Lua SDK again |
| `&misclear` | clear all mission flags and triggers of the sender |
| `&isblock` | tell if the sender stands in a blocked cell |
| `&pet <character id>` | summon a pet. The server does not read the argument and always uses id 0 (defect) |
| `&summon <character id>[,<life time>][,<AI type>]` | summon one monster near the sender |
| `&summonex <character id>[,<count>][,<0 or 1: activate view>][,<AI type>]` | summon monsters within 20 m of the sender |
| `&kill <monster name>[,<range in meters, 8>][,<count, 0 is all>]` | remove the monsters with that name near the sender |
| `&addmoney <gold>` | give gold to the sender |
| `&addimp <count>` | run the Lua function `GiveIMP` for the sender |
| `&addexp <experience>` | give experience to the sender |
| `&addlifeexp <experience>` | give life skill experience to the sender |
| `&addsailexp <experience>` | give sail experience to the sender |
| `&addcess <points>` | add trade tax points |
| `&setcesslevel <level>` | set the trade level |
| `&make <item id>,<count>[,<spawn type>]` | put the items in the first free bag slots. A count outside 0-100 is 10. With a 4th value: 1 is the bag, other values the ground, and the server ignores the 3rd value |
| `&attr <attribute id>,<value>[,<world id>]` | set an attribute of the sender or the character: 0 level, 4 job, 8 gold (`ChaAttrType.h`) |
| `&itemattr <1 equipment or 2 bag>,<slot>,<attribute id>,<value>` | set an attribute of the item |
| `&setexpiration <1 equipment or 2 bag>,<slot>,<seconds>` | set the expiration of the item. 0 removes it. The server adds the value as seconds, not as minutes (defect) |
| `&light <range>` | set the light range of the sender on the big map |
| `&seeattr <world id>,<attribute id>` | show an attribute of the character |
| `&forge <levels>[,<bag slot, 0>]` | add forge levels to the item, if it can be forged |
| `&skill <skill id>,<level>[,0]` | teach the skill to the sender. A 3rd value 0 skips the learn limits |
| `&delitem <item id>,<count>,<from>,<slot>,<to>,<forcible>` | remove items. From: 0 both, 1 equipment, 2 bag. Count 0: all. Slot -1: all. To: 0 ground, 1 bag, 2 delete. Forcible: 1 skips the bag lock checks |
| `&lua <code>` | run Lua code on this game server |
| `&lua_all <code>` | run Lua code on all game servers |
| `&setping <milliseconds>` | set the ping of the sender for the move timing |
| `&getping` | show the ping of the sender |
| `&senddata <interval>,<length>` | set the test send interval and length of the sender |
| `&setpinginfo <interval>,<length>` | set the ping interval and length of the sender |
| `&addkb <slots>[,<world id>]` | add bag slots to the sender or the character |
| `&itemvalid <bag slot>,<0 or 1>` | set the item in the bag slot as valid or not valid |
| `&scroll <text>` | send a scroll notice to all players |
| `&generatecharbag <character name>` | write the bag, bank and equipment of the character to `<name>.txt` on the server |
| `&editcharbag <character name>,<slot>,<attribute id>,<value>,<1 bag, 2 equipment or 3 bank>` | set an attribute of the item |
