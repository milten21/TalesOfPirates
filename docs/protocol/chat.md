# Chat packets

`Messages/` is short for `src/Top.Legacy.Protocol/Messages/`.

## Say (1)

**Type**

- C→S, through the gate to the game server, opcode 1 (`CMD_CM_SAY`). Writer: `SayRequest`
  (`Messages/Chat/SayRequest.cs`) through `SayEncoder` (`src/Top.Client.LegacyBridge/Chat/SayEncoder.cs`).

**Payload**

- `string` text, nothing else. Confirmed against the client send (`ProCirculateCS.cpp:518-524`, `CProCirculate::Say`,
  one `WriteSequence` of the text and its terminating zero), the gate (`ToClient.cpp:497`, one `ReadString`) and the
  game server (`CharacterPrl.cpp:343`, one `READ_SEQ`).

**Server**

- The gate (`ToClient.cpp:485-531`):
  - `:502`: a line that starts with `&` goes to `ToClient::DoCommand` (`:80-101`). Its body is commented out and it
    always gives `false`, so the line continues.
  - `:506-509`: a line that holds `#21` is dropped. No message.
  - `:511-526`: a muted player (`m_estop`) gets a system message 517 (`CMD_MC_SYSINFO`), and the line is dropped.
  - Else `:529`: the gate sends the packet on to the game server.
- The game server (`CharacterPrl.cpp:325-385`), in this order. A system message is opcode 517 to self
  (`CCharacter::SystemNotice`, `Character.cpp:2003-2019`):
  - `:328-333`: the server drops a line that comes less than `say_interval` after the last line that passed this
    check. The sender gets a system message (do not chat so often). The check counts every line, GM commands too.
    A dropped line does not restart the interval. The interval is a config value in seconds (`Config.cpp:253-255`).
    Its default in the code is 3 s (`Config.cpp:42`); `server/GameServer/GameServer00.cfg:42` sets 1 s, the other
    configs 0.
  - `:336-341`: a character with no map: the line is only logged.
  - `:346-354`: a line that starts with `&` is a GM command. GM level 0 or more than 150 gets a system message (no
    permission). Else `CCharacter::DoCommand` (`CharacterSuperCmd.cpp:22`) splits the command word at the first
    space and runs `DoGMCommand` (`:65`). The command words come from the server config (`Config.cpp:419-541`).
    Two that the test tools use:
    - `&make <item id>,<count>` (`CharacterSuperCmd.cpp:727-772`): a count outside 0-100 becomes 10. A third value
      is the spawn type. With four values, the fourth value selects the bag (1) or the ground, and the server ignores
      the third value. In the bag, `AddItem` (`Character.cpp:3509-3541`) puts the items in the first free bag slots.
      It sends a KITBAG NotiAction to self and a system message that names the item. A full bag gives ITEM_FAILED
      -4 (see [actions.md](actions.md#item_failed-15)); an unknown item id gives a system message.
    - `&attr <attribute id>,<value>[,<world id>]` (`CharacterSuperCmd.cpp:775-818`) sets one attribute
      (`ChaAttrType.h`: 0 level, 4 job) and sends the attribute sync 509.
  - `:356-359`: a line that starts with `$$` goes to `DoCommand_CheckStatus`.
  - Else `:368-370`: the Lua function `HandleChat` (`server/GameServer/Addons/chathandler.lua:15`) runs. It takes a
    line that starts with `/` as a script command: it answers with a notice and drops the line. `:371-375`: on a
    blind-chaos map a PK player gets a system message, and the line is dropped.
  - Else `:377-382`: `CMD_MC_SAY` (501) with the speaker's world id, the text and the chat colour goes to the
    eyeshot.

**Client**

- `SayCommand(Text)` (`src/Top.Client.Messages/Chat/Commands/SayCommand.cs`). Only `Top.Client.Cli` sends it now:
  the `say <text...>` step. The step keeps the `--say-interval` (default 1 s) and 0.1 s more between two lines, so
  that the server does not drop a line. The game client has no chat UI yet.
- **The original client** sends opcode 1 from the chat input on the sight channel (`UICozeForm.cpp:988-1013`), for a
  face with Ctrl and a digit (`UICozeForm.cpp:1436`, the text `***<digit>`), and from the face menus
  (`UICozeForm.cpp:1777`, `UIStartForm.cpp:1193`). On the sight channel it refuses the same line twice in 1 s with a
  message of its own, filters the text (`CTextFilter::Filter`) and replaces `#21` with `#99`.

**Gaps**

- The system message 517 has no decoder. A line that the server drops, and a GM command that it refuses, look the
  same as a line that it took.
- `CMD_MC_SAY` (501) has no decoder: the client does not show chat.
- Our sender does not filter the text and does not replace `#21`. The gate drops a line that holds `#21`.
