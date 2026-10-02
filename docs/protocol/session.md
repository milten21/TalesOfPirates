# Session and account packets

See [README.md](README.md) for the file path conventions.

## Summary

- Our readers and writers match the server layout for all packets in this file. The problems are in the situations
  the server can create, not in the normal layouts.
- The server cannot change a security code. `TP_UPDATE_PASSWORD2` has a stray `;` after its `if`
  (`GroupServerAppServ.cpp:2058-2060`). The block after the `if` always runs, and the function always returns 534.
- When the client is in the wrong state, the gate answers Create (346) and Change (347) with a DeleteCharacterReply
  (936) that has code 2. Our client drops that event outside the Delete state, so the form waits and does not stop.
- The client does not send Logout (432). The gate does the same clean-up when the socket closes.
- On some database errors, the group server writes a character list with a wrong layout. Our reader throws, the
  packet is dropped, and there is no reply timeout.
- Encryption is always on. `LoginScreen.cs:110` connects with `isEncrypted: true`. If the gate runs without RSA-AES,
  login fails after 30 s as "did not answer in time".

## Error codes

From `NetRetCode.h`:

| Group | Codes |
|---|---|
| Gate | 1 NETEXCP, 2 NOTSELCHA, 3 NOTPLAY, 4 NOTARRIVE, 5 TOOMANYPLY, 7 VER_ERROR |
| Group | 521 INERR, 522 NETEXCP, 524 INVALIDCHA, 529 KICKUSER, 530 ISGLDLEADER, 532 SERVERBUSY, 534 INVALID_PW2, 538 MULTICHA, 550 BADBOY |
| Account | 1001 INVALIDUSER, 1002 INVALIDPWD, 1006 BANUSER, 1007 PBANUSER, 1100 UNKNOWN, 1104 ONLINE, 1105 SAVING, 1107 DISCONN, 1108 UNKNOWNCMD, 1109 TLSWRONG |

`src/Top.Client.LegacyBridge/ServerError.cs` gives the text. Codes 6, 529, 1007 and 1100 can occur in these packets
and have no text.

A `DisconnectedEvent` that is not `ClientClosed` goes to `ConnectionWatch.ReportLostConnection`
(`src/Top.Client/Core/ConnectionWatch.cs:8-18`). That calls `SceneRouter.LeaveServer(text)`, which shows the main
menu with a "Connection lost" text.

---

## ServerPublicKey (943, server to client)

- **Type:** `ServerPublicKey.Read` (`src/Top.Legacy.Protocol/Crypto/ServerPublicKey.cs:40-50`). Only
  `GateConnection.ExchangeKeys` (`GateConnection.cs:167-207`) reads it. It has no bridge decoder.
- **Payload:** `u16` key size (read, not used), then `u16` length and bytes: an RSA public key in DER form, 3072 bit.
- **Server:** the gate builds it in `ToClient::OnConnected` (`ToClient.cpp:212-249`), once per connection, and only
  when `g_rsaaes` is on.
  - If the key is not valid, the gate disconnects with reason -27 (`:221-226`).
  - If any other exception occurs, the gate disconnects (`:243-247`).
  - The gate sends no error packet.
- **Client:** the first packet must be 943. If it is not, we throw `InvalidDataException`, and `Close(HandshakeFailed)`
  gives `DisconnectedEvent(HandshakeFailed)`. `LoginScreen.ShowDisconnect` (`src/Top.Client/Login/LoginScreen.cs:180-190`)
  shows a popup with the `Disconnection.ToText` text.
- **Gaps:**
  - If the gate has `g_rsaaes` off, no 943 arrives. The reader waits `ReadTimeout` (30 s) and closes with `TimedOut`.
    The Login packet is never sent.
  - If the server fails to make a key, it closes the socket. We report `ServerClosed`, not `HandshakeFailed`.

## PrivateKey (355, client to server)

