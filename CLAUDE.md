# Tales of Pirates Godot port

The project aims to port D3D9 C++ game Tales of Pirates to Godot.

## Reference repo

The original game reference code is at https://github.com/mothannakhzaleh/TalesOfPirateDX9: C++ sources in `sources/`,
server binaries and Lua scripts in `server/`, database schema in `mssql/`. All reference paths in the docs are relative
to the root of that repo. If `CLAUDE.local.md` names a local copy, read the code there. If not, read it from GitHub.
Do not change the reference code.

## Read before you act

| When                                                           | Read                             |
| -------------------------------------------------------------- | -------------------------------- |
| Writing, naming or reviewing any C#                            | `docs/agents/standards.md`       |
| Adding or changing a legacy packet, its decoder or its encoder | `docs/agents/legacy-protocol.md` |
| Checking client behaviour in the engine, the editor or live    | `docs/agents/testing.md`         |

## Communication

Talk in ASD-STE100 Simplified Technical English.
Use approved words with one meaning only. Do not use idioms.
