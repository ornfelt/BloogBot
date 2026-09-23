# Database setup

BloogBot keeps its own data - hotspots, NPCs, travel paths, gather routes, kill and quest tracking -
in a database of its own. This is **not** the WoW server's database and has nothing to do with the
realm you connect to.

Which one it uses comes from `botSettings.json`:

| `DatabaseType` | Store | Schema run at start-up | `DatabasePath` |
| --- | --- | --- | --- |
| `sqlite` (default) | `db.db` beside `BloogBot.dll` | `SqliteSchema.SQL` | **ignored** - `SqliteRepository.Initialize` discards it |
| `mssql` | whatever `DatabasePath` points at | `TSqlSchema.SQL` | used as the connection string |

Anything else throws `NotImplementedException` from `Repository.Initialize`.

## You normally do not have to do anything

On every start `Repository.Initialize` runs the schema for the configured store, and both schema
files are written with `CREATE TABLE IF NOT EXISTS`, so running them repeatedly is safe. For SQLite
it also creates `db.db` first if the file is missing. So the database is created and kept up to date
by simply running the bot.

That matters for an **existing** `db.db` made by an older build: because the schema is re-run every
start, tables added since are created on the spot. This used to be skipped whenever `db.db` already
existed, which is what produced

```text
SQL logic error
no such table: GatherRoutes
```

from `MainViewModel`'s constructor, before the window could open.

The rest of this file is for when you want to do it by hand: inspecting, reseeding, or starting over.

## Where the database lives

`db.db` sits next to `BloogBot.dll`, which means the build output folder, not the project folder:

| Configuration | Path |
| --- | --- |
| Debug | `Bot\db.db` |
| Release | `Bot\Release\db.db` |

The two are independent. `SqliteSchema.SQL` and `TSqlSchema.SQL` are copied into the same folders,
so the commands below work from the repository root.

## SQLite by hand

