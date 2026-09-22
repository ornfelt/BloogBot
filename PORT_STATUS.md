# BloogBot_net9 port status

Maintained by the `bloogbot-net9-port` skill. A hint for the next run, not the source of truth -
the two trees are. Re-derive from this directory with the commands in the skill's "Orient"
section.

**Last run:** group 3 - memory and native interop (all ten files; `MemoryManager.InjectAssembly`
stubbed behind the Fasm.NET blocker), plus `Game/MemoryAddresses.cs` and `Game/Cache/ItemCacheInfo.cs`
**Next:** (1) remove the unported .NET Framework sources still in this working tree (see "Blocked"
below) and confirm the four builds are green; (2) decide the Fasm.NET replacement so
`MemoryManager.cs` can be finished; (3) group 4 - the game layer (`Game/Objects/`, `Game/Frames/`,
`IGameFunctionHandler` and the three handlers, `Functions`, `ObjectManager`, ...)

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

Until the originals are gone, each run checks its slice with a scratch project outside the repo
that compiles only the header-carrying `BloogBot/**/*.cs` files (see the run log).

### Fasm.NET - decision needed

`MemoryManager.cs` is ported, but `Fasm.NET.dll` (`Binarysharp.Assemblers.Fasm`) is a net461
mixed-mode C++/CLI assembly and cannot load on .NET 9. The `using` line, the `fasm` field and the
two `InjectAssembly` bodies are commented out behind `TODO: port body here` markers; the stubs throw
`NotImplementedException`. Facts that bear on the choice:

- `InjectAssembly` is called from two places only: `SignalEventManager` (both hooks are commented
  out in the static constructor) and `WardenDisabler.Initialize` (under `USE_CUSTOM_CHANGES` it is
  behind `bool useWarden = false`). In the fork's default configuration FASM is never invoked at
  runtime; only the `static readonly FasmNet fasm = new FasmNet()` field initializer would have run.
- The whole tree assembles a very small x86 subset: `PUSH`/`POP` reg, `PUSHFD`/`POPFD`,
  `PUSHAD`/`POPAD`, `CLD`, `INC`, `MOV` reg/reg, reg/imm, reg/[reg+disp], [imm]/reg, `ADD` reg/reg
  and reg/imm, `CALL imm`, `JMP imm`.
- Options, as the skill lists them: (a) rebuild Binarysharp's Fasm.NET C++/CLI wrapper as a
  `CLRSupport=NetCore` x86 vcxproj in this solution, (b) P/Invoke `FASM.DLL` behind a `FasmNet`
  shim in namespace `Binarysharp.Assemblers.Fasm` with `Clear`, `AddLine`, `Assemble()`,
  `Assemble(IntPtr)` and `FasmAssemblerException` - with that namespace and those names
  `MemoryManager.cs` goes back to byte-identical, (c) a managed encoder for the subset above.

## Header convention

Every ported file starts with `// Ported from BloogBot/<path relative to the source repository
root>`, e.g. `// Ported from BloogBot/BloogBot/Game/XYZ.cs` and
`// Ported from BloogBot/Bootstrapper/Program.cs`. The leading `BloogBot/` is the source
repository directory; stripping it gives exactly what `find` prints from the source root, which is
what the skill's Orient pipeline compares against.

## Porting order

- [x] 1. Setup - solution, Directory.Build.props, csproj files, assets
- [x] 2. Leaf types - Position, XYZ, XYZXYZ, the 20 Game/Enums, Hotspot, Npc, TravelPath, GatherRoute, CommandModel, Wait, Logger, BotSettings
- [ ] 3. Memory and native interop - MemoryManager, Detour, Hack, HackManager, ThreadSynchronizer, SignalEventManager, WardenDisabler, Navigation, ClientHelper, Probe
  - all ten ported; `MemoryManager.cs` still carries `TODO: port body here` at the three Fasm.NET sites (blocked, see above)
- [ ] 4. Game layer - Objects, Frames, ObjectManager, Functions, the three function handlers
  - `Game/MemoryAddresses.cs` and `Game/Cache/ItemCacheInfo.cs` already ported (group 3 references them)
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
| `BloogBot/MemoryManager.cs` | 295 | the `OpenProcess` handle in `WriteBytes` is never closed, so every call leaks a process handle; the Warden page-scan hook calls it per scanned byte | `BloogBot/MemoryManager.cs:289` |

## Deviations from the original

Every place the port had to differ, and why. One line each.

| Port file | What differs | Why |
| --- | --- | --- |
| `Directory.Build.props` | every SDK property is conditioned on `.csproj`; the shared `..\Bot\` / `..\Bot\Release\` output path lives here instead of in each csproj | the four native `.vcxproj` files import `Directory.Build.props` too; the output path is identical for all 24 managed projects |
| `Bootstrapper/Bootstrapper.csproj` | `Newtonsoft.Json` 13.0.4 (BloogBot uses 13.0.3) | mirrors the two original `packages.config` files, which already differed |
| `BloogBot.UI.Avalonia`, `BloogBotTests` | no package references yet | packages are added by the run that ports the first file needing them (groups 10 and 14) |
| `BloogBot/MemoryManager.cs` | `using Binarysharp.Assemblers.Fasm`, the `fasm` field and both `InjectAssembly` bodies are commented out; the stubs throw `NotImplementedException` | Fasm.NET blocker - temporary, see "Blocked" |
| `BloogBot/MemoryManager.cs` (runtime note, no code change) | the nine `[HandleProcessCorruptedStateExceptions]` methods compile with warning SYSLIB0032; on .NET 9 an `AccessViolationException` outside the low 64 KiB is fatal and the `catch (AccessViolationException)` blocks never run | .NET 9 does not support recovering from corrupted-state exceptions; ported as-is because the attribute and the catches are the original's behavior on .NET Framework |

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
| group 3 (+ MemoryAddresses, ItemCacheInfo) | 12 files | ~2260 | red - unported originals still in tree; scratch compile of the 43 ported `BloogBot` files: only error is `Navigation.cs` `using BloogBot.AI;` (namespace arrives with group 6) | not run | scratch compile, no custom: same single error | not run |
