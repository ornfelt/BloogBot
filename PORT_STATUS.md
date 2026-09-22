# BloogBot_net9 port status

Maintained by the `bloogbot-net9-port` skill. A hint for the next run, not the source of truth -
the two trees are. Re-derive from this directory with the commands in the skill's "Orient"
section.

**Last run:** setup - solution scaffolding, plus group 2 (leaf types) and group 12 (Bootstrapper)
**Next:** remove the unported .NET Framework sources still in this working tree (see "Blocked"
below), confirm the four builds are green, then group 3 - memory and native interop
(MemoryManager, Detour, Hack, HackManager, ThreadSynchronizer, ...; read "Known blockers" first,
Fasm.NET lands here)

## Blocked - read this first

This directory is a **clone of the source repository** (same history, branch `net9-port`), not
the empty `git init` repository the skill describes. Setup therefore had to *remove* the
.NET Framework sources that are not ported yet, because SDK-style projects compile every `.cs`
under their directory. That removal was refused by the session's permission policy, so the tree
currently holds both the ported files (header line present) and the untouched originals (no header
line). Until the originals are removed, `dotnet build BloogBot_net9.sln` is red; the four
`BloogBot.UI.*` projects, which have no originals under them, build green.

What has to go, all of it tracked in git and restorable with `git checkout -- <path>`:

- every `.cs` / `.xaml` file **without** a `// Ported from BloogBot/` header line
- the old non-SDK leftovers: `*/packages.config`, `*/App.config`, `*/app.config`,
  `*/Properties/` (AssemblyInfo, Resources, Settings), `BloogBot/Fasm.NET.dll`
- source-repository tooling: `BloogBot.sln`, `UPSTREAM_SYNC.md`, `bloogbot_diff.ps1`, `.claude/`

The `Loader/`, `FastCall/`, `Navigation/` and `NavigationTests/` trees stay: they are the verbatim
copies group 13 starts from. `BloogBotTests/Assets/` stays for group 14.

## Header convention

Every ported file starts with `// Ported from BloogBot/<path relative to the source repository
root>`, e.g. `// Ported from BloogBot/BloogBot/Game/XYZ.cs` and
`// Ported from BloogBot/Bootstrapper/Program.cs`. The leading `BloogBot/` is the source
repository directory; stripping it gives exactly what `find` prints from the source root, which is
what the skill's Orient pipeline compares against.

## Porting order

- [x] 1. Setup - solution, Directory.Build.props, csproj files, assets
- [x] 2. Leaf types - Position, XYZ, XYZXYZ, the 20 Game/Enums, Hotspot, Npc, TravelPath, GatherRoute, CommandModel, Wait, Logger, BotSettings
- [ ] 3. Memory and native interop - MemoryManager, Detour, Hack, ThreadSynchronizer, Navigation
- [ ] 4. Game layer - Objects, Frames, ObjectManager, Functions, the three function handlers
- [ ] 5. Data layer - IRepository, Repository, Sql/Sqlite/TSql repositories
- [ ] 6. AI layer - Bot, DependencyContainer, the 28 SharedStates
- [ ] 7. Bot loading and services - BotLoader, DiscordClientWrapper, Loader (hostfxr entry)
- [ ] 8. UI abstractions and viewmodels - BloogBot.UI.Abstractions, BloogBot.UI.Core
- [ ] 9. WPF shell - BloogBot.UI.Wpf (default, the reference shell)
- [ ] 10. Avalonia shell - BloogBot.UI.Avalonia, at parity with 9
- [ ] 11. The 17 bot plugins
- [x] 12. Bootstrapper - ported out of order in the setup run: it depends on nothing in BloogBot, and an `Exe` project with no `Main` would have kept the solution red
- [ ] 13. Native - Loader/dllmain.cpp on hostfxr, FastCall/Navigation build config
- [ ] 14. Tests - BloogBotTests on MSTest 3.x

## Potential bugs found