These need the `sqlite3` CLI, which is not part of the repo - grab the "command-line tools" bundle
from [sqlite.org/download.html](https://sqlite.org/download.html) and put `sqlite3` on `PATH`. The
bot itself does not need it; it uses `System.Data.SQLite`.

### Create the database and apply the schema

`sqlite3` creates the file on first use, so one command covers both create-if-missing and
apply-schema. Re-running it is safe.

```powershell
# PowerShell - from the repository root
Get-Content Bot\SqliteSchema.SQL -Raw | sqlite3 Bot\db.db
```

```bash
# bash / Linux / Git Bash - from the repository root
sqlite3 Bot/db.db < Bot/SqliteSchema.SQL
```

For a Release layout, swap `Bot` for `Bot/Release`.

### Check what you have

```powershell
sqlite3 Bot\db.db ".tables"
sqlite3 Bot\db.db "SELECT COUNT(*) FROM Hotspots;"
```

```bash
sqlite3 Bot/db.db ".tables"
sqlite3 Bot/db.db "SELECT COUNT(*) FROM Hotspots;"
```

A complete schema has these 15 tables, plus SQLite's internal `sqlite_sequence`:

```text
BlacklistedMobs  Commands   GatherRoutes  Hotspots      HotspotsV2
Kills            Npcs       Quests        QuestHubs     QuestCompletions
QuestObjectives  QuestPrerequisites       ReportSignatures  Towns  TravelPaths
```

### Start over

`db.db` holds everything you have recorded in the UI, so copy it first if you care about it.

```powershell
if (Test-Path Bot\db.db) { Copy-Item Bot\db.db Bot\db.db.bak -Force; Remove-Item Bot\db.db }
Get-Content Bot\SqliteSchema.SQL -Raw | sqlite3 Bot\db.db
```

```bash
[ -f Bot/db.db ] && cp Bot/db.db Bot/db.db.bak && rm Bot/db.db
sqlite3 Bot/db.db < Bot/SqliteSchema.SQL
```

Or just delete `db.db` and start the bot - it rebuilds an empty one.

## Seed data - `Sql\`

The scripts in `Sql\` are **data**, not schema. Apply them **after** the schema exists.

**On a default build they are not optional.** With `USE_CUSTOM_CHANGES` on - the default -
`DependencyContainer.GetCurrentHotspot()` ignores `GrindingHotspotId` on every known map and looks
up a **hard-coded hotspot id** from the map and your faction:

| Map | Horde | Alliance | | Map | Id |
| --- | --- | --- | --- | --- | --- |
| Kalimdor (1) | 1 | 2 | | Warsong Gulch (489) | 9 |
| Eastern Kingdoms (0) | 3 | 4 | | Arathi Basin (529) | 10 |
| Outland (530) | 5 | 6 | | Alterac Valley (30) | 11 |
| Northrend (571) | 7 | 8 | | Nagrand Arena (559) | 12 |

Those are exactly the ids the `wander_nodes_*` scripts create. Without them `GetCurrentHotspot()`
returns null and `GrindState` throws `NullReferenceException` once per tick - the bot runs, but
never picks a waypoint. Only an unrecognised map falls back to `GrindingHotspotId`.

Build with `-p:UseCustomChanges=false` and the upstream `GetCurrentHotspot()` is used instead, which
always honours `GrindingHotspotId`; then the seed data really is optional and you can record your
own hotspots in the UI.

| Script | Table | What it does |
| --- | --- | --- |
| `npcs.sql` | `Npcs` | clears `Npcs` and inserts a set of innkeepers, repair and ammo vendors |
| `wander_nodes_ek.sql` | `Hotspots` | replaces hotspot IDs 3-4, Eastern Kingdoms |
| `wander_nodes_kalimdor.sql` | `Hotspots` | Kalimdor |
| `wander_nodes_outland.sql` | `Hotspots` | replaces IDs 5-6, Outland |
| `wander_nodes_northrend.sql` | `Hotspots` | replaces IDs 7-8, Northrend |
| `wander_nodes_bg.sql` | `Hotspots` | replaces IDs 9-12, battlegrounds and arenas |
| `clean.sql` | `Hotspots` | **destructive helper** - deletes hotspot IDs 1-4 and nothing else |

Each `wander_nodes_*` script deletes its own ID range before inserting, so re-running one is safe and
they do not tread on each other. `clean.sql` is a manual tidy-up, not part of setup - read it before
running it.

```powershell
# PowerShell - schema first, then the seed data, skipping clean.sql
Get-Content Bot\SqliteSchema.SQL -Raw | sqlite3 Bot\db.db
Get-ChildItem Sql\*.sql -Exclude clean.sql | Sort-Object Name | ForEach-Object {
    Write-Host "applying $($_.Name)"
    Get-Content $_.FullName -Raw | sqlite3 Bot\db.db
}
```

```bash
# bash - same thing
sqlite3 Bot/db.db < Bot/SqliteSchema.SQL
for f in $(ls Sql/*.sql | grep -v clean.sql | sort); do
    echo "applying $f"
    sqlite3 Bot/db.db < "$f"
done
```

## SQL Server / Azure SQL

Set `DatabaseType` to `mssql` and put a real connection string in `DatabasePath`. `TSqlRepository`
then runs `TSqlSchema.SQL` on every start, so the tables are created for you against whatever
database the connection string names - you only need the database itself to exist.

To apply it by hand you need `sqlcmd`
([install guide](https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility)):

```powershell
sqlcmd -S localhost -d BloogBot -i Bot\TSqlSchema.SQL          # Windows auth
sqlcmd -S myserver.database.windows.net -d BloogBot -U myuser -P mypassword -i Bot\TSqlSchema.SQL
```

```bash
sqlcmd -S localhost -d BloogBot -U sa -P 'yourStrong(!)Password' -i Bot/TSqlSchema.SQL
```

`SqlSchema.SQL` in the repository root is the original upstream Azure SQL schema, kept for
reference - full `CREATE TABLE [dbo].[...]` statements with `GO` batch separators. `TSqlSchema.SQL`
is the one the bot actually runs; prefer it.

The `Sql\` seed scripts are written for SQLite - backtick-quoted identifiers and all - so they will
not run against SQL Server unchanged.

## Troubleshooting

| Symptom | Cause |
| --- | --- |
| `NullReferenceException` in `GrindState.HandleWpSelection` | no hotspot for your map and faction. The default build needs hotspot ids 1-12 from the `Sql\` scripts - see above |
| `no such table: <name>` | a `db.db` older than the schema. Fixed automatically now, since the schema runs every start; by hand, apply `SqliteSchema.SQL` as above |
| `NotImplementedException` from `Repository.Initialize` | `DatabaseType` is neither `sqlite` nor `mssql` |
| SQLite errors although `DatabaseType` is `mssql` | the stale Azure connection string that ships in `DatabasePath` is ignored under `sqlite`, but under `mssql` it is used - replace it |
| `db.db` keeps coming back empty | you are looking at the wrong configuration's folder: Debug writes `Bot\`, Release writes `Bot\Release\` |
| `sqlite3` not recognised | the CLI is not on `PATH`; the bot does not need it, only these manual commands do |
| query output looks different from the examples | `sqlite3` reads `~/.sqliterc` at start-up, so a local one can turn on headers, column mode or a different separator. Harmless - pass `-batch` or `-noheader` if you want the bare value |
