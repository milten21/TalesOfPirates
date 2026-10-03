# Top.Client.Cli

`Top.Client.Cli` is a scripted second player. It sends the same typed commands as the game client. It has two
modes, which run the same steps:

- File mode runs a scenario file and exits.
- Live mode takes one step at a time over HTTP on `localhost`. See [Live mode](#live-mode).

```sh
dotnet run --project src/Top.Client.Cli -- --server <host>:<port> --account <name> --password <text> --scenario <file>
dotnet run --project src/Top.Client.Cli -- --server <host>:<port> --account <name> --password <text> --live [--port <port>]
```

`--say-interval <seconds>` is optional: the server's minimum time between two chat lines, 1 s when not given. Keep
scenario files next to their ticket under `.scratch/`. GM commands are in `gm-commands.md`.

A scenario has one step on each line. Empty lines and lines that start with `#` are skipped. Positions are in meters
and times are in seconds. A `[timeout]` must be more than 0. A step with no `[timeout]` waits 30 seconds at most.

| Step                               | Action                                                                        |
|------------------------------------|-------------------------------------------------------------------------------|
| `play <character>`                 | log in, select the character by name and enter the world                      |
| `move <x> <z> [timeout]`           | walk to the point and wait until the move ends. A block or a refusal fails it |
| `wait-actor <name> [timeout]`      | wait until the actor is in sight. Ends at once when it is already in sight    |
| `wait-event <EventName> [timeout]` | wait for a server event by its type name, such as `PartyInvitedEvent`         |
| `sleep <seconds>`                  | wait a fixed time. `sleep 0` is permitted                                     |
| `accept`, `decline`                | answer the open party invite                                                  |
| `invite <name>`                    | invite the character with that name to the party                              |
| `leave`                            | leave the party. Fails when the character is in no party                      |
| `kick <name>`                      | kick the party member with that name. Fails when another member leads         |
| `say <text...>`                    | say the rest of the line in local chat, also a GM command                     |
| `use <bag slot> [timeout]`         | use or equip the item in the bag slot. Bag slots start at 0                   |
| `quit`                             | close the connection and end the run                                          |

Rules:

- Events
  - `wait-event` takes the first matching event that arrived after the event of the previous `wait-event`, or after
    the start of `play`. An event that arrived during an earlier step counts. An event from before the previous wait
    does not count. The client keeps the newest 10,000 events. The result shows the event with its values, for
    example `PartyInvitedEvent { InviterId = 7, InviterName = "T1" } arrived`.
  - `move` and `use` wait for the reply to what they send. An event of the same type from before the step does not
    count. So after a step that causes the same event, wait for that event first: for example, put
    `wait-event BagChangedEvent` between a `&make` and a `use`.
  - `accept` and `decline` use up their invite and all older invites.
- Steps that do not wait
  - `invite` and `say` send and do not wait. The server tells about a refusal only with a system message, and the
    client does not decode system messages. To know the result, wait for an event: `PartyChangedEvent` when the other
    player accepts.
  - The server drops a chat line that comes less than its `say_interval` after the last line, GM commands too. So
    `say` waits until `--say-interval` and 0.1 s more have passed since the last `say`.
- Server facts
  - On 2026-10-03 the test server ignored an `invite` sent at once after `play`, with no reply. Put `sleep 3` between
    them.
  - The server sends no reply to a consumable used less than 200 ms after any other consumable on the server. `use`
    then fails at its time limit.
- Party: `leave` and `kick` use the member list of the last `PartyChangedEvent`. `kick` sends the member's world id
  from that list.
- File mode run: the client checks all lines before it connects. Each step prints `line <n>: <step>: done, <result>` or
  `line <n>: <step>: failed, <reason>`. The first failed step stops the run. A lost connection fails a step that
  waits, and `sleep`. Exit code 0: all steps are done. 1: a step failed or did not parse. 2: a launch argument is
  missing or bad.

Example:

```text
# The move points are examples. Take real points near the position of T1 at the time of the run.
# The sight range is about 20 m.
play Observer
move 2158 2779.8
wait-actor T1 180
move 2169.75 2774.75
sleep 120
quit
```

## Live mode

`--live` listens on `http://localhost:7973/`. `--port <port>` changes the port. The listener needs no admin rights.
Start the client as a background process. It writes each step and its result to its output.

| Request        | Body          | Response                                                                    |
|----------------|---------------|-----------------------------------------------------------------------------|
| `POST /step`   | one step line | `done, <result>` with status 200, or `failed, <reason>` with status 422     |
| `GET /events`  | none          | the events that arrived since the last read, one on each line. Empty: none  |
| other requests | any           | status 404 with the two requests above                                      |

- One request runs one step, with the same rules as file mode. Lines that start with `#` are not comments here.
- A bad or unknown step, and a failed step, return `failed, <reason>` and the client keeps running. Live mode has no
  first-failure stop.
- A wait step keeps its request open until its event arrives or its time limit ends. Do not give `curl` a shorter
  `--max-time` than the time limit.
- Steps run one at a time. A step request that comes during a running step waits until that step ends.
- `GET /events` does not wait for a running step. It is independent of `wait-event`: an event that a wait step took
  is also in the events. The client keeps the newest 10,000 unread events.
- `quit` returns its result, then the process exits with code 0. A step request that waited behind `quit` returns
  `failed, the run has ended`.
- A port that is in use exits with code 3.
- `curl -d` sends the step as it is, also a GM command with `&` or `,`. Do not use `curl -f`: it hides the reason
  of a failed step.
