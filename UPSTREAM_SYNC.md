# Upstream sync ledger

Upstream: <https://github.com/DrewKestell/BloogBot> branch `main`, cloned at
`$USERPROFILE/Downloads/BloogBot`, wired into this repo as the `upstream-local` remote.
Fork point: `1d0057c` - Merge branch 'main' of github.com:DrewKestell/BloogBot into main
High-water mark: `1d0057c` (every commit up to and including this one is decided)
Upstream HEAD when last checked: `a9be5e8` `BeastmasterHunterBot: LOS checks, pet management, rest state rewrite` (2026-09-20)
Remaining after the high-water mark: `61`
Local customizations: guarded by `USE_CUSTOM_CHANGES`, defined in `BloogBot/BloogBot.csproj`,
`ArmsWarriorBot/ArmsWarriorBot.csproj`, `FrostMageBot/FrostMageBot.csproj`,
`ShadowPriestBot/ShadowPriestBot.csproj` and `Loader/Loader.vcxproj`
Branch: `main`. The `commented` branch is out of scope.

Statuses: `applied` (cherry-picked clean), `adapted` (landed, conflicts resolved by hand),
`partial` (part landed, part dropped - the notes say which), `skipped` (deliberately not landed),
`asked` (waiting on an answer from the user, cherry-pick left in progress or aborted).

| # | Commit | Subject | Status | Conflicts | Notes |
| --- | --- | --- | --- | --- | --- |
| - | `1d0057c` | (fork point - Phase 0 setup) | applied | - | XML doc comments stripped, all customizations guarded, toggles wired, both builds green. No upstream commit landed. |

## Guarded files

26 files carry `#if USE_CUSTOM_CHANGES`, 80 directives total. Re-derive with
`git grep -c USE_CUSTOM_CHANGES`; the tree is the source of truth, not this list.

| File | Directives | What is guarded |
| --- | --- | --- |
| `BloogBot/AI/Bot.cs` | 22 | `ResetValues`, BG/map-change/short-delay gating at the top of the main loop, level-up handling (`HandleLevelUp`, item/spell/talent grants, optional SMTP mail), stuck-in-combat force-teleport, both killswitches rerouted to `HandleBotStuck`, death handling, BG-aware corpse states, repair/inventory block, `Task.Delay(100)` vs `25`, `HandleBotStuck`/`ForceTeleport`/`IsPlayerInBg`/`IsBgFinished`, second `LogToFile` overload |
| `BloogBot/AI/SharedStates/GrindState.cs` | 5 | Whole wander-node/waypoint-blacklist/forced-wp-path body (`HandleWpSelection` and helpers), Z-delta target gate, non-readonly `player` plus `isInBg`/`playerLevel` |
| `BloogBot/Game/Position.cs` | 5 | 8-arg `[JsonConstructor]`, `ID`/`Zone`/`MinLevel`/`MaxLevel`/`Links`, `ToStringFull`, `ZoneIdNameDict`, `GetZoneName` |
| `BloogBot/AI/SharedStates/CombatStateBase.cs` | 5 | `DeathsAtWp` link-teleport, `loopTimer`/`lastTargetHealth` unstick loop, npcbot-friend and dead-player bail-out |
| `BloogBot/AI/SharedStates/MoveToCorpseState.cs` | 4 | Whole rewritten `Update` body, `HasReachedWpCloseToCorpse`, `ForcedWpPathToCorpse` (BFS over waypoint links) |
| `BloogBot/Game/Objects/WoWObject.cs` | 4 | Three suppressed access-violation logs, null-pointer name guard |
| `FrostMageBot/RestState.cs` | 4 | `FoodNames`/`DrinkNames` lookup, party-aware combat/drink/mana conditions dropped |
| `BloogBot/AI/DependencyContainer.cs` | 3 | `FindThreat` rewrite, `FindClosestTarget` rewrite plus `CanAttackTarget`/`GetHotspotById`, map-id-driven `GetCurrentHotspot` |
| `BloogBot/AI/SharedStates/RetrieveCorpseState.cs` | 3 | `resDistance` 25 vs 30, ghost-form bail-out and `WpStuckCount` reset, res-location log |
| `BloogBot/AI/SharedStates/LootState.cs` | 2 | Loot-index bounds check, epics/coins-only looting |
| `BloogBot/AI/SharedStates/MoveToHotspotWaypointState.cs` | 2 | Casting bail-out, Z-delta + `WpStuckCount` pop condition |
| `BloogBot/AI/SharedStates/MoveToPositionState.cs` | 2 | Distance 5 / stuck 15 thresholds and the ghost-form early pop (2D and 3D branches) |
| `BloogBot/AI/SharedStates/ReleaseCorpseState.cs` | 2 | Health-aware release branch, long BG/arena release delay |
| `BloogBot/AI/SharedStates/StuckState.cs` | 2 | `WpStuckCount`-scaled unstick distance and move time |
| `BloogBot/Navigation.cs` | 2 | Suppressed 'Problem building path' log, `rand` field |
| `FrostMageBot/ConjureItemsState.cs` | 2 | `FoodNames`/`DrinkNames` lookup, party-aware rest condition dropped |
| `ShadowPriestBot/CombatState.cs` | 2 | Wand action slot 12 vs 11, wand-use condition |
| `ArmsWarriorBot/CombatState.cs` | 1 | `ObjectManager.Aggressors.ToList()` |
| `BloogBot/AI/SharedStates/ArenaSkirmishQueueState.cs` | 1 | Fork-only file, wrapped whole |
| `BloogBot/AI/SharedStates/BattlegroundQueueState.cs` | 1 | Fork-only file, wrapped whole |
| `BloogBot/AI/StuckHelper.cs` | 1 | `WpStuckCount` increment and log |
| `BloogBot/Game/ObjectManager.cs` | 1 | Party-member aggro dropped from `Aggressors` |
| `BloogBot/Game/Objects/LocalPlayer.cs` | 1 | One 230-line block of fork-only player state (zone/waypoint tracking, BG flags, level item/spell/talent tables, `BotFriend`, blacklists) |
| `BloogBot/MemoryManager.cs` | 1 | Null-buffer guard in `ReadStruct` |
| `BloogBot/WardenDisabler.cs` | 1 | `useWarden` flag wrapping the whole detour install |
| `Loader/dllmain.cpp` | 1 | `skipDebug` flag around the `#if _DEBUG` attach-a-debugger wait (uses `#ifdef`, fed by `CustomChangesDefine` in `Loader.vcxproj`) |