- **Type:** no message type. `GateConnection.ExchangeKeys` writes it inline (`GateConnection.cs:184-190`).
- **Payload:** a `string` with Base64 of RSA-OAEP-SHA1(our AES key). The packet counter is 0.
- **Server:** `ToClient::OnProcessData`, case `CMD_CM_SEND_PRIVATE_KEY` (`ToClient.cpp:405-443`). It decodes the
  Base64, decrypts the key, keeps `AES::MIN_KEYLENGTH` bytes and sets `handshakeDone`. If decoding fails, it calls
  `Disconnect(datasock)`. It sends no reply.
- **Client:** we send it before the send thread starts. Then we set `_cipher` and call `ReportOpen`, which gives
  `ConnectedEvent`.
- **Gap:** if the server rejects the key, we see only a later `GateClosed`, which becomes `ServerClosed`.

## Ping (515, server to client) / PingReply (15, client to server)

- **Type:** no type. `GateConnection.EchoPing` (`GateConnection.cs:312-328`) handles it below the bridge.
- **Payload:** we read up to 5 `u32`: tick, gate address, source id, gate player id, gate player address. The server
  also writes a `u16` of 1, which we ignore. The reply echoes the 5 values.
- **Server:** the game server builds it in `GameAppNet.cpp:1596-1613`, case `CMD_MM_QUERY_CHAPING`. The trigger is the
  GM command `ping_game <name>` (`CharacterSuperCmd.cpp:1272-1279`). The target character gets 515. The game server
  handles the reply in `CharacterPrl.cpp:229-264`: it computes the ping and sends `CMD_MC_QUERY_CHAPING` to the GM.
- **Client:** we echo automatically. No event is made.
- **Gaps:** none.

## CheckPing (537, server to client) / CheckPingReply (17, client to server)

- **Type:** inline in `GateConnection.ReadPackets` (`GateConnection.cs:225-227`).
- **Payload:** `m_ulPingDataLen` random bytes. The default length is 0 (`Character.cpp:139`). The GM command
  `setpinginfo` changes it (`CharacterSuperCmd.cpp:1012-1024`). We ignore the bytes. The reply is empty.
