# Item and kitbag packets

See [README.md](README.md) for the file path conventions. The kitbag syncs that come as NotiAction are in
[actions.md](actions.md#kitbag-6-kitbagtmp-28-bank-26-guildbank-30).

## TemporaryKitbagSync (35) / reply (554)

`CMD_CM_KITBAGTEMP_SYNC` = `CMD_CM_BASE + 35` (`NetCommand.h:103`), client to server. `CMD_MC_KITBAGTEMP_SYNC` =
`CMD_MC_BASE + 54` (`NetCommand.h:349`), server to client. Our constants are `Opcode.TemporaryKitbagSync` and
`Opcode.TemporaryKitbagSyncReply` (`src/Top.Legacy.Protocol/Packets/Opcode.cs`).

**Payload.**

- Request: empty. Our writer is `TemporaryKitbagSyncRequest`
  (`src/Top.Legacy.Protocol/Messages/Play/TemporaryKitbagSyncRequest.cs`).
- Reply: one `WriteKitbag` block with sync type INIT (`CharacterPrl.cpp:1414-1416`), so it carries the capacity. The
  block is the one in [actions.md](actions.md#kitbag-6-kitbagtmp-28-bank-26-guildbank-30). There is no world id. Our
  reader is `TemporaryKitbagSyncReply` (`src/Top.Legacy.Protocol/Messages/Play/TemporaryKitbagSyncReply.cs`).

**Server.** The only handler is `CPlayer` command dispatch, `case CMD_CM_KITBAGTEMP_SYNC` (`CharacterPrl.cpp:1405-1428`),
and it is the only writer of 554.

- No temporary bag (`m_pCKitbagTmp` is null): no reply.
- Otherwise it writes the temporary bag of the main character with INIT. It does not set the change flags first, so
  the reply lists only the slots whose flag is still set; the capture below of an empty bag lists none.
- Then, if the player has a pending item-mall item (`GetStoreItemID() > 0`), it gives it
  (`g_StoreSystem.Accept`). That item then reaches the temporary bag with a KITBAGTMP NotiAction.

The original client sends the request once when the world scene starts (`WorldScene.cpp:2079`) and reads the reply
in `SC_SyncKitbagTemp` (`PacketCmd_SC.cpp:3017-3035`).

Captured on 2026-09-28 from the local stack for `T1`: `00 0020 FFFF`, an INIT sync with capacity 32 and no slots.

**Client.**

- `WorldScene._Ready` sends `SyncTemporaryBagCommand` once per world entry. `SyncTemporaryBagEncoder`
  (`src/Top.Client.LegacyBridge/Inventory/SyncTemporaryBagEncoder.cs`) writes the empty request.
- `TemporaryBagSyncDecoder` (`src/Top.Client.LegacyBridge/Inventory/TemporaryBagSyncDecoder.cs`) gives
  `TemporaryBagChangedEvent(BagSync)` with the capacity. The subscriber is `WorldScene.ChangeTemporaryBag`, which calls
  `CharacterInventory.ChangeTemporaryBag`.

**Gaps.**

- The capacity (32 on the reference server) is dropped. `CharacterInventory` holds 16 temporary-bag slots and drops a slot from
  16 up with a `Log.Warning`.
- A map switch sends a new EnterMap, but we send the request only on the first world entry. See finding 1 in
  [README.md](README.md#most-important-findings).

## KitbagLock (31) / KitbagUnlock (32) / lock state (553)

`CMD_CM_KITBAG_LOCK` = `CMD_CM_BASE + 31` (`NetCommand.h:99`) and `CMD_CM_KITBAG_UNLOCK` = `CMD_CM_BASE + 32`
(`NetCommand.h:100`), client to server. `CMD_MC_KITBAG_CHECK_ASR` = `CMD_MC_BASE + 53` (`NetCommand.h:348`), server to
client. Our constants are `Opcode.KitbagLock`, `Opcode.KitbagUnlock` and `Opcode.KitbagLockState`
(`src/Top.Legacy.Protocol/Packets/Opcode.cs`).

**Payload.**

- Lock: empty. Our writer is `KitbagLockRequest` (`src/Top.Legacy.Protocol/Messages/Play/KitbagLockRequest.cs`).
- Unlock: one string, the security code as plain text. Our writer is `KitbagUnlockRequest`
  (`src/Top.Legacy.Protocol/Messages/Play/KitbagUnlockRequest.cs`).
- Lock state: one byte, 0 unlocked, 1 locked. There is no error code. Our reader is `KitbagLockStateReply`
  (`src/Top.Legacy.Protocol/Messages/Play/KitbagLockStateReply.cs`).

**Server.** `CPlayer` command dispatch reads 31 at `CharacterPrl.cpp:974-978` and 32 at `CharacterPrl.cpp:1116-1121`,
and calls the main character. Each writer of 553 sends to self:

- `CCharacter::Cmd_LockKitbag` (`CharacterCmd.cpp:3436-3449`): locks the kitbag if it is not locked, then writes
  state 1. A lock never fails.
- `CCharacter::Cmd_UnlockKitbag` (`CharacterCmd.cpp:3452-3469`): unlocks when the code equals the player's second
  password, or when the account has no second password. Then it writes the state: 0 after an unlock, 1 after a wrong
  code. A wrong code gives the same reply as a lock.
- `CCharacter::Cmd_CheckKitbagState` (`CharacterCmd.cpp:3472-3481`), the answer to `CMD_CM_KITBAG_CHECK` 33: the
  current state.
- Login (`CharacterCmd.cpp:196-205`, `chLogin` 0): with auto-lock on and the kitbag unlocked, the server calls
  `Cmd_LockKitbag` before it writes EnterMap, so a 553 with state 1 comes before the EnterMap. EnterMap itself carries
  the lock state (see [EnterMap](view.md#entermap-516)).

The original client locks from the inventory form's lock button after a yes/no box (`UIEquipForm.cpp:2025-2067`),
unlocks from the security-code form (`UIDoublePwdForm.cpp:244-247`), and reads 553 in `NetKitbagCheckAnswer`
(`NetProtocol.cpp:2995-3009`): "locked" while it shows locked means a wrong code, "unlocked" closes the form.

**Client.**

- The Lock button in the Inventory title bar emits `InventoryWindow.BagLockRequested`. `HudScreen` passes it on, and
  `WorldScene.LockBag` sends `CharacterInventory.LockBag()`, a `LockBagCommand`. `LockBagEncoder`
  (`src/Top.Client.LegacyBridge/Inventory/LockBagEncoder.cs`) writes the empty request.
- While locked, the window shows `BagUnlockForm`, one strip with the notice, the code field and the Unlock button. The
  Locked button puts focus in the field. The Unlock button emits the code when `SecurityCode.IsValid` accepts it. `WorldScene.UnlockBag` sends `CharacterInventory.UnlockBag(code)`, an
  `UnlockBagCommand`, which marks the inventory as waiting for the unlock. `UnlockBagEncoder`
  (`src/Top.Client.LegacyBridge/Inventory/UnlockBagEncoder.cs`) writes the code.
- `BagLockDecoder` (`src/Top.Client.LegacyBridge/Inventory/BagLockDecoder.cs`) gives `BagLockReportedEvent(IsLocked)`
  for every 553. The subscriber is `WorldScene.ChangeBagLock`, which calls `CharacterInventory.ChangeBagLock`. The
  inventory takes the lock state only from this reply and from EnterMap, never from the request. A locked reply
  while an unlock waits gives `BagLockResult.CodeRejected`: the form clears the field, keeps the focus in it, and the
  HUD toast shows "Wrong security code". The original client opens a message box. A code of the wrong length is not
  sent; the toast shows "Use 6 to 12 digits".

**Gaps.**

- `CMD_CM_KITBAG_CHECK` 33 and `CMD_CM_KITBAG_AUTOLOCK` 34 are not sent. The auto-lock flag of EnterMap is dropped.
- The 553 that auto-lock sends before EnterMap arrives while no world subscriber exists and is dropped. EnterMap
  carries the same state, so nothing is lost.
- The server lock blocks item actions with an error. The client does not yet stop them itself: there are no item
  actions yet.