## Unguarded customizations

No preprocessor available - hand-merge these when upstream touches them.

- `Sql/clean.sql`, `Sql/npcs.sql`, `Sql/wander_nodes_{bg,ek,kalimdor,northrend,outland}.sql` -
  fork-only files (wander-node data and NPC seeding). Upstream never touches them; no conflict risk.
- `BloogBot/botSettings.json`, `BloogBot/bootstrapperSettings.json` - fork's own bot profile,
  hotspots and paths.
- `BloogBot/App.config`, `BloogBotTests/app.config`, `Bootstrapper/App.config` - assembly binding
  redirects. **Upstream does touch these** and adds new per-bot `app.config` files. Expect hand-merges.
- `README.md`, `.gitignore`, `bloogbot_diff.ps1`, `.claude/skills/bloogbot-upstream-sync/SKILL.md`.

Deliberately not guarded, per the skill's do-not-guard list:

- `TargetFrameworkVersion` v4.6.1 -> v4.8 plus `<TargetFrameworkProfile />` in every csproj. Upstream
  `608ab7f` ('Build settings') makes the same retarget; guarding it would create a phantom conflict.
- `FastCall/FastCall.vcxproj` platform-toolset change.
- `BloogBot/Properties/Resources.Designer.cs`, `Settings.Designer.cs` - designer-regenerated churn.
  These are the only two code files whose diff against the fork point still contains deletions.
- Three dead `using System;` additions (`ArcaneMageBot/ConjureItemsState.cs`,
  `BloogBot/AI/SharedStates/EquipArmorState.cs`, `FrostMageBot/BuffSelfState.cs`), the unused
  `using System.ComponentModel;` in `LocalPlayer.cs`/`DependencyContainer.cs`, the unused
  `using BloogBot.AI;` in `Navigation.cs`, and a commented-out `Health` property experiment in
  `WoWUnit.cs`. All non-behavioral in both configurations. **`ArcaneMageBot` therefore needs no
  toggle** - its only fork change is one of those dead usings.

## Open questions

None. Setup landed no upstream commits; the next run starts at `a5e450a`.