- **Server:** `CCharacter::CheckPing` (`Character.cpp:5459-5467`).
  - It sends every `defPING_INTERVAL` = 20 s (`Character.h:26`) to a character that a player controls
    (`CharacterRun.cpp:168-171`).
  - The timer resets on map entry (`CharacterCmd.cpp:295`).
  - Reply: `CharacterPrl.cpp:266-290` sets `m_dwPing` to the elapsed ticks and sends PreMoveTime (536). See
    [actions.md](actions.md#premovetime-536).
- **Client:** we reply automatically.
- **Gaps:** none.

## GroupPing (5022, server to client) / GroupPingReply (6022, client to server)

- **Type:** inline in `GateConnection.ReadPackets` (`GateConnection.cs:229-231`).
- **Payload:** empty in both directions.
- **Server:**
  - The group builds it in `Player::DoCommand`, `pingx <name>` (`GroupServer/src/Player.cpp:344-358`).
  - The gate records `m_pingtime` and forwards it (`GateServer/src/ToGroupServer.cpp:366-370`).
  - The gate handles the reply in `ToClient.cpp:469-484`. It sends it to the group only if the player has both
    `gp_addr` and `gm_addr`. The group handles it in `CP_PING` (`GroupServerAppServ.cpp:661-670`).
  - No caller of `Player::DoCommand` was found in `GroupServer/src`. In this build the packet is probably never sent.
- **Client:** we reply automatically.
- **Gaps:** none.

---

## Login (431) / LoginReply (931)

**Types**

- Writer: `LoginRequest` (`Messages/Account/LoginRequest.cs`) through `LoginEncoder`.
- Reader: `LoginReply` (`Messages/Account/LoginReply.cs:39-88`) through `LoginDecoder`.

**Request payload:** `string` account, `string` hex digest of the password, `string` MAC ("Unknown"), `u16` 911,
`u16` client version (default 32125, `GateSettings.cs:13`). The gate reads the version and the 911 mark from the end
(`ToClient.cpp:771-794`), so the order matters. Our order matches.

**Reply variants**

| Variant | Layout |
|---|---|
| Success | `u16` 0, `u8` MaxChaNum, `u8` row count, rows, `u8` has-second-password. A row is `u8` flag; if the flag is 1: `string` name, `string` job, `u16` level, `u16` type id, 34 × `u16` equipment ids. |
| Error from the gate | `u16` code only (codes 1 and 7) |
| Error relayed from the group or account server | `u16` code, then `u16` code again. The gate does `l_wpk = l_rpk` and then `WriteShort(l_errno)` (`ToClient.cpp:834-839`). `ReadRefusalText` (`LoginReply.cs:80-88`) finds no string and returns empty. This is safe. |

**Server path**

1. The gate `ToClient::CM_LOGIN` (`ToClient.cpp:751-885`) adds the IP and address and calls the group with
   `TP_USER_LOGIN`.
2. The group `TP_USER_LOGIN` (`GroupServerAppServ.cpp:989-1145`) calls the account server with `PA_USER_LOGIN`.
3. `AuthThread::AccountLogin` (`AccountServer2.cpp:885-1073`) checks the account.
4. On success, the group adds the character list (`GetCHAsFromDBByPlayer`, `:900-987`). The gate removes the 13
   trailing bytes and adds the has-second-password byte at the end (`ToClient.cpp:847-861`).

HasSecondPassword = the account has a stored second password and the account row is not new
(`GroupServerAppServ.cpp:1116-1123`).

**Error codes in 931**

| Code | Built by | Cause |
|---|---|---|
| 1 | gate | packet older than 30 s (`:868-878`); group did not answer (`:821-832`); DDoS rate limit (`:362-371`, can come at any time in the session) |
| 7 | gate | client version is not the gate `m_version` (`:771-782`) |
| 5 | group | the group has `MaxLoginUsr` players (`GroupServerAppServ.cpp:1050-1056`) |
| 521 | group | account name is null or longer than 128 (`:1032-1039`) |
| 522 | group | 25 s timeout, or the account server did not answer (`:995-1000`, `:1062-1070`) |
| 1001 | account | no such account, or name or MAC not valid (`AccountServer2.cpp:773-795`, `:904-908`) |
| 1002 | account | wrong password digest (`:933-941`) |
| 1006 | account | ban = 2 or 3 (`:910-928`) |
| 1007 | account | ban = 1 (`:916-921`) |
| 1100 | account | database update failed, or unknown login status (`:966-971`, `:1013-1019`, `:1045-1059`, `:556`) |
| 1104 | account | account is online on the same group (`:998-1003`). The group also sends `AP_KICKUSER` (`GroupServerAppServ.cpp:1074-1080`, `:1197-1245`), which disconnects the older session. |
| 1105 | account | account is in "saving" state, inside its protection time (`AccountServer2.cpp:1021-1029`) |
| 1107 | account | group socket lost (`:892-898`) |
| 1108 | account | unknown command (`:534-538`) |
| 1109 | account | thread-local storage error (`:516-520`) |

- After every error, the gate disconnects the client (`Disconnect(..., -31)`).
- When the account is online on a different group, the account server writes no code after `WriteCmd(0)`
  (`AccountServer2.cpp:1004-1009`). What the group then reads is not confirmed.
- A second 431 on a socket that is already logged in gets `CMD_MC_MAPCRASH` (503), not 931 (`ToClient.cpp:754-761`).

**Client**

- **Send:** `LoginScreen.LogIn` (`LoginScreen.cs:101-112`) runs on the Log In button, on Enter in either field, or on
  `Block.AcceptRequested`. It calls `Connect(...)` and then sends `LogInCommand`.
- **Success:** `LoginSucceededEvent` goes to `LoginScreen.CompleteLogin` (`:144-158`), which emits `LoggedIn`.
  `MainMenuScene.CompleteLogin` (`src/Top.Client/MainMenu/MainMenuScene.cs:191-204`) remembers the login, fills
  `Session` and calls `GoToCharacterSelect(characters, slotCap, hasSecurityCode)`.
- **Error:** `LoginFailedEvent(code, text)` goes to `LoginScreen.ShowFailure` (`:160-178`). It clears the password,
  closes the connection and opens the popup "Log in failed" with the text and the code. With no text it shows "The
  server refused the log in."
- Both handlers act only while `_isAttempting` is true.

**Gaps**

- Codes 1007 and 1100 have no text. The popup shows the fallback text and the code.
- The DDoS form of 931 (code 1) can arrive outside the login screen. `LoginFailedEvent` is then ignored. The user
  sees only "The server closed the connection." after the gate disconnects.
- **Wrong list, case 1:** if `FetchRowByActName` returns more than 1 row (`GroupServerAppServ.cpp:980-985`), the group
  writes no row count. The reply is `[0][max][pw]`. `CharacterSummary.ReadList` reads the has-password byte as the
  count, and the next `ReadByte` throws. The packet is dropped. The login screen stays on "Logging in as…" with no
  timeout until the user presses Cancel.
- **Wrong list, case 2:** `SendCharData` writes flag 1 and then one `u8` 0 when it cannot parse the look string
  (`:875-884`). Our reader misreads this row, with the same result.
- Packet 503 (MAPCRASH) has no decoder.

## Logout (432, client to server)

- **Type:** `Opcode.Logout` exists. There is no command, encoder or message type. **We never send it.**
- **Payload:** empty. The original client sends it as a 3 s sync call (`ProCirculateCS.cpp:465-470`).
- **Server:**
  - As an RPC, `ToClient::OnServeCall` (`ToClient.cpp:278-296`) runs `CM_LOGOUT` and disconnects. As an async packet,
    `TransmitCall::Process` (`:699-702`) does the same. No packet comes back.
  - `CM_LOGOUT` (`:889-966`) sends `TP_DISC` to the group, `TM_GOOUTMAP` to the game if the player plays, and a sync
    `TP_USER_LOGOUT` to the group. The group runs `EndPlay`/`EndRun`, `PA_USER_BILLEND` and `PA_USER_LOGOUT`
    (`GroupServerAppServ.cpp:1303-1359`).
  - No server builds `CMD_MC_LOGOUT` (932).
- **Client:**
  - The Log Out button on character select calls `CharacterSelectScreen.Leave` (`CharacterSelectScreen.cs:153`,
    `:559-569`). Then `CharacterSelectScene.GoBack` (`CharacterSelectScene.cs:73`, `:334-336`) calls
    `SceneRouter.LeaveServer()` (`src/Top.Client/Core/SceneRouter.cs:66-73`), which closes the socket.
  - The world menu actions Log Out and Exit (`WorldScene.cs:404-416`) also call `LeaveServer()`.
  - The gate `ToClient::OnDisconnect` (`ToClient.cpp:251-259`) calls `CM_LOGOUT` on socket close. The server clean-up
    is the same.
- **Gaps:** none in effect. The TCP close comes before the server clean-up.

## DeleteCharacter (436) / DeleteCharacterReply (936)

- **Types:** `DeleteCharacterRequest` through `DeleteCharacterEncoder`; `DeleteCharacterReply` through
  `DeleteCharacterDecoder` (`Account/DeleteCharacterDecoder.cs`).
- **Payload:**
  - Request: `u8` character index, `string` security code in plain text. This is the same as the original
    (`ProCirculateCS.cpp:501-508`).
  - Reply: `u16` code.
- **Server:** the gate `CM_DELCHA` (`ToClient.cpp:1234-1288`) passes the request to the group `TP_DELCHA`
  (`GroupServerAppServ.cpp:1887-2016`). The gate forwards the group code unchanged.

| Code | Built by | Cause |
|---|---|---|
| 0 | group | row updated and backed up. The group also sends `PC_REFRESH_SELECT` to other sessions of the same account (`:1991`, `:1545`). |
| 1 | gate | packet older than 30 s, or the group did not answer. The gate then disconnects (`ToClient.cpp:1261-1267`, `:1280-1287`). |
| 2 | gate | client is not in character selection, or has no group address (`:1246-1251`) |
| 521 | group | a character is in play (`:1890-1894`) |
| 524 | group | index out of range (`:1900-1904`) |
| 534 | group | wrong second password (`:1908-1914`) |
| 538 | group | the character is online in another session (`:1916-1922`) |
| 530 | group | the character leads a guild (`:1924-1928`) |
| 532 | group | database transaction failed (`:2002-2004`) |
| 529 | group | player pointer does not match (`GroupServerAppServ.cpp:131-136`). The gate then disconnects (`ToClient.cpp:1273-1276`). |

**The gate also sends 936 with code 2** in answer to CreateSecondPassword (`ToClient.cpp:1302-1307`) and
ChangeSecondPassword (`:1369-1374`) when the client is not in selection.

- **Client:**
  - **Send:** the Delete button emits `DeleteRequested`. The scene calls `OpenDelete` (`CharacterSelectScreen.cs:268-281`).
    `DeleteForm.DeleteRequested` calls `DeleteCharacter` (`:455-460`), which sends `DeleteCharacterCommand`.
  - **Code 0:** `CharacterDeletedEvent` goes to `CompleteDelete` (`:462-476`). It shows the select state and the toast
    "<name> deleted", and emits `CharacterDeleted`. `CharacterSelectScene.RemoveCharacter`
    (`CharacterSelectScene.cs:201-213`) removes the row and redraws the stands.
  - **Other codes:** `DeleteCharacterFailedEvent` goes to `ReportDeleteFailure` (`:478-497`). 534 gives "Wrong security
    code" on the code field. 530, 532 and 538 show the text only. Other codes show "text · Error N".
  - Both handlers return early unless `_state == State.Delete`.
- **Gaps:**
  - A 936 with code 2 that answers Create or Change arrives in state `FirstCode` or `ChangeCode`. It is dropped, and
    the form waits and does not stop.
  - Code 529 has no text.
  - `PC_REFRESH_SELECT` (5418), which other sessions of the account get, has no decoder.

## CreateSecondPassword (346) / reply (941)

- **Types:** `CreateSecondPasswordRequest` through `CreateSecurityCodeEncoder`; `CreateSecondPasswordReply` through
  `CreateSecurityCodeDecoder`.
- **Payload:** request `string` code in plain text; reply `u16` code.
- **Server:** the gate `CM_CREATE_PASSWORD2` (`ToClient.cpp:1290-1355`) passes it to the group `TP_CREATE_PASSWORD2`
  (`GroupServerAppServ.cpp:2018-2048`).

| Code | Cause |
|---|---|
| 0 | the code is saved |
| 534 | a password already exists, the code is longer than 32 (`ROLE_MAXSIZE_PASSWORD2`, `RoleCommon.h:50`), or `IsValidName` fails |
| 532 | the database update failed |
| 1 | gate timeout or no answer from the group. The gate then disconnects. |
| 529 | player mismatch. The gate then disconnects. |
| (936, code 2) | wrong state. See DeleteCharacter. |

- **Client:**
  - `Setup` opens the `FirstCode` state when `!hasSecurityCode` (`CharacterSelectScreen.cs:176-179`).
  - **Send:** `FirstCodeForm.CreateRequested` calls `CreateSecurityCode` (`:335-340`), which sends
    `CreateSecurityCodeCommand`.
  - **Code 0:** `SecurityCodeCreatedEvent` goes to `CompleteSecurityCode` (`:342-354`): select state and the toast
    "Security code set".
  - **Other codes:** `CreateSecurityCodeFailedEvent` goes to `ReportSecurityCodeFailure` (`:356-367`), which shows the
    form error.
  - Both handlers act only in `State.FirstCode`.
- **Gaps:**
  - The wrong-state answer arrives as 936 and is dropped.
  - For 534 the text is "Cha second password not created · Error 534". This text is wrong for a create failure.
  - Code 529 has no text.

## ChangeSecondPassword (347) / reply (942)

- **Types:** `ChangeSecondPasswordRequest` through `ChangeSecurityCodeEncoder`; `ChangeSecondPasswordReply` through
  `ChangeSecurityCodeDecoder`.
- **Payload:** request `string` old code, `string` new code; reply `u16` code.
- **Server:** the gate `CM_UPDATE_PASSWORD2` (`ToClient.cpp:1357-1410`) passes it to the group `TP_UPDATE_PASSWORD2`
  (`GroupServerAppServ.cpp:2051-2078`).
  - **Server bug:** line 2060 ends the `if (...)` with `;`. The block after it always runs, so the group **always
    returns 534**. Codes 0 and 532 cannot occur.
  - The gate can also send 1 or 529, and then disconnects.
  - Wrong state: 936 with code 2.
- **Client:**
  - **Send:** `SecurityCodeButton` calls `OpenChangeCode`. `ChangeCodeForm.ChangeRequested` calls
    `ChangeSecurityCode` (`:369-396`), which sends `ChangeSecurityCodeCommand`.
  - **Code 0:** `SecurityCodeChangedEvent` goes to `CompleteSecurityCodeChange` (`:398-410`): toast "Security code
    changed".
  - **Other codes:** `ChangeSecurityCodeFailedEvent` goes to `ReportSecurityCodeChangeFailure` (`:412-431`). 534 shows
    "Current code is wrong". Other codes show the form error.
  - Both handlers act only in `State.ChangeCode`.
- **Gaps:**
  - With this server, every change shows "Current code is wrong", also with a correct old code.
  - The wrong-state answer arrives as 936 and is dropped.
  - Code 529 has no text.

## BeginPlay (433) / BeginPlayReply (933)

- **Types:** `BeginPlayRequest` through `BeginPlayEncoder`; `BeginPlayReply` through `BeginPlayDecoder`, which always
  gives `BeginPlayFailedEvent` (`Play/BeginPlayFailedEventMapper.cs`).
- **Payload:** request `u8` character index; reply `u16` code, always above 0. Success does not use 933: the game
  server sends EnterMap (516) through `ToGameServer::MC_ENTERMAP` (`ToGameServer.cpp:489-550`). See
  [view.md](view.md#entermap-516).
- **Server:** the gate `CM_BGNPLAY` (`ToClient.cpp:968-1059`) calls the group `TP_BGNPLAY`
  (`GroupServerAppServ.cpp:1361-1470`). On group success, the gate calls `GameServer::EnterMap`.

| Code | Built by | Cause |
|---|---|---|
| 1 | gate | packet older than 30 s (`:972-979`), or no answer from the group (`:1005-1012`) |
| 2 | gate | client not in selection, or no group address (`:990-997`) |
| 4 | gate | no game server has the map (`:1032-1040`) |
| 5 | gate | the game server has more than 15000 players (`:1041-1048`) |
| 521 | group | a character is already current (`:1364-1368`), or the character row was not found (`:1404-1408`) |
| 524 | group | index out of range (`:1374-1378`) |
| 534 | group | no second password (`:1382-1386`) |
| 550 | group | a stat is above `MaxVal`. The group also sends a SYSINFO to all players in play (`:1414-1441`). |
| 529 | group | player mismatch. The gate then disconnects (`ToClient.cpp:1018-1021`). |

- If the game server refuses EnterMap, the gate **disconnects** the client (-33). The forward is commented out
  (`ToGameServer.cpp:511-527`), so no 933 or 516 arrives.
- If the character is online in another session, the group first asks that gate to kick it
  (`GroupServerAppServ.cpp:1389-1401`).
- **Client:**
  - **Send:** `EnterRequested` goes to `CharacterSelectScreen.BeginPlay` (`CharacterSelectScreen.cs:254-266`). It shows
    "Entering the world as …" and sends `BeginPlayCommand`.
  - **Failure:** `BeginPlayFailedEvent` goes to `ReportBeginPlayFailure` (`:513-522`). It returns to the Select state
    and opens the popup "Enter failed" with the text and the code. This handler has no state guard.
  - **Success:** `MapEnteredEvent` goes to `CompleteBeginPlay` (`:499-511`). See [view.md](view.md#entermap-516).
- **Gaps:**
  - Code 529 has no text. After the 529 disconnect, `CloseMessagePopup` (`:524-527`) hides the popup. The user sees
    only "The server closed the connection."
  - The SYSINFO that comes with 550 has no decoder.
  - If the gate has no `ClientConnection` pointer, it sends nothing (`ToClient.cpp:983-987`). The client has no
    timeout for this case.

## EndPlay (434) / EndPlayReply (934)

- **Types:** `EndPlayRequest` (empty) through `EndPlayEncoder`; `EndPlayReply` (`Messages/Play/EndPlayReply.cs:29-46`)
  through `EndPlayDecoder`.
- **Payload:**
  - Request: empty.
  - Success reply: `u16` 0, `u8` MaxChaNum, then the character rows as in LoginReply, with **no** has-password byte.
  - Error reply: `u16` code only.
- **Server:** the gate `CM_ENDPLAY` (`ToClient.cpp:1061-1127`) sends `TM_GOOUTMAP` to the game and sets the state to
  character selection. Then it calls the group `TP_ENDPLAY` (`GroupServerAppServ.cpp:1472-1503`). Before the reply,
  the game server sends one CharacterEndSee (505) for each character in view.

| Code | Built by | Cause |
|---|---|---|
| 0 | group | success. The group adds the list (`:1499-1500`). |
| 3 | gate | client not in play, or no game address. The gate then **disconnects** (`:1071-1077`). |
| 1 | gate | no answer from the group. The gate then **disconnects** (`:1105-1111`). |
| 521 | group | `m_currcha < 0`: the character never reached `MP_ONLINE` (`:1475-1477`) |
| 529 | group | player mismatch. The gate then disconnects (`:1119-1122`). |

- If the game socket is missing (`l_game` null), the gate sends nothing (`:1080-1081`).
- `ToGameServer::MT_PALYEREXIT` (`ToGameServer.cpp:592-621`) is a second sender. It needs
  `m_exit == CharacterSelection`, and the gate never sets that value. This path is dead in this build.
- **Client:**
  - **Send:** `WorldScene.EndPlay` (`src/Top.Client/World/WorldScene.cs:434-441`) blocks input, clears the map and
    sends `EndPlayCommand`. Callers: `GameMenu.SwitchCharacterConfirmed` through `SwitchCharacter` (`:418-423`), and a
    map load failure through `LeaveWorld` (`:180`, `:425-432`).
  - **Success:** `PlayEndedEvent` goes to `ReturnToCharacterSelect` (`:443-447`). It calls
    `GoToCharacterSelect(characters, slotCap, HasSecurityCode: true, entry failure text)`. Character select shows the
    "Enter failed" popup if a failure text exists (`CharacterSelectScreen.cs:181-184`).
  - **Error:** `EndPlayFailedEvent` goes to `ReportEndPlayFailure` (`:449-456`), which calls `LeaveServer(text or code)`.
- **Gaps:**
  - Code 529 shows as the bare number "529".
  - The wrong-list cases of LoginReply also apply here. Our reader throws, and the world stays behind the loading
    screen with no timeout.
  - When `l_game` is null, no reply comes, with the same result.
  - The original client ignores the code and always reads a list (`PacketCmd_SC.cpp:318-335`). Our handling of the
    error form is stricter, and it is correct for this server.
