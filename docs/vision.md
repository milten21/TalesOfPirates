# Product Vision

## The product

This project is an open-source remake of Tales of Pirates (Pirate King Online) on the Godot engine. The original
game becomes playable again on a modern engine. The project adds modern content authoring and the modding ecosystem
that the original game did not have.

The game is nostalgic in feel and modern in look. The world, the art identity, the music and the game rules stay
recognizable. The rendering, the UI layout and the usability become modern.

There are three ways to play:

- **Singleplayer.** This mode is planned, but it does not have priority. The balance for solo play is a known gap.
- **Multiplayer.** A friend hosts a local server. Other players can join while the friend plays. This mode is also
  the environment to test the game and to develop mods.
- **Online.** Players join dedicated community servers.

The game ships with the converted vanilla content. The client and the server are both open source.

Desktop platforms come first: Windows, Linux and macOS. Mobile and web are possible targets for later, but they are
not goals.

## Pillars

1. Each content kind has modern authoring. The project can release this support in phases, but it supports all
   content kinds.
2. One client works with all servers. A player can join any server without manual installation. The client gets the
   content of the server automatically.
3. The server has a full C# mod tier. Only sandboxed code goes to clients, and only through the client script tier.
   The client script tier is a future deliverable. Its design comes later.
4. The final product uses only the modern protocol. The original protocol is only a temporary bridge for the
   transition.
5. Singleplayer, multiplayer and online play use one content model. The load order is always on the computer that
   runs the server.
6. The converted vanilla content ships with the game. A player never has to import content.
7. The client and the server are open source. If the mod tiers cannot do something, a fork can do it.

## The components

The product has four components and two shared contracts.

### Client (Godot)

The client renders the game, and the player plays the game in it.

The client builds all content that travels at runtime: models, textures, effects, maps and tables. It builds this
content from the documented formats of the project. The client never receives content that depends on a Godot
version.

These items are compiled into the client binary:

- The palette of materials and effects. This palette is the set of shaders that mods set with data.
- The first-party UI, its widgets and its theme.

These items update together with the client. Thus they cannot become different from the client.

The client applies the load order of each server itself. It keeps one content pool for all servers. The hash of
each file is its key in this pool. When a player joins a server for the first time, the client shows the mod list
and the download size of the server.

The client also contains:

- A legacy-server mode for the transition period.
- A local presentation layer. This layer contains only presentation content, for example retextures, icons, themes
  and sounds. It loads after the content of each server.
- The map editor, if the project puts it in the client. This is an open item.

### Server (.NET)

The server is one headless program. It serves dedicated play and friend-hosted play. For singleplayer, the server
runs on the same computer as the client.

The server is a plain .NET program and does not use an engine. Walkability and movement use the grid of the
original game, because vanilla parity needs this grid.

The game logic is in C#. The vanilla scripts run without changes on an embedded Lua runtime. The server implements
the script API of this runtime again in C#. New server functions come only through the C# mod tier.

The server composes the load order and announces it when a player joins. It sends missing content to the client
through the game connection.

### Conversion tool

The conversion tool converts the original formats into the modern formats. The conversion goes in one direction
only. The original files are only an import source, and they do not change after the conversion.

The conversion tool makes the vanilla pack that ships with the game. The conversion is deterministic: the same
input always gives the same output. The tool records the original numeric ID on each converted entry as provenance.

During the transition period, the assets server works together with the conversion tool. The assets server converts
the client files of an original server into one large mod. Then it sends only the files that are different from
vanilla.

### Mod-kit

The mod-kit is the set of tools for the community:

- Blender with the export preset of the project. A full Blender add-on comes later.
- Any image editor and any text editor.
- JSON schemas with full validation.
- A headless packer. The packer validates a pack and writes its hashes and derived metadata.
- A live preview for effects. The preview loads an effect again when its file changes.

The Godot editor is a permitted mod-kit tool. Modding never needs a build of the client.

### Shared contracts

**The format contract.** One integer gives the version of all the formats of the project. The project increases
this integer only for a breaking change, and this occurs rarely.

Each pack declares the range of format versions that it supports. If the client is outside this range, the client
shows a warning and loads the pack.

The compatibility promise is different in the two directions:

