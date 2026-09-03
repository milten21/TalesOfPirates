# Tales of Pirates Unity port

The project aims to port D3D9 C++ game Tales of Pirates to Unity.

## Unity editor

**Unity CLI MCP** drives the running editor. It does not start one, so check firstwith `unity status --json`.
It exposes one tool per editor operation, forwarded from the `com.unity.pipeline` package.
Mutating tools take `dry_run`; destructive ones need `confirm`. Builds, tests, bakes and package
changes return at once and report progress through a matching `*_status` tool.

## Lessons learned

- Each Unity package under `src/` has two project files. The `src/` one targets `netstandard2.1` with the NuGet
  reference set and no defines. Unity generates a second one from the `.asmdef`, with `v4.7.1`, Unity's reference
  set and about 150 `UNITY_*` defines. An LSP loads one of the two, so its diagnostics can miss what Unity
  reports. Check the editor console.
- A `save_path` on a capture command always resolves under `Assets/`, even when absolute. The result reports
  success and a rewritten `savedPath`, and the PNG becomes an asset. Use `include_inline_image` instead.
- Unity pins C# 9, and the Unity packages set `LangVersion 9` to match. The other `src/` projects are `net10.0`
  with no `LangVersion`, so they get the newest C#. Code moved from one of those into a Unity package can stop
  compiling.
