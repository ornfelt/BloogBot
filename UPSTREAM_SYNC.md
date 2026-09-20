# Upstream sync ledger

Upstream: <https://github.com/DrewKestell/BloogBot> branch `main`, cloned at
`$USERPROFILE/Downloads/BloogBot`, wired into this repo as the `upstream-local` remote.
Fork point: `1d0057c` - Merge branch 'main' of github.com:DrewKestell/BloogBot into main
High-water mark: `452b3fb` (every commit up to and including this one is decided)
Upstream history is **not linear** at the start of this range: `a5e450a` and `569d3cb` both branch
directly off the fork point and rejoin at the merge `f22af34`. So while those two were the
high-water mark, `rev-list <mark>..upstream-local/main` over-counted by one (it still listed the
parallel sibling). From `f22af34` on the history is linear - no further merge commits in the
range - and the count is exact again.
Upstream HEAD when last checked: `a9be5e8` `BeastmasterHunterBot: LOS checks, pet management, rest state rewrite` (2026-09-20)
Remaining after the high-water mark: `57`
Local customizations: guarded by `USE_CUSTOM_CHANGES`, defined in `BloogBot/BloogBot.csproj`,
`FrostMageBot/FrostMageBot.csproj` and `Loader/Loader.vcxproj`. `ArmsWarriorBot` and
`ShadowPriestBot` had their toggles removed after `569d3cb` left them with no guards.
Branch: `main`. The `commented` branch is out of scope.

Statuses: `applied` (cherry-picked clean), `adapted` (landed, conflicts resolved by hand),
`partial` (part landed, part dropped - the notes say which), `skipped` (deliberately not landed),
`asked` (waiting on an answer from the user, cherry-pick left in progress or aborted).

Every commit that touched a guarded file also carries a mirror verdict in `Notes`, one of
`mirror pending` (asked, unanswered - blocks the next commit), `mirrored`, `mirror declined: <reason>`
or `mirror n/a: <reason>`. It answers: did upstream fix a bug that the `#if USE_CUSTOM_CHANGES`
branch has too? A cherry-pick only ever patches the `#else` half, so without this the fix lands in
the configuration nobody runs. A commit with a pending mirror keeps its real status - the commit
landed; only the mirror waits.

| # | Commit | Subject | Status | Conflicts | Notes |
| --- | --- | --- | --- | --- | --- |
| - | `1d0057c` | (fork point - Phase 0 setup) | applied | - | XML doc comments stripped, all customizations guarded, toggles wired, both builds green. No upstream commit landed. |
| 1 | `a5e450a` | More information when building path | adapted | `BloogBot/Navigation.cs` | Upstream expanded the 'Problem building path' log with the mapId and an mmaps hint. Took it into the `#else` branch verbatim; the `#if` branch suppresses that log entirely and was left alone. mirror n/a: the customization removes the log, so a better message has no subject in the `#if` branch - a diagnostic improvement, not a correction. |
| 2 | `569d3cb` | Perf fixes | adapted | `ArmsWarriorBot/CombatState.cs`, `BloogBot/AI/Bot.cs`, `BloogBot/AI/DependencyContainer.cs`, `BloogBot/Game/ObjectManager.cs`, `FrostMageBot/ConjureItemsState.cs`, `FrostMageBot/RestState.cs`, `ShadowPriestBot/CombatState.cs` (all 7) | Upstream's text went into the `#else` branch in every file; no `#if` branch was touched. Verified by preprocessing the off-configuration of all 7 files and diffing against upstream `569d3cb` - identical apart from the known unguarded dead `using` lines. `ObjectManager.IsGrouped` was deleted by upstream and its four surviving references all sat in `#else` branches that the same commit rewrote, so the off-build still resolves. `Bot.cs`: the 25 -> 50 delay hit the guarded call site in `Start` (`#if` keeps 100); the unguarded one in `StartPowerlevel` is 25 upstream and here. `DependencyContainer.cs`: upstream's rewritten `FindThreat`/`FindClosestTarget` replaced the old single-expression forms in `#else`. mirror n/a (all 7): upstream converged on changes the `#if` branch already had - `.ToList()`, wand slot 12, the wand condition, mana<=70, the party-aware rest/drink drops, party members out of `Aggressors`, and the straight-line target ordering - except `Bot.cs`, where 50 is a tuning value against the fork's deliberate 100. Left 8 guards whose `#if` and `#else` were identical; dropped in the follow-up commit below. |
| - | (cleanup) | Drop the 8 guards upstream `569d3cb` made redundant | applied | - | Not an upstream commit. The 8 guard regions whose two branches had become identical were collapsed to the shared line: `ArmsWarriorBot/CombatState.cs`, `BloogBot/Game/ObjectManager.cs`, `FrostMageBot/ConjureItemsState.cs`, `FrostMageBot/RestState.cs` (3), `ShadowPriestBot/CombatState.cs` (2). Verified behaviour-neutral: the preprocessed on- and off-views of all five files are byte-identical to before. `ArmsWarriorBot` and `ShadowPriestBot` were left with no guards, so their csproj toggles were removed. Guard regions 76 -> 68. Both full rebuilds green. |
| 3 | `f22af34` | Merge branch 'main' of github.com:DrewKestell/BloogBot into main | applied | `BloogBot/Navigation.cs` | Empty - contributed nothing. The one merge commit in the range, picked with `-m 1`. Its first-parent diff is exactly `a5e450a`'s Navigation.cs change and its second-parent diff is exactly `569d3cb`, so the merge resolved nothing of its own; both sides were already landed. It conflicted rather than reporting itself empty only because the fork's guard structure means the merge base does not line up - the incoming text was character-identical to what the `#else` branch already held. Resolved to ours (zero net change, confirmed with `git diff HEAD`) and finished with `git cherry-pick --skip`. No commit created, tree untouched. mirror n/a: no content to mirror. |
| 4 | `452b3fb` | Separate npc types in select list to make the ui less busy | applied | - | Clean cherry-pick, no conflicts. Splits the single `Npcs` collection in `BloogBot/UI/MainViewModel.cs` into `RepairNpcs`/`InkeeperNpcs`/`AmmoNpcs` and repoints the three `MainWindow.xaml` combo boxes at them. Touched no guarded file and neither file has ever diverged from the fork point, so nothing to reconcile. mirror n/a: no guarded file touched. |

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

None. The next run starts at `fe3533f` ('Reload NPCs').