- New clients load old packs, also after a major engine upgrade. This promise has no end date.
- Old clients load new packs with reduced function. To fix this, the player updates the client.

The format contract does not depend on the Godot version or on the release schedule of the client.

**The delivery conversation.** This is one exchange, and it is the same for all connection types. First, the two
sides compare manifests. Then, the server sends the missing files. The project defines this exchange one time, and
all modes and periods use it.

## Content strategy

### Runtime data only, with no exceptions

Godot resource files never travel as server content or mod content. These files are:

- PCK files.
- Scenes and resources: `.tscn`, `.tres`, `.scn` and `.res`.
- Imported resources.

There are two reasons for this rule:

- These files are locked to one engine version.
- These files can contain scripts. Godot runs these scripts when it loads the file.

Content loaders never send pack data to the resource loader of Godot.

All content that travels is data, and the client builds it at runtime. The client already does this for models,
rigs, animation and textured materials. It uses the runtime glTF loader of Godot for this.

Vanilla ships as the same runtime packs that mods use. Thus vanilla and mods use one loading pipeline, and each
play session tests this pipeline.

The results of this rule are:

- Modding never needs a build of the client. Blender, an image editor, a text editor and the mod-kit are the full
  toolchain.
- Mods do not ship shader code. They set the first-party palette of materials and effects with data. Godot compiles
  shader code at runtime. Thus mod shader code is a possible future option, and it is an open item. Until then, new
  shading technology comes only in client releases. The client is open source, thus contributors can add new
  shading technology.
- Packs supply animation clips. Each clip targets a rig by the names of its joints. The logic of the animation
  states is first-party client code.
- The format contract controls content compatibility. The Godot version never controls it.

### Identity

Namespaced string IDs are the only identity that authors write. This rule applies to client-only tables and to
gameplay tables.

The identity is on the entry. The file layout is only packaging. One file can contain many entries or one entry,
and a pack can mix the two layouts.

The server assigns numeric IDs at runtime and sends them to each connection. The original numeric IDs stay only as
provenance that the conversion tool records.

The canonical format for authors is JSON with published schemas. Two entries with the same ID in one pack cause a
validation error.

Client-only tables describe types of scene objects in game terms: model reference, material parameters, flags and
declared behaviors. They never use Godot node names or class names. The client makes the objects from these tables
at runtime.

Packs never contain scenes. The first-party scenes in the client binary are not content. Client-only tables never
go to the server.

### Mods and packs

A mod is one identity with two halves. Each half is optional:

- **The pack.** It contains only data. It is the only part of a mod that goes to clients.
- **The server C#.** It runs on the computer that runs the server.

A retexture is a mod with no code half. A balance script is a mod with no pack. In the two cases, the mod has one
version, one dependency node and one namespace.

A pack is a folder tree in the runtime formats of the project. A manifest is at the root of the tree. A person
writes the manifest.

The packer does not change the pack. It validates the tree. Then it writes the hash of each file and the derived
metadata into a separate generated file. The packer never edits the manifest. A zip of the tree is only a
convenience to move the pack. It is not a second format.

The mod ID is a token in lowercase letters. It is also the namespace of each entry that the mod adds. Vanilla is
pack zero, and it has a usual mod ID.

The server composes the load order in this sequence:

1. Vanilla.
2. The mods, in the sequence that the server owner sets.
3. The changes of the server owner, as a usual pack.

One rule resolves each conflict, for each entry and for each file: the later pack wins.

A local presentation layer on the client can load after the load order of the server. The client ignores gameplay
content in this layer.

A pack can change content in only five ways. The client applies them in one pass, in load order:

- **Add.** The pack adds new entries in its own namespace.
- **Attach.** The pack adds contribution entries. Each contribution entry refers to a target by ID. Examples are
  spawn placements, drop lines, shop stock and animations for a rig. The client calculates the final set from all
  contributions in the load order. If mods can extend a relation, authors write it on the "many" side, and it
  points to the "one" side.
- **Override.** The pack contains a full entry with the ID of a different mod. This entry replaces the full
  original entry.
- **Patch.** The pack changes some fields of a structured entry. For binary kinds, the pack replaces the full file.
  A full entry later in the load order cancels the patch.
