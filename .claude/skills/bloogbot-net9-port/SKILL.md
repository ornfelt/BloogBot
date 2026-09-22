---
name: bloogbot-net9-port
description: Port the .NET Framework 4.8 BloogBot solution (Code2/C#/BloogBot) to .NET 9 in a new sibling tree Code2/C#/BloogBot_net9. The port is a faithful replica - same files, same types, same members, same behavior - not a redesign. The WPF UI is split into a UI-agnostic abstraction plus two interchangeable shells, WPF (default) and Avalonia, that carry the same controls and both default to dark mode with a light toggle. The USE_CUSTOM_CHANGES preprocessor directive is carried over unchanged, anything that looks like a real bug is tagged POTENTIAL BUG FOUND rather than fixed, and the native Loader.dll is rewritten from the .NET Framework CLR hosting API to nethost/hostfxr. Run it over and over - each run does one slice, leaves the solution building, updates PORT_STATUS.md and writes commit_message.txt.
disable-model-invocation: true
argument-hint: "[empty means the next unported slice, or: setup, status, port <project-or-file>, ui [wpf|avalonia], native, review <path>, verify]"
---

# Port BloogBot from .NET Framework 4.8 to .NET 9

The goal is a **.NET 9 replica of BloogBot**: the same files, the same namespaces, the same types,
the same members, the same behavior, the same on-disk settings (`botSettings.json`,
`bootstrapperSettings.json`), the same SQLite / T-SQL schemas and the same injected-bot workflow, so
an existing BloogBot install can be pointed at the new build and behave identically.

Two things are deliberately **not** a straight copy, and only two:

1. **The UI.** The WPF window is split into a UI-agnostic abstraction plus two shells - WPF
   (default) and Avalonia - with the same controls, the same bindings, the same behavior, dark mode
   by default and a light toggle in both. See "The UI layer".
2. **The native loader.** `Loader/dllmain.cpp` hosts the CLR through `ICLRRuntimeHost` and
   `BindAsLegacyV2Runtime`, which are .NET Framework-only. It is rewritten against
   `nethost` / `hostfxr`. See "The native side".

Everything else is a replica. **Do not modernize, do not refactor, do not "improve".** If the
original code looks wrong, tag it - see "POTENTIAL BUG FOUND" - and port it as it is.

This is the **BloogBot .NET 9 port**. It has nothing to do with `bloogbot-upstream-sync` (which
lands upstream `DrewKestell/BloogBot` commits in the .NET Framework tree and keeps running on its
own), nothing to do with the `cpp-csharp-*` server ports, with `libwow`, with the `wc_clean_new`
ports, or with `ameisenbotx-upstream-sync`. Do not apply any `.claude/rules/wc-clean-new-*.md` file
here - none of them apply to this tree.

## Source and target

Source (read-only reference), the .NET Framework 4.8 solution, on branch `migrate-to-net9`:

`$code_root_dir/Code2/C#/BloogBot` (PowerShell: `$env:code_root_dir\Code2\C#\BloogBot`)

Target, created by setup mode, its own git repository, on branch `net9-port`:

`$code_root_dir/Code2/C#/BloogBot_net9` (PowerShell: `$env:code_root_dir\Code2\C#\BloogBot_net9`)

The source tree stays on .NET Framework and keeps receiving upstream syncs. **Never edit it** apart
from this skill file and its own `commit_message.txt`. That is what makes `bloogbot-upstream-sync`
keep working: it cherry-picks upstream commits into `BloogBot`, and a later run of this skill
carries the result across into `BloogBot_net9`.

### What is in the source tree

| Project | Kind | Files | What it is |
| --- | --- | --- | --- |
| `BloogBot` | `WinExe`, x86, `v4.8` | 117 `.cs` + 2 `.xaml` | the bot itself - memory, game layer, AI states, repositories, WPF UI |
| `Bootstrapper` | `Exe`, x86 | 4 `.cs` | starts `WoW.exe` and injects `Loader.dll` into it |
| 17 bot plugins | `Library`, x86 | 6-10 `.cs` each | one MEF plugin per spec (`ArcaneMageBot`, `FuryWarriorBot`, `TestBot`, ...) |
| `BloogBotTests` | `Library`, x86, MSTest | 2 `.cs` | the managed test project |
| `Loader` | `vcxproj` | `dllmain.cpp` | native DLL injected into `WoW.exe`; hosts the CLR and calls `BloogBot.Loader.Load` |
| `FastCall` | `vcxproj` | `dllmain.cpp` | native `__fastcall` / `__thiscall` trampolines |
| `Navigation` | `vcxproj` | Detour + `PathFinder` | native mmap pathfinding, P/Invoked from `BloogBot/Navigation.cs` |
| `NavigationTests` | `vcxproj` | - | native test project |

~25,000 lines of C# over 249 files, 20 C# projects and 4 C++ projects. Roughly a third of the C# is
the UI: `UI/MainWindow.xaml` is 1,544 lines and `UI/MainViewModel.cs` is 1,901.

The `BloogBot` project's internal layout, which the port mirrors exactly:

| Directory | Files | What |
| --- | --- | --- |
| `AI/` | 7 | `Bot`, `IBot`, `IBotState`, `IDependencyContainer`, `DependencyContainer`, `PlayerTracker`, `StuckHelper` |
| `AI/SharedStates/` | 28 | the state machine - grind, loot, travel, corpse, battleground and arena queues |
| `Game/` | ~15 | `ObjectManager`, `Functions`, the three `*GameFunctionHandler`s, `MemoryAddresses`, `Spell`, `Inventory`, `WowDb` |
| `Game/Objects/` | 9 | `WoWObject`, `WoWUnit`, `WoWPlayer`, `LocalPlayer`, `LocalPet`, `WoWItem`, ... |
| `Game/Enums/` | 20 | the client enums |
| `Game/Frames/` | 3 | `DialogFrame`, `LootFrame`, `MerchantFrame` |
| `UI/` | 5 `.cs` + 2 `.xaml` | `App`, `MainWindow`, `MainViewModel`, `CommandHandler`, `BotService` |
| root | ~25 | `MemoryManager`, `Detour`, `Hack`, `HackManager`, `ThreadSynchronizer`, `Logger`, `BotLoader`, `Loader`, the three repositories, `WardenDisabler` |

## Scope guard

Write only inside `$code_root_dir/Code2/C#/BloogBot_net9`, plus this skill file and
`commit_message.txt` in the source repo. Nothing else under `Code2/C#` is touched. **Never edit the
source tree's code** - not a `.cs`, not a `.csproj`, not a `.vcxproj`, not a `.xaml`, not a `.sql`.
It has to keep building as it is and it has to stay the reference for every later run. If the source
looks wrong, tag it in the *port* (see "POTENTIAL BUG FOUND") and say so in the summary instead of
fixing it here.

## Branch and repositories

The two repositories use **different branch names on purpose**. The source tree is on
`migrate-to-net9`; the port is on `net9-port`. Every run starts by making sure of both:

```bash
SRC="$code_root_dir/Code2/C#/BloogBot"
DST="$code_root_dir/Code2/C#/BloogBot_net9"

# source repo - the branch already exists in the normal case
cd "$SRC"
git rev-parse --verify migrate-to-net9 >/dev/null 2>&1 || git checkout -b migrate-to-net9
[ "$(git branch --show-current)" = "migrate-to-net9" ] || git checkout migrate-to-net9

# port repo
cd "$DST"
git rev-parse --verify net9-port >/dev/null 2>&1 || git checkout -b net9-port
[ "$(git branch --show-current)" = "net9-port" ] || git checkout net9-port
```

If either working tree is dirty with changes this skill did not make, say so and stop rather than
switching branches under the user.

The target is its own repository, created once by setup mode with `git init -b net9-port`, so the
port has a history independent of the upstream syncs happening in the source tree.

### Why the branch names differ

Both repositories push to the same GitHub remote, `https://github.com/ornfelt/BloogBot`:

| Repository | Local branch | Remote branch |
| --- | --- | --- |
| `BloogBot` (source) | `migrate-to-net9` | `origin/migrate-to-net9` |
| `BloogBot_net9` (port) | `net9-port` | `origin/net9-port` |

`origin/migrate-to-net9` belongs to the source tree. The port must never be pushed there: the two
repositories have unrelated histories, so such a push is rejected as a non-fast-forward and only
goes through with `--force`, which would repoint the branch away from the source-tree history.

GitHub shows `net9-port` as "122 commits behind". `ornfelt/BloogBot` is a **fork** of
`DrewKestell/BloogBot`, and for a fork GitHub compares each branch against the *parent* repository's
default branch, not against your own `main`. 122 is exactly the commit count of
`DrewKestell/BloogBot:main`, and `net9-port` is a root commit that shares no ancestor with it, so
every one of those 122 counts as absent. The badge says nothing about whether the port is current -
`main` is caught up with upstream and the port was taken from `main`. Ignore it. The real question
is which source commit the port was taken from, which "Orient" answers by diffing the two working
trees.

### What the port is based on

The port replicates the source tree's **working tree** as it stands when a run executes, which on
`migrate-to-net9` is `main` plus this skill file. Confirm that `migrate-to-net9` carries no C#
changes of its own before porting:

```bash
cd "$SRC"
git diff main..migrate-to-net9 --stat    # expect only .claude/ and .gitignore
```

If that diff ever touches a `.cs`, `.csproj`, `.vcxproj`, `.xaml` or `.sql` file, stop and say so:
the port would no longer be a replica of `main`.

**Never commit anything.** A run stages nothing, commits nothing and pushes nothing. It writes the
message it *would* use to `commit_message.txt` and stops there - see "Output expectations".

## Modes

Decide from the argument:

1. **Setup** - `setup`, or `BloogBot_net9/BloogBot_net9.sln` does not exist yet. Create the
   directory and the git repository, `Directory.Build.props`, the solution, the `.csproj` files, the
   `.gitignore`, `README.md` and `PORT_STATUS.md`, and copy the non-code assets across. If the
   argument also named a project or file, continue into port mode in the same run.
2. **Port** - `port <project-or-file>`. Port the named project or file and whatever it needs to
   compile.
3. **UI** - `ui`, optionally `ui wpf` or `ui avalonia`. Work only on the UI layer - abstractions,
   viewmodels, shells, theming, parity. See "The UI layer".
4. **Native** - `native`. Work only on the C++ side: the `hostfxr` rewrite of `Loader/dllmain.cpp`
   and the build configuration of the other three `vcxproj` files. See "The native side".
5. **Review** - `review <path>`. Compare the port against the original and report gaps. Report
   only; do not modify files unless the user asks.
6. **Verify** - `verify`. Run every build configuration from "Verification" and report, nothing
   else.
7. **Status** - `status`. Read `PORT_STATUS.md`, confirm it against the tree, print where the port
   stands and what the next slice is. Change nothing.
8. **Continue (no argument, or `continue`)** - work out what is missing and do the next slice. This
   is the default when the skill is invoked bare.

Check `BloogBot_net9/BloogBot_net9.sln` for existence before doing anything else. If it exists,
never regenerate it or the `.csproj` files - extend them only when a ported file genuinely needs a
new package reference.

### Continue mode

**This mode is meant to be run over and over until the port is finished.** Every run starts from a
cold context and has to re-derive its own position, so it begins by looking at the two trees rather
than by assuming anything. A run picks up exactly where the last one stopped, does one useful slice,
leaves the solution building, and says what the next slice is. Nothing about a run may depend on
having seen the previous one.

#### 1. Orient

Work out the current state from the trees themselves - that is the authoritative answer - before
deciding anything:

```bash
SRC="$code_root_dir/Code2/C#/BloogBot"
DST="$code_root_dir/Code2/C#/BloogBot_net9"

# does the solution exist at all?
ls "$DST/BloogBot_net9.sln"

# every source file that has to end up somewhere
(cd "$SRC" && find . \( -name '*.cs' -o -name '*.xaml' \) \
   -not -path './packages/*' -not -path '*/obj/*' -not -path '*/bin/*' \
   -not -path './Bot/*' -not -path './Debug/*' -not -path './Release/*' \
   | sed 's|^\./||' | sort) > /tmp/bloogbot-src.txt

# every source file the port already claims, via the header line each ported file carries
grep -rho '// Ported from BloogBot/[^ ]*' "$DST" \
     --include='*.cs' --include='*.xaml' --include='*.axaml' \
  | sed 's|// Ported from BloogBot/||' | sort -u > /tmp/bloogbot-done.txt

comm -23 /tmp/bloogbot-src.txt /tmp/bloogbot-done.txt      # still to port

# what is half-done - these markers are the resume pointer
grep -rln 'TODO: port body here' "$DST" --include='*.cs' --include='*.xaml' --include='*.axaml' | sort

# what was flagged on the way through
grep -rn 'POTENTIAL BUG FOUND' "$DST" | wc -l
```

`PORT_STATUS.md` is a hint that makes this cheaper, not a substitute for it: read it first, then
confirm against the trees. Where the two disagree, the trees win and the ledger gets fixed.

#### 2. Pick the next slice

1. If `BloogBot_net9` does not exist, or the `.sln` is missing, run setup mode first, then carry on
   into the steps below in the same run.
2. Otherwise walk the porting order below and pick the first group that has a missing file, or a
   file that exists but still carries `TODO: port body here`. Skip everything listed under "Not
   ported" - it is done by definition and never comes up again.
3. A file already ported and free of TODOs is finished. Do not revisit it, re-diff it against the
   original, or tidy it - that is review mode's job and it is not part of a continue run. The only
   reason to touch a finished file is that the current slice genuinely needs a change in it.
4. Never start the Avalonia shell (group 10) before the WPF shell (group 9) is complete - the WPF
   shell is the reference the Avalonia one is checked against.

#### 3. Do the work

1. Port the picked files completely, then keep going into the next ones in the order. **Aim for
   roughly 1500-3000 lines of ported C# per run** - that is the target, not a ceiling to creep up
   on. A whole project, or a whole directory, in one pass is exactly right.
2. `UI/MainWindow.xaml` (1,544 lines) and `UI/MainViewModel.cs` (1,901) are each bigger than one
   comfortable run. Port them tab by tab, in source order - `Overview`, `Settings`, `Travel Paths`,
   `NPCs`, `Hotspots`, `Powerlevel`, `Gathering` - and leave the untranslated remainder marked with
   `<!-- TODO: port body here - MainWindow.xaml -> <Tab> tab -->` /
   `// TODO: port body here - MainViewModel.cs -> <member>`. A run that ends mid-file must still
   leave the solution building.
3. Build before finishing (see "Verification"). A run always ends on a building solution.

#### 4. Record and report

Update `PORT_STATUS.md`, write `commit_message.txt`, then summarize per "Output expectations". End
the summary with a single explicit line so the next run - and the reader - knows the entry point:

```text
Next: port BloogBot/AI/SharedStates (GrindState onwards)
```

#### 5. When to stop, and when it is done

- **Normal stop** - the line target is met. Report and stop. Do not start another file to round the
  number up.
- **Blocked** - no .NET 9 SDK, no x86 runtime, the build is broken by code this run did not touch,
  or a dependency genuinely cannot run on .NET 9 (see "Known blockers"). Say what is blocking,
  leave the solution building if it already was, and stop. Do not reshape the projects to get around
  it and do not silently pick a different file to have something to show.
- **Nothing left** - every source file outside "Not ported" has a counterpart,
  `grep -rl 'TODO: port body here' "$DST"` comes back empty, and all four build configurations from
  "Verification" are green. Say the port is feature-complete, print the `POTENTIAL BUG FOUND`
  inventory, and suggest `review` plus a real injection test as the next steps. **Do not invent
  work**: no new tests, no refactors, no helper classes, no restructuring. An empty run that says
  "nothing left to port" is a correct run.

Reruns are cheap and idempotent by design: the scaffolding is only ever created once and finished
files are left alone. Running the skill bare twice in a row must never produce a diff on the same
lines twice.

### Porting order

Each group depends on the ones above it. Bottom-up: nothing is ported before the things it calls.

1. **Setup** - the directory, the git repository, `Directory.Build.props`, `BloogBot_net9.sln`, the
   `.csproj` files, `.gitignore`, `README.md`, `PORT_STATUS.md`, and the non-code assets
   (`botSettings.json`, `bootstrapperSettings.json`, `SqliteSchema.SQL`, `TSqlSchema.SQL`,
   `SqlSchema.SQL`, `Sql/`, `Docs/`, `LICENSE`). See "Setup mode".
2. **Leaf types** - `Game/Position.cs`, `Game/XYZ.cs`, `Game/XYZXYZ.cs`, all 20 files in
   `Game/Enums/`, `Hotspot.cs`, `Npc.cs`, `TravelPath.cs`, `GatherRoute.cs`, `CommandModel.cs`,
   `Wait.cs`, `Logger.cs`, `BotSettings.cs`. No dependencies, so they go first and give the rest
   something to compile against.
3. **Memory and native interop** - `MemoryManager.cs`, `Detour.cs`, `Hack.cs`, `HackManager.cs`,
   `ThreadSynchronizer.cs`, `SignalEventManager.cs`, `WardenDisabler.cs`, `Navigation.cs`,
   `ClientHelper.cs`, `Probe.cs`. This is where the `Fasm.NET` blocker lands - read "Known
   blockers" before starting it.
4. **Game layer** - `Game/Objects/` (9 files), `Game/Frames/` (3), `Game/Cache/ItemCacheInfo.cs`,
   `Game/MemoryAddresses.cs`, `Game/IGameFunctionHandler.cs` and the three handlers
   (`VanillaGameFunctionHandler`, `TBCGameFunctionHandler`, `WotLKGameFunctionHandler`),
   `Game/Functions.cs`, `Game/ObjectManager.cs`, `Game/Spell.cs`, `Game/SpellEffect.cs`,
   `Game/Inventory.cs`, `Game/Intersection.cs`, `Game/LuaTarget.cs`, `Game/WowDb.cs`,
   `Game/WoWEventHandler.cs`.
5. **Data layer** - `IRepository.cs`, `Repository.cs`, `SqlRepository.cs`, `SqliteRepository.cs`,
   `TSqlRepository.cs`.
6. **AI layer** - `AI/IBot.cs`, `AI/IBotState.cs`, `AI/IDependencyContainer.cs`,
   `AI/DependencyContainer.cs`, `AI/PlayerTracker.cs`, `AI/StuckHelper.cs`, `AI/Bot.cs`, then all
   28 files in `AI/SharedStates/`.
7. **Bot loading and services** - `BotLoader.cs` (MEF), `DiscordClientWrapper.cs`,
   `HotspotGenerator.cs`, `TravelPathGenerator.cs`, and `Loader.cs` rewritten as the `hostfxr`
   entry point (see "The native side").
8. **UI abstractions and viewmodels** - `BloogBot.UI.Abstractions` and `BloogBot.UI.Core`
   (`MainViewModel`, `CommandHandler`, `BotService`). See "The UI layer".
9. **The WPF shell** - `BloogBot.UI.Wpf`: `App.xaml`, `MainWindow.xaml`, `Themes/Dark.xaml`,
   `Themes/Light.xaml`, the host implementations. This is the default and the reference shell.
10. **The Avalonia shell** - `BloogBot.UI.Avalonia`, at parity with group 9.
11. **The 17 bot plugins** - `AfflictionWarlockBot`, `ArcaneMageBot`, `ArmsWarriorBot`,
    `BackstabRogueBot`, `BalanceDruidBot`, `BeastmasterHunterBot`, `CombatRogueBot`,
    `ElementalShamanBot`, `EnhancementShamanBot`, `FeralDruidBot`, `FrostMageBot`, `FuryWarriorBot`,
    `ProtectionPaladinBot`, `ProtectionWarriorBot`, `RetributionPaladinBot`, `ShadowPriestBot`,
    `TestBot`. Mechanical - one `.csproj` and a straight copy of the sources each. Several per run.
12. **Bootstrapper** - `Bootstrapper/Program.cs`, `WinImports.cs`, `BootstrapperSettings.cs`.
13. **The native side** - `Loader/dllmain.cpp` rewritten against `nethost` / `hostfxr`; `FastCall`,
    `Navigation` and `NavigationTests` copied with build-configuration changes only. See "The
    native side".
14. **Tests** - `BloogBotTests` on MSTest 3.x.

## Project layout

One solution under `$code_root_dir/Code2/C#/BloogBot_net9`, SDK-style throughout:

| Project | TFM | Output | Notes |
| --- | --- | --- | --- |
| `BloogBot` | `net9.0-windows` | `Library` | everything except `UI/`. `EnableDynamicLoading=true` so a `.runtimeconfig.json` is emitted for `hostfxr` |
| `BloogBot.UI.Abstractions` | `net9.0` | `Library` | contracts + MVVM primitives. **Plain `net9.0` on purpose** - a WPF type here will not compile |
| `BloogBot.UI.Core` | `net9.0` | `Library` | `MainViewModel`, `CommandHandler`, `BotService`. Same reason |
| `BloogBot.UI.Wpf` | `net9.0-windows` | `Library` | `UseWPF=true`. The default shell |
| `BloogBot.UI.Avalonia` | `net9.0-windows` | `Library` | the alternate shell |
| `Bootstrapper` | `net9.0-windows` | `Exe` | |
| 17 bot plugins | `net9.0-windows` | `Library` | one per spec, names unchanged |
| `BloogBotTests` | `net9.0-windows` | `Library` | MSTest 3.x |
| `Loader`, `FastCall`, `Navigation`, `NavigationTests` | - | native | `vcxproj`, unchanged layout |

`BloogBot` is a **library**, not a `WinExe`. Under .NET Framework the loader called
`ExecuteInDefaultAppDomain` against `BloogBot.exe`; `hostfxr` loads an assembly that has a
`runtimeconfig.json` next to it instead. `Bootstrapper` stays an executable.

Everything is **x86**. The WoW clients BloogBot injects into are 32-bit, so `PlatformTarget` is
`x86` and the RID is `win-x86` for every managed project, exactly as the original solution had it.

`BloogBot` holds the conditional reference to the selected shell and nothing else knows which shell
is in play:

```xml
<ItemGroup>
  <ProjectReference Include="..\BloogBot.UI.Core\BloogBot.UI.Core.csproj" />
  <ProjectReference Include="..\BloogBot.UI.Wpf\BloogBot.UI.Wpf.csproj" Condition="'$(Ui)'=='Wpf'" />
  <ProjectReference Include="..\BloogBot.UI.Avalonia\BloogBot.UI.Avalonia.csproj" Condition="'$(Ui)'=='Avalonia'" />
</ItemGroup>
```

### Directory and file mapping

The port mirrors the original directory tree one-for-one, with one exception: `BloogBot/UI/` splits
across the four UI projects.

| Original | Port |
| --- | --- |
| `BloogBot/AI/**` | `BloogBot/AI/**` |
| `BloogBot/Game/**` | `BloogBot/Game/**` |
| `BloogBot/*.cs` (root) | `BloogBot/*.cs` |
| `BloogBot/UI/MainViewModel.cs` | `BloogBot.UI.Core/MainViewModel.cs` |
| `BloogBot/UI/CommandHandler.cs` | `BloogBot.UI.Core/CommandHandler.cs` |
| `BloogBot/UI/BotService.cs` | `BloogBot.UI.Core/BotService.cs` |
| `BloogBot/UI/App.xaml(.cs)` | `BloogBot.UI.Wpf/App.xaml(.cs)` **and** `BloogBot.UI.Avalonia/App.axaml(.cs)` |
| `BloogBot/UI/MainWindow.xaml(.cs)` | `BloogBot.UI.Wpf/MainWindow.xaml(.cs)` **and** `BloogBot.UI.Avalonia/MainWindow.axaml(.cs)` |
| `<Spec>Bot/**` | `<Spec>Bot/**` |
| `Bootstrapper/**` | `Bootstrapper/**` |
| `Loader/`, `FastCall/`, `Navigation/`, `NavigationTests/` | same names |

**Namespaces are unchanged.** `BloogBot`, `BloogBot.AI`, `BloogBot.Game`, `BloogBot.Game.Objects`,
`BloogBot.Game.Enums`, `BloogBot.Game.Frames`, `ArcaneMageBot`, `Bootstrapper` and the rest keep
their exact spelling. The UI namespace `BloogBot.UI` stays on the viewmodels; the shells add
`BloogBot.UI.Wpf` and `BloogBot.UI.Avalonia`.

### Every ported file carries a header line

The first line of every ported `.cs` / `.xaml` / `.axaml`, above the `using` block, is:

```csharp
// Ported from BloogBot/AI/SharedStates/GrindState.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
```

The path is relative to the source tree root, exactly as `find` prints it. This is not decoration:
**it is how the next run works out what is already done** (see "Orient"). A file with no header line
is invisible to the next run and will be ported twice.

For the two shells, both point at the same original:

```xml
<!-- Ported from BloogBot/UI/MainWindow.xaml (.NET Framework 4.8 -> .NET 9, Avalonia shell). Replica - do not redesign. -->
```

### Not ported

These never come up again:

- `packages/` - replaced by `PackageReference`.
- `packages.config`, `App.config`, `Properties/AssemblyInfo.cs`, `Properties/Resources.resx`,
  `Properties/Settings.settings` and their generated `.Designer.cs` - SDK-style projects generate
  assembly attributes, and nothing in BloogBot reads `Resources` or `Settings`. Confirm that with a
  grep before dropping them; if something does read them, port that file instead.
- `Bot/`, `Debug/`, `Release/`, `x64/`, `obj/`, `bin/` - build output.
- `bloogbot_diff.ps1`, `bloogbot_current_vs_prefork.diff`, `bloogbot_downloaded_vs_prefork.diff` -
  tooling for `bloogbot-upstream-sync`, which stays in the source tree.
- `UPSTREAM_SYNC.md` and `.claude/` - they belong to the source repository.
- `Fasm.NET.dll` and every other prebuilt binary under `Bot/` - see "Known blockers".

## Replication rules

The original is the source of truth. Port it with **no** behavioral change:

1. Keep control flow, loops, conditions, arithmetic, field assignments, error handling, call order
   and side effects exactly as they are.
2. Keep every comment, in the same position, with the same wording - including the ones that are
   wrong or stale.
3. Keep member order, access modifiers and `static` / `readonly` / `internal` spelling. BloogBot
   writes `static internal` and `static public` in places; keep that order too.
4. Keep field and local names verbatim, including the odd ones.
5. Keep log lines, chat output and UI strings byte-identical - same text, same casing, same order.
6. Keep `botSettings.json` and `bootstrapperSettings.json` keys and defaults identical, so an
   existing install works unchanged.
7. Keep the SQL and the schema files identical. `SqliteSchema.SQL` and `TSqlSchema.SQL` are copied,
   not rewritten.
8. Do not add nullable annotations, file-scoped namespaces, records, primary constructors,
   collection expressions, `var` where the original wrote a type, or a type where the original wrote
   `var`. `ImplicitUsings` is **disabled** and `Nullable` is **disabled** precisely so the diff
   against the original stays readable.
9. Do not add tests, helpers, logging, null checks or argument validation the original does not
   have.
10. The one thing that *does* change is what has to: an API that does not exist on .NET 9. See
    "The .NET Framework to .NET 9 API map". Keep the change as small as possible and add a one-line
    comment saying what moved and why.

### POTENTIAL BUG FOUND

When the original code looks like a real defect - not a style choice, an actual bug - **port it
faithfully anyway** and tag it so it can be found later with one grep:

```csharp
// POTENTIAL BUG FOUND: botPaths spells the assembly 'BeastMasterHunterBot.dll' but the project
//   builds 'BeastmasterHunterBot.dll', so File.ReadAllBytes throws on a case-sensitive volume.
//   Original: BloogBot/BotLoader.cs:40
//   Ported as-is - behavior matches .NET Framework BloogBot.
```

Rules:

- The tag is the exact string `POTENTIAL BUG FOUND`, uppercase, so `grep -rn "POTENTIAL BUG FOUND"`
  finds every one of them.
- It sits immediately above the code it is about, never at the top of the file.
- Three parts: what looks wrong, the original `file:line`, and the confirmation that it was ported
  unchanged.
- It works in every file type the port touches: `// POTENTIAL BUG FOUND: ...` in `.cs` and `.cpp`,
  `<!-- POTENTIAL BUG FOUND: ... -->` in `.xaml`, `.axaml` and `.csproj`.
- **Never fix the bug.** Not even an obvious one-character one. The port has to behave the way the
  original does, or the two trees stop being comparable. If a bug is bad enough that the port cannot
  run at all with it, tag it, keep the original behavior behind `#if !USE_CUSTOM_CHANGES`-style
  reasoning **only if the original already had such a guard**, and otherwise say so in the summary
  and let the user decide.
- Every tag is also listed in `PORT_STATUS.md` under "Potential bugs found", with the file, the line
  and the one-line description, so there is a single readable inventory as well as the grep.
- Do not tag style, naming, dead code, a missing `using`, a magic number, a `catch` that swallows by
  design, or anything that merely reads oddly. The bar is: **this would misbehave at runtime**.

Known candidates, listed so they are not re-litigated every run - tag them when the file is reached:
the `BeastMasterHunterBot.dll` casing in `BotLoader.cs`, the fixed `wchar_t buffer[255]` module-path
buffer in `Loader/dllmain.cpp` (a longer install path truncates silently), and the
`TerminateThread` on `DLL_PROCESS_DETACH` in the same file.

## USE_CUSTOM_CHANGES

The source tree guards every local customization behind the `USE_CUSTOM_CHANGES` preprocessor
directive, maintained by `bloogbot-upstream-sync`: 22 files carry it today, `AI/Bot.cs` alone has 23
sites. **The port carries every one of them across unchanged** - same symbol name, same placement,
same `#else` branch, same nesting.

```csharp
#if USE_CUSTOM_CHANGES
    // the fork's behavior
#else
    // upstream's behavior
#endif
```

Rules:

- Never collapse a directive to whichever branch happens to be active, never turn it into a runtime
  `if`, never drop the `#else` branch, never re-indent around it.
- **Both branches get ported.** A C# `#if` branch that is off is not compiled and not type-checked,
  so one build only proves one configuration - see "Verification", which builds both ways.
- The symbol is defined the way the source tree defines it - an MSBuild property with a default -
  but set once in `Directory.Build.props` instead of being repeated in 20 `.csproj` files.
- `Loader/dllmain.cpp` has a `#ifdef USE_CUSTOM_CHANGES` block too, the debugger-wait skip. It
  survives the `hostfxr` rewrite: the CLR-hosting calls around it change, that block does not.
- New code this port writes - the UI abstraction, the shells, the `hostfxr` loader - is **not**
  wrapped in `USE_CUSTOM_CHANGES`. That symbol means "diverges from upstream BloogBot", and the port
  as a whole diverges by definition. Growing the guard to cover port scaffolding makes it useless to
  `bloogbot-upstream-sync` later.

## Setup mode

Create, in this order:

1. **`$code_root_dir/Code2/C#/BloogBot_net9/`** and `git init -b net9-port` inside it.
   `Code2/C#` already exists and holds the other C# projects, so create only the `BloogBot_net9`
   directory inside it - nothing else in `Code2/C#` is touched.

2. **`Directory.Build.props`**:

   ```xml
   <Project>

     <PropertyGroup>
       <TargetFramework>net9.0-windows</TargetFramework>
       <Platforms>x86</Platforms>
       <PlatformTarget>x86</PlatformTarget>
       <RuntimeIdentifier>win-x86</RuntimeIdentifier>
       <SelfContained>false</SelfContained>
       <LangVersion>latest</LangVersion>
       <!-- deliberate: the original is C# 7.3 with no nullable annotations and explicit usings
            in every file. Enabling either would mean rewriting 249 files and would make the
            diff against BloogBot unreadable. See "Replication rules". -->
       <Nullable>disable</Nullable>
       <ImplicitUsings>disable</ImplicitUsings>
       <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
       <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
       <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
       <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
       <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
     </PropertyGroup>

     <!-- Mirrors the same switch in the .NET Framework tree, where it lives in every csproj.
          Local customizations on top of upstream BloogBot; set to false to build the tree as
          upstream wrote it. Override per build with -p:UseCustomChanges=false -->
     <PropertyGroup>
       <UseCustomChanges Condition="'$(UseCustomChanges)'==''">true</UseCustomChanges>
     </PropertyGroup>
     <PropertyGroup Condition="'$(UseCustomChanges)'=='true'">
       <DefineConstants>$(DefineConstants);USE_CUSTOM_CHANGES</DefineConstants>
     </PropertyGroup>

     <!-- Which UI shell is compiled in. Wpf is the default; Avalonia is the alternate.
          Override per build with -p:Ui=Avalonia. Both shells must always build and must
          always be at feature parity - see "The UI layer". -->
     <PropertyGroup>
       <Ui Condition="'$(Ui)'==''">Wpf</Ui>
     </PropertyGroup>
     <PropertyGroup Condition="'$(Ui)'=='Wpf'">
       <DefineConstants>$(DefineConstants);UI_WPF</DefineConstants>
     </PropertyGroup>
     <PropertyGroup Condition="'$(Ui)'=='Avalonia'">
       <DefineConstants>$(DefineConstants);UI_AVALONIA</DefineConstants>
     </PropertyGroup>
     <Target Name="CheckUi" BeforeTargets="BeforeBuild">
       <Error Condition="'$(Ui)'!='Wpf' And '$(Ui)'!='Avalonia'"
              Text="Ui must be Wpf or Avalonia - got '$(Ui)'." />
     </Target>

   </Project>
   ```

   The two UI-agnostic projects override `TargetFramework` to plain `net9.0` in their own `.csproj`
   - that is what makes a leaked WPF type a compile error rather than a review finding.

3. **`BloogBot_net9.sln`** and the `.csproj` files listed under "Project layout", each holding only
   what `Directory.Build.props` does not: `OutputType`, `RootNamespace`, `AssemblyName`, `UseWPF`,
   `EnableDynamicLoading`, its `PackageReference`s and its `ProjectReference`s. The output paths
   mirror the original: every managed project writes to `..\Bot\` in Debug and `..\Bot\Release\` in
   Release, so the injected layout is unchanged.

   **Add only the packages a ported file actually needs.** The full mapping is in "NuGet packages";
   each one goes in when the first file that needs it is ported, and the run that adds one says so
   in its summary.

4. **`.gitignore`**:

   ```gitignore
   bin/
   obj/
   Bot/
   .vs/
   .idea/
   .vscode/
   *.user
   *.suo
   *.pdb
   # runtime output
   *.log
   db.db
   # written fresh by every run of the bloogbot-net9-port skill
   commit_message.txt
   ```

5. **Non-code assets, copied byte-for-byte** from the source tree: `botSettings.json`,
   `bootstrapperSettings.json`, `BloogBot/SqliteSchema.SQL`, `BloogBot/TSqlSchema.SQL`,
   `SqlSchema.SQL`, `Sql/`, `Docs/`, `LICENSE`. Do not reformat or re-key any of them.

6. **`README.md`** - what the port is, the project table, the four build commands from
   "Verification", how `-p:Ui=` and `-p:UseCustomChanges=` work, which x86 .NET runtime has to be
   installed for injection to work, how `grep -rn "POTENTIAL BUG FOUND"` is meant to be used, and
   the note that the source tree at `Code2/C#/BloogBot` stays the reference and keeps receiving
   upstream syncs.

7. **`PORT_STATUS.md`** - the ledger continue mode reads and updates. Committed, short, and
   append-only apart from the two live lines at the top:

   ```markdown
   # BloogBot_net9 port status

   Maintained by the `bloogbot-net9-port` skill. A hint for the next run, not the source of truth -
   the two trees are. Re-derive from this directory with the commands in the skill's "Orient"
   section.

   **Last run:** setup - solution scaffolding
   **Next:** group 2 - leaf types (Game/Position, Game/XYZ, Game/Enums, ...)

   ## Porting order

   - [ ] 1. Setup - solution, Directory.Build.props, csproj files, assets
   - [ ] 2. Leaf types - Position, XYZ, Game/Enums, Hotspot, Npc, TravelPath, Logger, BotSettings
   - [ ] 3. Memory and native interop - MemoryManager, Detour, Hack, ThreadSynchronizer, Navigation
   - [ ] 4. Game layer - Objects, Frames, ObjectManager, Functions, the three function handlers
   - [ ] 5. Data layer - IRepository, Repository, Sql/Sqlite/TSql repositories
   - [ ] 6. AI layer - Bot, DependencyContainer, the 28 SharedStates
   - [ ] 7. Bot loading and services - BotLoader, DiscordClientWrapper, Loader (hostfxr entry)
   - [ ] 8. UI abstractions and viewmodels - BloogBot.UI.Abstractions, BloogBot.UI.Core
   - [ ] 9. WPF shell - BloogBot.UI.Wpf (default, the reference shell)
   - [ ] 10. Avalonia shell - BloogBot.UI.Avalonia, at parity with 9
   - [ ] 11. The 17 bot plugins
   - [ ] 12. Bootstrapper
   - [ ] 13. Native - Loader/dllmain.cpp on hostfxr, FastCall/Navigation build config
   - [ ] 14. Tests - BloogBotTests on MSTest 3.x

   ## Potential bugs found

   | File (port) | Line | What looks wrong | Original |
   | --- | --- | --- | --- |
   | (none yet) | | | |

   ## Deviations from the original

   Every place the port had to differ, and why. One line each.

   | Port file | What differs | Why |
   | --- | --- | --- |
   | (none yet) | | |

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
   | setup | scaffolding | - | green | green | green | green |
   ```

   Tick a group only when every file in it is ported and TODO-free. A partially done big file gets a
   sub-bullet naming the last ported member, so the next run can resume without re-reading the whole
   original. If the ledger ever contradicts the trees, fix the ledger.

Do not scaffold empty files for types that have not been ported yet.

## The UI layer

The original UI is one WPF window: `UI/MainWindow.xaml` (1,544 lines, 7 tabs) bound to
`UI/MainViewModel.cs` (1,901 lines), with `UI/App.xaml`, `UI/CommandHandler.cs` and
`UI/BotService.cs` around it. It is already close to MVVM - the viewmodel touches only
`System.Windows.Input.ICommand`, which is in the base class library on .NET 9 - so the split is
mostly mechanical.

**Both shells are first-class.** Neither is a stub, neither is a demo. Every control, every binding,
every command, every tab that exists in one exists in the other, and the two look and behave
substantially the same.

### BloogBot.UI.Abstractions (`net9.0`)

Contracts only, no implementation beyond the MVVM primitives, **no reference to WPF or Avalonia**.
Plain `net9.0` rather than `net9.0-windows` so that a leaked `System.Windows.Media` type is a
compile error.

| Type | What it is for |
| --- | --- |
| `IUiHost` | starts the shell: `void Run()`, `void Shutdown()`, `IMainWindowHandle MainWindow { get; }` |
| `IUiDispatcher` | `void Invoke(Action)`, `void BeginInvoke(Action)`, `bool CheckAccess()` - the shell's UI thread |
| `IDialogService` | `void ShowMessage(string title, string message)`, `bool Confirm(string title, string message)`, `string OpenFile(string filter)`, `string SaveFile(string filter, string suggestedName)` |
| `IThemeService` | `UiTheme Current { get; }`, `void Apply(UiTheme theme)`, `event EventHandler ThemeChanged` |
| `UiTheme` | `enum UiTheme { Dark, Light }` - **`Dark` is the first member and the default** |
| `IClipboardService` | whatever `MainViewModel` copies today, if anything |
| `ObservableObject` | `INotifyPropertyChanged` base with `Set<T>(ref T, T, [CallerMemberName] string)` |
| `RelayCommand` | the `ICommand` implementation. `UI/CommandHandler.cs` is the original's version - port it here under its own name and keep the behavior |

Keep the surface small. Add an interface only when a shell actually needs one; do not design for
imagined shells.

### BloogBot.UI.Core (`net9.0`)

`MainViewModel`, `CommandHandler` and `BotService`, ported **as replicas** under namespace
`BloogBot.UI`. The only permitted changes:

- `using System.Windows.Documents;` is dropped if nothing in the file uses it (check first - if
  something does, that use is routed through an abstraction instead).
- Anything that reached for a WPF type goes through `IUiDispatcher` / `IDialogService` /
  `IThemeService`, injected through the constructor. Nothing else.
- Everything else - every property, every command, every string, every piece of logic, every
  `#if USE_CUSTOM_CHANGES` - is identical to the original.

The viewmodel is the **single source of UI behavior**. If a shell needs code-behind beyond
`InitializeComponent()` and wiring the `DataContext`, that is a sign logic leaked into the view;
move it back.

### BloogBot.UI.Wpf (`net9.0-windows`, default)

`UseWPF=true`. `App.xaml` / `App.xaml.cs`, `MainWindow.xaml` / `MainWindow.xaml.cs`,
`Themes/Dark.xaml`, `Themes/Light.xaml`, and the `IUiHost` / `IUiDispatcher` / `IDialogService` /
`IThemeService` implementations.

`MainWindow.xaml` is a straight port of the original: same `Window` attributes (`Title="BloogBot"`,
the same `MinHeight` / `Height` / `MaxHeight` / `MinWidth` / `Width` / `MaxWidth`, `ResizeMode`,
`SizeToContent`), same `Grid` structure, same `TabControl` with the same seven tabs in the same
order, same controls with the same `Content=` and `Header=` text, same `Binding` paths, same
`DataTemplate`s, same `MultiBinding`s.

Two changes only: the hardcoded colors are replaced by `{DynamicResource <Key>}` against the theme
keys below, and `<Window.DataContext><local:MainViewModel /></Window.DataContext>` becomes a
`DataContext` assigned in code-behind, because the viewmodel now takes constructor dependencies.

Because `BloogBot` is a library rather than a `WinExe`, the XAML-generated `Main` no longer exists.
The shell provides its own, keeping the original `App.OnStartup` body verbatim:

```csharp
public partial class App : Application
{
    [STAThread]
    public static void Main()
    {
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // ... the original body, unchanged
    }
}
```

`BloogBot.UI.Avalonia` exposes a type with the same name and the same static `Main`, so
`BloogBot/Loader.cs` differs between the two builds by its `using` line alone.

### BloogBot.UI.Avalonia (`net9.0-windows`)

Avalonia 11.x. `App.axaml` / `App.axaml.cs`, `MainWindow.axaml` / `MainWindow.axaml.cs`,
`Themes/Dark.axaml`, `Themes/Light.axaml`, and the same four host implementations.

The control set the original uses maps one-for-one onto Avalonia 11: `Grid`, `RowDefinition`,
`ColumnDefinition`, `Label`, `CheckBox`, `Button`, `TextBox`, `TextBlock`, `ComboBox`, `TabControl`,
`TabItem`, `DataTemplate`, `MultiBinding`, `Separator`, `ItemsControl`, `StackPanel`,
`ScrollViewer`. Nothing in the window needs an Avalonia-only workaround.

The differences that do come up, and how to handle them:

| WPF | Avalonia 11 |
| --- | --- |
| `x:Class` on `<Window>` | same, in `.axaml` |
| `{DynamicResource Key}` | same |
| `ComboBox.ItemsSource` | same (Avalonia 11; it was `Items` in 0.10) |
| `Visibility="Collapsed"` | `IsVisible="False"` |
| `Margin="10,0,10,0"` | same |
| `Label` `Content` | same |
| `MultiBinding` + `IMultiValueConverter` | same shape, `Avalonia.Data.Converters.IMultiValueConverter` |
| `Dispatcher.Invoke` | `Dispatcher.UIThread.Invoke` |
| `MessageBox.Show` | no built-in - implement `IDialogService.ShowMessage` with a small `Window` |
| `Microsoft.Win32.OpenFileDialog` | `StorageProvider.OpenFilePickerAsync` |
| `Application.Current.Resources.MergedDictionaries` | `Application.Current.Styles` / `RequestedThemeVariant` |

When a construct genuinely has no counterpart, implement the same user-visible behavior with what
Avalonia has, and record it in the "Deviations from the original" table in `PORT_STATUS.md`.

### Theming

Both shells define the **same resource keys with the same values**, which is what makes them look
alike. Dark is the default in both; light is a toggle.

| Key | Dark | Light |
| --- | --- | --- |
| `WindowBackgroundBrush` | `#FF1E1E1E` | `#FFF5F5F5` |
| `PanelBackgroundBrush` | `#FF252526` | `#FFFFFFFF` |
| `ControlBackgroundBrush` | `#FF2D2D30` | `#FFFFFFFF` |
| `ControlBorderBrush` | `#FF3F3F46` | `#FFCCCCCC` |
| `ForegroundBrush` | `#FFF1F1F1` | `#FF1E1E1E` |
| `MutedForegroundBrush` | `#FF9D9D9D` | `#FF6E6E6E` |
| `AccentBrush` | `#FF0E639C` | `#FF0078D4` |
| `AccentForegroundBrush` | `#FFFFFFFF` | `#FFFFFFFF` |
| `SuccessBrush` | `#FF4EC9B0` | `#FF107C10` |
| `WarningBrush` | `#FFD7BA7D` | `#FF9D5D00` |
| `ErrorBrush` | `#FFF48771` | `#FFC42B1C` |
| `SeparatorBrush` | `#FF3F3F46` | `#FFE0E0E0` |
| `SelectionBrush` | `#FF094771` | `#FFCCE4F7` |
| `TabActiveBackgroundBrush` | `#FF1E1E1E` | `#FFFFFFFF` |
| `TabInactiveBackgroundBrush` | `#FF2D2D30` | `#FFECECEC` |

Rules:

- **No hardcoded color anywhere in either `MainWindow`.** Every brush is `{DynamicResource <Key>}`.
  A literal `#RRGGBB` in a view is a review finding.
- Adding a key means adding it to **all four** theme files in the same run, with the same name.
- The toggle lives in the same place in both shells - a `CheckBox` labelled `Dark Mode`, checked by
  default, in the Overview tab next to the existing general controls - bound to a `MainViewModel`
  property that calls `IThemeService.Apply`.
- The choice is persisted in `botSettings.json` as `"Theme": "Dark"`, read at startup, defaulting to
  `Dark` when the key is absent or unrecognized. This is the one settings key the port adds; record
  it in "Deviations from the original" and keep the rest of the file byte-identical.
- WPF swaps `Application.Current.Resources.MergedDictionaries[0]` between `Themes/Dark.xaml` and
  `Themes/Light.xaml`. Avalonia sets `Application.Current.RequestedThemeVariant` and swaps the
  matching `ResourceInclude`. Both raise `IThemeService.ThemeChanged`.
- The theme must switch **live**, without restarting the shell, in both.

### Parity

A run that touches either shell proves they still match, and reports the result:

```bash
DST="$code_root_dir/Code2/C#/BloogBot_net9"
W="$DST/BloogBot.UI.Wpf/MainWindow.xaml"
A="$DST/BloogBot.UI.Avalonia/MainWindow.axaml"

# same bound viewmodel members in both
diff <(grep -o 'Binding [A-Za-z_][A-Za-z0-9_.]*' "$W" | sort -u) \
     <(grep -o 'Binding [A-Za-z_][A-Za-z0-9_.]*' "$A" | sort -u)

# same visible text in both
diff <(grep -oE '(Content|Header|Text)="[^"{][^"]*"' "$W" | sort -u) \
     <(grep -oE '(Content|Header|Text)="[^"{][^"]*"' "$A" | sort -u)

# same theme keys in all four theme files
for f in "$DST"/BloogBot.UI.Wpf/Themes/*.xaml "$DST"/BloogBot.UI.Avalonia/Themes/*.axaml; do
  echo "== $f"; grep -o 'x:Key="[^"]*"' "$f" | sort
done
```

Both diffs must come back empty, and the four key lists must be identical. Tick the tab in the
"UI parity" table in `PORT_STATUS.md` only when both shells have it and both diffs are clean.

## The native side

`Bootstrapper.exe` starts `WoW.exe` and `LoadLibraryW`s `Loader.dll` into it. `Loader.dll` then
starts a CLR inside the WoW process and calls the managed entry point. That second half is the only
part of the native side that changes.

### What the original does, and why it cannot stay

```cpp
CLRCreateInstance(CLSID_CLRMetaHostPolicy, ...);
g_pMetaHost->GetRequestedRuntime(METAHOST_POLICY_HIGHCOMPAT, dllLocation, ...);
g_pRuntimeInfo->BindAsLegacyV2Runtime();
g_pRuntimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, ...);
g_clrHost->Start();
g_clrHost->ExecuteInDefaultAppDomain(dllLocation, L"BloogBot.Loader", L"Load", L"NONE", &dwRet);
```

`ICLRMetaHostPolicy`, `ICLRRuntimeHost`, `BindAsLegacyV2Runtime` and `ExecuteInDefaultAppDomain` are
.NET Framework hosting APIs. .NET 9 has no app domains and no `mscoree` host; it is hosted through
`nethost` / `hostfxr` instead.

### What replaces it

```cpp
// nethost resolves the hostfxr for this process's bitness - x86 here, since WoW.exe is 32-bit.
get_hostfxr_path(hostfxr_path, &buffer_size, nullptr);
// load hostfxr, then resolve: hostfxr_initialize_for_runtime_config,
//                             hostfxr_get_runtime_delegate,
//                             hostfxr_close
hostfxr_initialize_for_runtime_config(L"<dir>\\BloogBot.runtimeconfig.json", nullptr, &ctx);
hostfxr_get_runtime_delegate(ctx, hdt_load_assembly_and_get_function_pointer, (void**)&load_fn);
load_fn(L"<dir>\\BloogBot.dll",
        L"BloogBot.Loader, BloogBot",
        L"Load",
        nullptr,              // default component_entry_point_fn signature
        nullptr,
        (void**)&entry);
entry(nullptr, 0);            // int (*)(void* arg, int32_t argSizeInBytes)
```

Requirements that follow from it, all of which the port has to satisfy:

- `BloogBot` is built with `<EnableDynamicLoading>true</EnableDynamicLoading>` so
  `BloogBot.runtimeconfig.json` and the dependency closure land next to `BloogBot.dll`.
- `BloogBot.Loader` becomes a **public** class and `Load` a **public static** method with the
  `component_entry_point_fn` signature: `public static int Load(IntPtr arg, int argSize)`. The
  original `static int Load(string args)` signature cannot be reached through `hostfxr`. Record it
  in "Deviations from the original"; the body - spin an STA thread and run the shell - is unchanged.
- Injection needs the **x86 .NET 9 Desktop Runtime** installed
  (`C:\Program Files (x86)\dotnet\`), because `get_hostfxr_path` resolves per bitness and WoW is
  32-bit. Say this in `README.md`. A self-contained x86 publish with `hostfxr.dll` beside
  `Loader.dll` is the fallback if the user would rather not install a runtime; do not switch to it
  without being asked.
- `nethost.lib` is linked and `nethost.dll` is copied next to `Loader.dll` by a post-build step.
  `#pragma comment(lib, "mscoree")` goes away.
- `BindAsLegacyV2Runtime` has no counterpart and simply disappears - there is no mixed-mode
  .NET 3.5 assembly to bind for. Note the drop in a comment where the call used to be.
- On `DLL_PROCESS_DETACH` there is no `ICLRRuntimeHost::Stop`. .NET 9 cannot be unloaded from a
  process; the close is `hostfxr_close(ctx)` on the init context and nothing else. Keep the rest of
  `DllMain` as it is - including the `TerminateThread`, which gets a `POTENTIAL BUG FOUND` tag.
- **Everything else in `dllmain.cpp` is a replica**: the `AllocConsole` / `freopen` console setup,
  the `#ifdef USE_CUSTOM_CHANGES` debugger-wait block, the `MB()` macro, the message-box error
  handling for each `HRESULT`, `LoadClr`, the thread creation through `_beginthreadex`, and the
  comment attributing the file to Zzuk.

### The other three native projects

`FastCall`, `Navigation` and `NavigationTests` are pure native code with no CLR involvement.
**Copy them verbatim** - every `.cpp`, `.h`, the Detour and g3dlite subtrees, the `.vcxproj.filters`.
Change only what the new tree needs: the output path, the platform toolset if the installed Visual
Studio requires it, and the `Windows SDK` version. No source change. If one of them fails to build,
report it and stop; do not start editing native pathfinding code.

`BloogBot/Navigation.cs` P/Invokes `Navigation.dll` by name - that still works unchanged on .NET 9,
as does every other `[DllImport]` in the tree.

## The .NET Framework to .NET 9 API map

Most of BloogBot ports without a single character changing: `[DllImport]`, `Marshal`, `unsafe`,
`IntPtr`, `Thread` with `SetApartmentState`, `System.Text`, `System.Linq` and the whole game layer
are identical. These are the places that are not.

| Original | .NET 9 | Notes |
| --- | --- | --- |
| `System.ComponentModel.Composition` (MEF) | same namespace, `System.ComponentModel.Composition` package | `[ImportMany]`, `AggregateCatalog`, `AssemblyCatalog`, `CompositionContainer` all still exist. `BotLoader.cs` ports unchanged |
| `AppDomain.CurrentDomain.AssemblyResolve` | `AssemblyLoadContext.Default.Resolving` | .NET 9 still raises `AssemblyResolve`, so `BotLoader`'s handler compiles and works. Keep it as it is; do not pre-emptively rewrite it |
| `Assembly.Load(byte[])` | same | still supported |
| `System.Data.SQLite` | `System.Data.SQLite.Core` package | ships `netstandard2.1` plus the x86 `SQLite.Interop.dll`. The `using System.Data.SQLite;` line and every call site stay identical |
| `System.Data.SqlClient` | `System.Data.SqlClient` package | keeps `TSqlRepository.cs` and `SqlRepository.cs` character-identical. `Microsoft.Data.SqlClient` is the alternative if the old package misbehaves, but it changes the `using` line - only switch on a real failure, and record it |
| `Discord.Net` 2.2.0 (`net461`) | `Discord.Net` 3.x | the 2.x assemblies are `net461`-only. 3.x renames a few members; port `DiscordClientWrapper.cs` to the 3.x equivalents and record each change |
| `Newtonsoft.Json` 13.0.3 | same package | unchanged |
| `Microsoft.Win32.Registry` | `Microsoft.Win32.Registry` package | unchanged |
| `System.Configuration.ConfigurationManager` / `App.config` | dropped | nothing in BloogBot reads it - confirm with a grep before dropping |
| `MSTest` 2.2.7 | `MSTest` 3.x (`MSTest.TestAdapter` + `MSTest.TestFramework`) | `[TestClass]` / `[TestMethod]` unchanged |
| `Moq` 4.18.3, `Castle.Core` 5.1.0 | same versions | already `netstandard2.0` |
| `System.Memory`, `System.Buffers`, `System.Numerics.Vectors`, `System.Runtime.CompilerServices.Unsafe`, `System.Threading.Tasks.Extensions`, `System.ValueTuple`, `Microsoft.Bcl.*` | **dropped** | all in the .NET 9 base class library. Removing them is not a code change |
| `System.Text.Json`, `System.Collections.Immutable`, `System.IO.Pipelines`, `System.Threading.Tasks.Dataflow`, `System.Diagnostics.DiagnosticSource` | **dropped** | in-box on .NET 9 |
| `MessagePack`, `Nerdbank.MessagePack`, `StreamJsonRpc`, `Nerdbank.Streams`, `PolyType`, `Microsoft.VisualStudio.Threading` | keep only if referenced | grep first: several came in as transitive dependencies of the old `packages.config` and no source file uses them. Do not carry a package across that nothing references |
| `System.Linq.Async` / `System.Interactive.Async` | keep only if referenced | same check |
| WPF `System.Windows.*` | `net9.0-windows` + `UseWPF=true` | in-box, no package |
| `App.Main` generated by `ApplicationDefinition` | hand-written `[STAThread] static Main` | `BloogBot` is a library now - see "The UI layer" |

Rule: **a package is carried across only when a ported `.cs` file actually references it.** Run the
check before adding one:

```bash
grep -rn "using MessagePack\|using StreamJsonRpc\|using Nerdbank" "$SRC" --include='*.cs' | grep -v packages
```

## Known blockers

Read this before starting group 3.

| Thing | Why it blocks | What to do |
| --- | --- | --- |
| `Fasm.NET.dll` (`Binarysharp.Assemblers.Fasm`) | a **mixed-mode C++/CLI assembly built for `net461`**. Mixed-mode assemblies built against .NET Framework cannot load on .NET 9, so `MemoryManager.InjectAssembly` fails at first use. Used in 4 places, all in `MemoryManager.cs` | Stop and report before changing anything. The options, in order: (a) rebuild Binarysharp's Fasm.NET C++/CLI wrapper as a `vcxproj` in this solution with `<CLRSupport>NetCore</CLRSupport>` targeting `net9.0-windows` x86, (b) P/Invoke `FASM.DLL` directly and keep a thin managed `FasmNet` shim with the same four members the port uses (`Clear`, `AddLine`, `Assemble`, `Assemble(IntPtr)`), (c) replace with a managed x86 encoder. **Ask before picking one** - it is the single largest deviation in the port |
| x86 .NET 9 runtime | `hostfxr` resolves per bitness; a 64-bit-only .NET install cannot host inside 32-bit `WoW.exe` | Document it in `README.md`. If it is missing, the build still succeeds - only injection fails. Do not switch the solution to x64 |
| WPF at x86 | supported, but `Microsoft.WindowsDesktop.App` x86 must be installed | same as above |
| `Discord.Net` 3.x API drift | 2.x is `net461`-only, so an upgrade is forced | Port `DiscordClientWrapper.cs` against the 3.x API, keep every message string identical, and record each renamed member in "Deviations from the original" |

When a run hits a blocker it does not have an answer for: leave the affected file with a
`// TODO: port body here` marker and a comment naming the blocker, keep the solution building, say
so in the summary, and stop. **Do not invent a workaround that changes behavior.**

## Preprocessor directives

- `USE_CUSTOM_CHANGES` is carried over verbatim - see its own section above.
- `DEBUG` / `TRACE` keep their meaning; `App.xaml.cs`'s `#if DEBUG Debugger.Launch();` ports as-is.
- `UI_WPF` / `UI_AVALONIA` are **new**, added by this port, and appear in exactly one place -
  `BloogBot/Loader.cs`, selecting which shell's `App.Main` is started. If a second place needs them,
  that is a sign the abstraction is leaking; fix the abstraction instead.
- Do not introduce any other symbol.
- Do not turn a directive into a runtime `if`, do not evaluate it at port time, and do not drop the
  inactive branch.

## Verification

Compiling in **all four configurations** is the acceptance bar for a run:

```powershell
cd "$env:code_root_dir/Code2/C#/BloogBot_net9"
dotnet build BloogBot_net9.sln -c Debug
dotnet build BloogBot_net9.sln -c Debug -p:Ui=Avalonia
dotnet build BloogBot_net9.sln -c Debug -p:UseCustomChanges=false
dotnet build BloogBot_net9.sln -c Debug -p:Ui=Avalonia -p:UseCustomChanges=false
```

```bash
cd "$code_root_dir/Code2/C#/BloogBot_net9"
dotnet build BloogBot_net9.sln -c Debug
dotnet build BloogBot_net9.sln -c Debug -p:Ui=Avalonia
dotnet build BloogBot_net9.sln -c Debug -p:UseCustomChanges=false
dotnet build BloogBot_net9.sln -c Debug -p:Ui=Avalonia -p:UseCustomChanges=false
```

A run always builds the first two. It builds all four whenever it touched a file containing
`#if USE_CUSTOM_CHANGES`, and reports every result in the run-log row. A C# `#if` branch that is off
is **not** compiled and **not** type-checked, so an untouched configuration proves nothing about the
branch it excludes - say so explicitly when a run ports a branch it did not build.

The native projects are built separately, and only by a run that touched them:

```powershell
msbuild Loader\Loader.vcxproj /p:Configuration=Debug /p:Platform=Win32
msbuild FastCall\FastCall.vcxproj /p:Configuration=Debug /p:Platform=Win32
msbuild Navigation\Navigation.vcxproj /p:Configuration=Debug /p:Platform=Win32
```

**Do not run the bot.** Do not start `Bootstrapper.exe`, do not launch a WoW client, and do not
inject into a running process. The port is verified by compiling; an injection test is the user's
call, on their machine, with their client. If no .NET 9 SDK or no x86 targeting pack is available,
say so and stop, rather than reshaping the projects to compile.

Fix compile errors caused by the current slice. Do not rewrite unrelated ported code unless it is
necessary.

## Review mode

Compare the requested scope against the original and report:

1. Missing files, types, members or constants.
2. `TODO: port body here` markers still present.
3. Behavior differences - changed control flow, changed arithmetic, changed call order, a changed
   log or UI string, a reordered member.
4. Modernizations that slipped in: nullable annotations, file-scoped namespaces, `var` where the
   original wrote a type, an added null check, an added helper, a renamed field.
5. A `USE_CUSTOM_CHANGES` site dropped, collapsed to one branch, or turned into a runtime `if`.
6. A `POTENTIAL BUG FOUND` that was silently fixed instead of tagged, or a tag missing its original
   `file:line`, or a tag that is not in the `PORT_STATUS.md` inventory.
7. UI parity gaps - a control, binding, command, tab or theme key present in one shell and not the
   other; a hardcoded color in a view; a theme that does not switch live; a shell that does not
   default to dark.
8. WPF or Avalonia types leaking into `BloogBot.UI.Abstractions` or `BloogBot.UI.Core`, and logic
   that leaked from a viewmodel into a view's code-behind.
9. Native issues - a `mscoree` / `ICLRRuntimeHost` remnant, a missing `runtimeconfig.json`, an entry
   point whose signature does not match `component_entry_point_fn`, a changed `Navigation` or
   `FastCall` source file.
10. A NuGet package carried across that nothing references, or a `packages.config` remnant.
11. Anything written into the source tree at `Code2/C#/BloogBot`.
12. Build errors in the reviewed area.

Output: scope reviewed, matching parts, missing parts, behavior differences, suggested next edits.
Do not modify files unless the user explicitly asks.

## Output expectations

After the edits, summarize:

- which mode ran, and in continue mode which slice was picked and why, plus roughly how many lines
  were ported
- which original files were ported, into which project, and which extra ones were pulled in to make
  the slice compile
- every `POTENTIAL BUG FOUND` tag added this run - file, line, one line each - and confirmation that
  each was ported unchanged and added to the `PORT_STATUS.md` inventory
- every deviation from the original and why, with the `PORT_STATUS.md` row that records it
- every `USE_CUSTOM_CHANGES` site carried across, and which branches were ported but not compiled,
  and so not verified
- which NuGet packages were added, to which project, for which original reference - and which ones
  were dropped as in-box or unreferenced
- for a UI run: which tabs, controls and bindings landed in which shell, the result of both parity
  diffs, the theme-key check across all four theme files, and confirmation that both shells still
  default to dark and toggle live
- for a native run: what replaced the CLR-hosting calls, what `BloogBot.Loader.Load`'s signature is
  now, which native projects were rebuilt and with what result
- anything that could not be ported, and why - blockers especially
- which build commands were run and their results, one line each
- that `PORT_STATUS.md` was updated
- **a suggested commit message, written to `BloogBot_net9/commit_message.txt` rather than printed.**
  1-3 sentences, overwriting whatever the previous run left there; in the reply just say it was
  written, without reproducing it. It names what was ported and how, including anything adapted or
  left out, and is prefixed with the project name. A subject line, a blank line, then the body:

  ```text
  BloogBot_net9: port the AI shared states (AI/SharedStates, 28 files)

  Ported all 28 shared states from .NET Framework BloogBot unchanged, including the 22
  USE_CUSTOM_CHANGES sites across GrindState, CombatStateBase and the corpse states. Tagged
  two POTENTIAL BUG FOUND sites in RetrieveCorpseState. Builds green in all four
  configurations.
  ```

  Hard rules for this file:

  - **Never add a `Co-Authored-By:` trailer, and never add any other co-author, attribution or
    tool-generated line.** The message is the message and nothing else. This applies to the commit
    as well as to the file: if the user later asks for the message to be committed, it is committed
    verbatim, with no trailer appended, whatever the session's default attribution setting says.
  - **Never use double quotes (`"`)** - where a quote is needed, use a single quote (`'`).
  - Leave the file untouched if the run wrote no code at all, and say so.