| File (port) | Line | What looks wrong | Original |
| --- | --- | --- | --- |
| `Bootstrapper/Program.cs` | 47 | `VirtualAllocEx` reserves `loaderPath.Length` bytes but `Encoding.Unicode.GetBytes(loaderPath)` writes twice that; works only because the allocation rounds up to a zeroed page | `Bootstrapper/Program.cs:46` |
| `Bootstrapper/Program.cs` | 62 | no `DllImport` in `WinImports.cs` sets `SetLastError = true`, so the four `Marshal.GetLastWin32Error()` checks never reflect those calls | `Bootstrapper/Program.cs:56`, `Bootstrapper/WinImports.cs` |

## Deviations from the original

Every place the port had to differ, and why. One line each.

| Port file | What differs | Why |
| --- | --- | --- |
| `Directory.Build.props` | every SDK property is conditioned on `.csproj`; the shared `..\Bot\` / `..\Bot\Release\` output path lives here instead of in each csproj | the four native `.vcxproj` files import `Directory.Build.props` too; the output path is identical for all 24 managed projects |
| `Bootstrapper/Bootstrapper.csproj` | `Newtonsoft.Json` 13.0.4 (BloogBot uses 13.0.3) | mirrors the two original `packages.config` files, which already differed |
| `BloogBot.UI.Avalonia`, `BloogBotTests` | no package references yet | packages are added by the run that ports the first file needing them (groups 10 and 14) |

## NuGet packages

| Package | Version | Project | For |
| --- | --- | --- | --- |
| `Newtonsoft.Json` | 13.0.3 | `BloogBot` | `Game/Position.cs`, `BotSettings.cs` |
| `Newtonsoft.Json` | 13.0.4 | `Bootstrapper` | `Program.cs` |

Still to come, with the file that needs them (checked with grep over the source tree):
`Discord.Net` 3.x (`DiscordClientWrapper.cs`), `System.Data.SQLite.Core` (`SqliteRepository.cs`),
`System.Data.SqlClient` (`SqlRepository.cs`, `TSqlRepository.cs`), **`StreamJsonRpc`
(`UI/BotService.cs` - the skill lists it as unreferenced, but `BotService.cs` has
`using StreamJsonRpc;`, so it stays and goes into `BloogBot.UI.Core`)**, MSTest
(`BloogBotTests/NavigationTests.cs`), Moq and Castle.Core (tests).

Dropped as in-box on .NET 9 or unreferenced by any source file: `System.Memory`, `System.Buffers`,
`System.Numerics.Vectors`, `System.Runtime.CompilerServices.Unsafe`, `System.Threading.Tasks.Extensions`,
`Microsoft.Bcl.*`, `System.Text.Json`, `System.Collections.Immutable`, `System.IO.Pipelines`,
`System.Threading.Tasks.Dataflow`, `System.Diagnostics.DiagnosticSource`, `System.Text.Encodings.Web`,
`System.Security.*`, `System.Reflection.Emit*`, `MessagePack*`, `Nerdbank.*`, `PolyType`,
`Microsoft.VisualStudio.Threading` / `.Validation`, `Microsoft.NET.StringTools`, `System.Linq.Async`,
`System.Interactive.Async`, `Microsoft.Win32.Registry` (no `using Microsoft.Win32` in any source file).

## UI parity

| Tab | WPF | Avalonia |
| --- | --- | --- |
| Overview | [ ] | [ ] |
| Settings | [ ] | [ ] |
| Travel Paths | [ ] | [ ] |
| NPCs | [ ] | [ ] |
| Hotspots | [ ] | [ ] |
| Powerlevel | [ ] | [ ] |
| Gathering | [ ] | [ ] |

## Run log

| Run | Files | ~Lines | Wpf | Avalonia | Wpf, no custom | Avalonia, no custom |
| --- | --- | --- | --- | --- | --- | --- |
| setup + group 2 + group 12 | scaffolding (24 csproj, sln, props), 31 leaf types, 3 Bootstrapper files | ~1300 | red - unported originals still in tree (UI projects green) | not run | not run | not run |