- **Remove.** The pack declares a removal. The removal deletes the entry at this position in the load order. A full
  entry later in the load order adds the entry again.

Packs have no rule language. There are no conditions, no selectors and no list operations.

To make two mods work together, an author makes a compatibility pack. This is a small pack that depends on the two
mods. The server owner adds it when the server runs the two mods.

To configure a mod that contains only content, the server owner patches it from their own pack. Large changes to
game systems are in the server C# tier.

The packer finds the dependencies from the references in a pack. The manifest has a small field for constraints
that the packer cannot find.

Mod versions use semver, and a person sets them. Versions are only for display and to find updates. Hashes identify
the content.

### Delivery

Content travels through the game connection. An HTTP mirror for files is a possible later addition. The mirror
never has authority. The game server always owns the hash list.

The transfer works per file. The hash identifies each file. The transfer sends the files in a batch:

1. The server announces its load order with the generated hash lists.
2. The client replies with the hashes that it does not have.
3. The server sends these files one after the other.

Thus a join needs one round trip and one stream for any number of files.

The client keeps one content pool. The hash of each file is the only key in the pool. For each known server, the
client also keeps a small record of the load order. If two servers use the same file, the pool stores it one time.

Vanilla is in the pool from the start. Thus the join comparison also updates the content, and there is no separate
content updater. Old and new vanilla files stay in the pool together. Thus one client can join servers with
different vanilla versions.

The client applies the load order itself. The five ways to change content are deterministic. The same load order
always gives the same result on all computers.

Hashes identify only the files that authors wrote. Thus a patched entry travels as the base entry and the patch.
The pool stores each of them one time for all servers.

The transfer cannot resume a file. The client verifies each file before it puts the file in the pool. If the
connection stops, the client loses only the file that was in transfer. When the player joins again, the client
requests only the files that are still missing.

When a player joins a server for the first time, the client shows one screen. The screen shows:

- The mod list of the server.
- The total download size.
- Format range warnings, if there are any. The client shows a warning, but it does not refuse the pack.

When the player joins the server again and there is no important change, the client does not show this screen.

**Trust.** A hash makes sure that a file is not damaged. It does not make sure that the file is safe. The data-only
format is the sandbox:

- A server can send only data.
- Loaders treat each pack as input that is not trusted.
- Code that is not sandboxed never goes to the client.

There is no signing and no central authority at this level. A future signature can be a field in the manifest. This
addition does not need a new design.

### Authoring per content kind

**Models and textures.** Blender is the primary tool. The project and the community use one pipeline. Each valid
glTF export is content. All the `TOP_*` extras are optional, and they have vanilla default values. Authors work
with PNG files. Compressed textures come later, and the tools make them from the PNG files.

Rigs stay consistent through the names of their joints. New animations ship as additions that target the rig of a
race by name. Documentation and an export preset come first. The full Blender add-on comes later.

**Maps.** Each map has one chunked grid that does not depend on an engine. The grid holds the terrain, the
walkability and the regions. Each map also has a list of placed objects. The server reads only the terrain part of
the grid.

There is one map editor, and the community can use it. Version 1 of the editor has these functions:

- A fly camera.
- Place, move and delete objects.
- Save the map.

Terrain sculpting and terrain painting come in later versions. The location of the editor is an open item: in the
client or as a Godot editor plugin. The editor makes the minimap when it saves the map.

**Effects.** The converted effect files become the versioned effect data format of the project. The corpus has
1,235 effect files, 1,035 particle files and composite files. A ported runtime plays these effects. It reproduces
the motion integrators of the original exactly.

New effects are data. They set the first-party palette with parameters. Authors edit the data files by hand and use
the live preview. Converted effects and new effects travel as pack data. Thus mods can change both.

**Tables.** Each entry is JSON and uses the identity rules above. The conversion tool converts the original
rendering tables into client-only tables and asset metadata. It converts the gameplay tables into pack data. The
toolchain is a text editor and full validation. The project does not make a GUI to edit tables.

**UI.** The project builds the UI again in this repository with Godot `Control` nodes and themes. Four things make
up the UI:

- **First-party windows and theme.** They ship in the client binary.
- **Rethemes.** They bring the art of the original game through the content pipeline. A retheme is in a data
  format, and the client builds a theme from it at runtime.
