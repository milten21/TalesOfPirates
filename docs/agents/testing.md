# Testing tools

Do not write a temporary script, scene or console tool to check a result. Use the tools below.

## Engine tests

`tests/Top.Client.EngineTests` runs GdUnit4Net tests inside Godot, headless, with no server. `dotnet test` from the
repo root runs them with all other tests. `dotnet test tests/Top.Client.EngineTests` runs only them. The user
environment variable `GODOT_BIN` must be the path of the Godot executable, not its folder.

A new test goes under `test/`, in the folder that follows the client code, with `[TestSuite]` and
`[RequireGodotRuntime]`. `TestContent` gives the converted content, and `FakeServerConnection` takes the place of the
server.

## godot-ai

godot-ai connects the agent to the developer's own Godot editor. Through its MCP tools the agent runs the client
from the editor, reads the live scene tree and UI controls, sends clicks and keys, takes screenshots and reads the
logs. The addon runs only in a visible editor. A headless editor skips it. The addon is in git, and `.mcp.json` pins
the server to the same version.

- Read logs with `logs_read` source `game` or `editor`. Source `all` returns mostly plugin readiness lines.
- godot-ai cannot enable an editor plugin, and `project_manage` refuses the `editor_plugins/*` keys. Ask the user to
  enable the plugin in Project Settings.

## Launch profile

In a debug build, a launch profile logs in and enters the world as a named character, with no clicks. A profile is
`%APPDATA%\Godot\app_userdata\Top.Client\launch_profiles\<name>.json`, outside the repo:

```json
{ "Host": "127.0.0.1", "Port": 1973, "Account": "tester", "Password": "password", "CharacterName": "Hero" }
```

- Shell: `<godot> --path src/Top.Client -- --launch-profile <name>`.
- Editor: `project_run`, then `game_eval` with `get_node("/root/App").StartProfileLaunch("<name>")` at the main menu.
  Do not await in that eval: the scene change removes the eval node and stops the game at a debugger break.

A failure logs `launch profile failed: <reason>` and shows the main menu.

## Top.Client.Cli

`Top.Client.Cli` is a scripted second player on the live server. It runs a scenario file: log in, walk, wait for
actors and events, party actions, chat and GM commands, item use. Use it for flows that need a second player and
for GM setup of a character.

```sh
dotnet run --project src/Top.Client.Cli -- --server <host>:<port> --account <name> --password <text> --scenario <file>
```

Live mode takes one step at a time over HTTP on `localhost`. Use it to try a flow step by step, when the next step
depends on the result of the last one. Start it as a background process, then send each step with `curl`. Each
request returns the result of its step. `quit` ends the process.

```sh
dotnet run --project src/Top.Client.Cli -- --server <host>:<port> --account <name> --password <text> --live
curl -s -d 'play Observer' localhost:7973/step
curl -s -d 'say &make 481,1' localhost:7973/step
curl -s -d 'wait-event PartyInvitedEvent 60' localhost:7973/step
curl -s localhost:7973/events
curl -s -d 'quit' localhost:7973/step
```

Steps, rules, live mode responses and an example are in `client-cli.md`. GM commands are in `gm-commands.md`.

## Live server

The server address and the test accounts are in `CLAUDE.local.md`. `T1` is on one test account and `Observer` on the
other. A character stays where it logged out, so read the position of `T1` from the `entered ...` line of the game
log before a scenario walks to it.

How to capture packets from the server is in `docs/agents/legacy-protocol.md`.
