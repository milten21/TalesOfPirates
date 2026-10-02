# Legacy protocol packets

`docs/protocol/` is the record of every packet the client reads or writes. The packet and its record change in the
same diff: a packet added to `src/Top.Legacy.Protocol`, or a decoder or encoder added or changed in
`src/Top.Client.LegacyBridge`, updates its section in `docs/protocol/`. `docs/protocol/README.md` holds the file path
conventions and says which file holds which packet group.

## A packet section

Each section has these parts, as the existing sections show:

1. **Type** - direction, opcode, our reader or writer type and file.
2. **Payload** - field order and types, and every variant and conditional branch.
3. **Server** - every situation in which the original server sends the packet, or how it reacts to it: the function
   with file and line, and the field values that situation carries (sync type, state, error code).
4. **Client** - for each situation: the decoder, the event (or `null`), the `Top.Client` subscribers and what they
   do. For a client-to-server packet: the UI action, command and encoder that send it.
5. **Gaps** - each situation, field or variant the server can send that we skip, drop or mishandle.

Find the situations by searching the server for the writer of the opcode, not by reading the handler you port.
A packet has many senders, and a section with one sender is not complete.

## Done

- Every send site of the opcode in `sources/Server` is in the Server part, or the section says why it is left
  out.
- Every field the reader reads but the bridge drops is in Gaps.
- The row in `docs/protocol/opcodes.md` says "implemented" and links the section.
- A finding that changes what the user sees, or a server defect, is in the "Most important findings" table of
  `docs/protocol/README.md`.

## Capturing packets from the server

Pin reader tests on real server bytes. The tool is a net10.0 console project in the scratchpad with one
`ProjectReference` to `src/Top.Legacy.Protocol`. It opens a `GateConnection` to the test server, implements
`IGateListener`, sends `LoginRequest` and `BeginPlayRequest`, and prints `Convert.ToHexString` of every packet it
wants. Read the bytes with `PacketReader.ReadByte` up to `Remaining`. Pass the server address and the credentials in
environment variables, never in a file.

- Send end play (opcode 434, empty body) before closing. The server then sends one disappear (505) for each
  character in view, before the end-play reply.
- A player appear packet needs a second logged-in player near the first: log the second account in after the
  first has entered. The first client gets its 504, and its 505 when the second ends play.
- The same client, waiting for a marker file, can act as the other player during a live client check.
- Split a packet into named hex constants from its tail: the fixed-size blocks after the base-info block give its
  length.