- **Server-declared windows.** A C# mod class on the server declares a typed schema of properties and actions. The
  server sends this schema when a player joins. The client shows these windows with the same window code as other
  windows. These windows can only add UI. The client can remove them, and this has no effect on other UI.
- **Client scripts.** These come with the future client script tier.

Nameplates and HP bars are 3D labels and sprites in the world. The core UI does not use UI frameworks from third
parties.

**Server scripts.** The vanilla scripts are 221 Lua scripts with approximately 167,000 lines. They contain
missions, NPC dialogs and events. These scripts run without changes on an embedded Lua runtime in the C# server.
The server implements the script API again in C#. The API has approximately 545 functions. The original server
binaries are the reference for the correct behavior.

The Lua API is a permanent compatibility tier, and it does not change. Only the C# tier grows. Each set of maps has
one Lua state. This keeps the isolation that the original scripts expect.

Scripts are pack content, and only the server uses them. The vanilla scripts ship in pack zero. Thus a mod
overrides a vanilla script with the usual load order rules.

Script functions accept IDs in two forms. A number resolves through provenance. A string is a modern ID.

**Client scripts.** The future client script tier is sandboxed code that travels as pack content. Godot extensions
for sandboxed scripts exist. godot-sandbox and luau-gdextension are candidates, not decisions.

## Transition strategy

The original protocol ships as a small legacy compatibility mode. This mode has two jobs:

- It is the development backend of the client.
- It is a bridge for early players.

**Development.** The team builds the client one feature at a time against the original server binaries. The
original client connects to the same local server. The team uses it as a live reference for behavior. The first
playable version needs only these steps against the local reference server: login, character select, spawn on a
converted map and walk.

**Supported servers.** The original source code defines the supported protocol. A small set of documented
configuration toggles adds to it. The client supports toggles only. It never has special cases for one server. The
client does not support servers outside this set.

**Servers with custom content.** These servers run the assets server. The assets server converts their client files
into one large mod. Legacy mode uses two connections:

- Game traffic uses the old protocol.
- Content uses the modern delivery conversation.

The shipped vanilla content is already in the pool. Thus the client downloads only the custom content of the
server.

**End of legacy mode.** Legacy mode stops changing at vanilla parity. Vanilla parity is when the full original game
is playable from start to end on the modern client and server. A later major release removes legacy mode after a
deprecation period. Two protocols for an unlimited time would make each future feature slower to build.

## Distribution

GitHub Releases is the first official location for builds. Stores can be mirrors later, at most. The project does
not use Steam.

Two kinds of release use the same repository:

- **The game train.** The client and the dedicated server have one version tag. They use the same protocol and
  release together. Singleplayer and friend-hosted play need only the client download.
- **Tool tags.** The conversion tool, the assets server and, later, the Blender add-on each have their own tags.
  Their versions follow the format contract, not a client release. Thus the project can release a tool fix alone.

The client reads the release feed. When a newer game-train version exists, the client shows an update notice and
the changelog in the game. The project has no automatic updater. If the format range of a server is newer than the
client, the join screen shows a link to the download page.

The project does not host mods, and it has no mod discovery. A pack is a complete folder tree, and authors put it
where they want. At first, the community finds mods through its own conventions. Players join servers by address.

A first-party mod hub and a server list are possible later work, but the project does not promise them. The project
can run an official server in the future.

## Open items

These items are deferred. Each item shows its owner.

- The load order mechanism, the vanilla namespace token, the layout and keys of script packs, and the dependency
  rules. The rules are provisional. Owner: modding implementation planning.
- The time to build the map editor, and the location of the map and effect editors: in the client or as Godot
  editor plugins. Owner: map-system and effect-system implementation planning.
- PCK as a pack format. The project must first solve the risk of embedded scripts. Owner: modding implementation
  planning.
- Shader code in mods. Owner: modding implementation planning.
- The list of legacy-mode toggles. The team sets the list during implementation, with tests against real servers.
- The local server for singleplayer: in the client process or in a child process. The current preference is a child
  process. The Godot .NET host makes the in-process option possible. Owner: architecture.
- Solo balance for singleplayer. Owner: game design. This item is not scheduled.
- The design and the sandbox technology of the client script tier. Owner: a separate future effort.
