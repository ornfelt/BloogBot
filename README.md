# BloogBot in .net 9

This directory is the **.NET 9 port of BloogBot**: the same files, namespaces, types, members and
behavior as the .NET Framework 4.8 tree at `Code2/C#/BloogBot`, the same `botSettings.json` /
`bootstrapperSettings.json`, the same SQLite / T-SQL schemas and the same injected-bot workflow.
An existing BloogBot install can be pointed at this build and behave identically.

Two things are deliberately not a straight copy:

1. **The UI** - the WPF window is split into a UI-agnostic abstraction plus two shells, WPF
   (default) and Avalonia, with the same controls, bindings and behavior, dark mode by default and
   a light toggle in both.
2. **The native loader** - `Loader/dllmain.cpp` hosts the runtime through `nethost` / `hostfxr`
   instead of the .NET Framework-only `ICLRRuntimeHost`.

Everything else is a replica. The source tree at `Code2/C#/BloogBot` stays on .NET Framework, stays
the reference for every later port run, and keeps receiving upstream syncs from
`DrewKestell/BloogBot`; those are carried across into this tree by the `bloogbot-net9-port` skill.
`PORT_STATUS.md` records where the port stands.

## Projects

| Project | TFM | Output | Notes |
| --- | --- | --- | --- |
| `BloogBot` | `net9.0-windows` | `Library` | everything except `UI/`. `EnableDynamicLoading=true` so a `.runtimeconfig.json` is emitted for `hostfxr` |
| `BloogBot.UI.Abstractions` | `net9.0` | `Library` | contracts + MVVM primitives. Plain `net9.0` on purpose - a WPF type here will not compile |
| `BloogBot.UI.Core` | `net9.0` | `Library` | `MainViewModel`, `CommandHandler`, `BotService`. Same reason |
| `BloogBot.UI.Wpf` | `net9.0-windows` | `Library` | `UseWPF=true`. The default shell |
| `BloogBot.UI.Avalonia` | `net9.0-windows` | `Library` | the alternate shell |
| `Bootstrapper` | `net9.0-windows` | `Exe` | starts `WoW.exe` and injects `Loader.dll` |
| 17 bot plugins | `net9.0-windows` | `Library` | one per spec, names unchanged (`TestBot` lives in `TestBot.cs/`, as in the original) |
| `BloogBotTests` | `net9.0-windows` | `Library` | MSTest 3.x |
| `Fasm.NET` | `net9.0-windows` | `Library` | **new**: a managed `Binarysharp.Assemblers.Fasm.FasmNet` over the stock `FASM.DLL`, replacing the prebuilt net461 mixed-mode `Fasm.NET.dll` |
| `Loader`, `FastCall`, `Navigation`, `NavigationTests` | - | native | `vcxproj`, unchanged layout |

Everything managed is **x86** (`PlatformTarget=x86`, RID `win-x86`), because the WoW clients
BloogBot injects into are 32-bit. Every managed project writes to `Bot\` in Debug and
`Bot\Release\` in Release, exactly like the original solution.

## Building

```powershell
cd $env:code_root_dir/Code2/C#/BloogBot_net9
dotnet build BloogBot_net9.sln -c Debug
dotnet build BloogBot_net9.sln -c Debug -p:Ui=Avalonia
dotnet build BloogBot_net9.sln -c Debug -p:UseCustomChanges=false
dotnet build BloogBot_net9.sln -c Debug -p:Ui=Avalonia -p:UseCustomChanges=false
```

- `-p:Ui=Wpf` (default) or `-p:Ui=Avalonia` selects which shell `BloogBot` references and which of
  `UI_WPF` / `UI_AVALONIA` is defined. Both shells must always build and stay at parity.
- `-p:UseCustomChanges=true` (default) defines `USE_CUSTOM_CHANGES`, the fork's local
  customizations on top of upstream BloogBot; `false` builds the tree as upstream wrote it. A C#
  `#if` branch that is off is not compiled, so both settings have to build.

Both switches live in `Directory.Build.props`.

The native projects are built separately:

```powershell
msbuild Loader\Loader.vcxproj /p:Configuration=Debug /p:Platform=Win32
msbuild FastCall\FastCall.vcxproj /p:Configuration=Debug /p:Platform=Win32
msbuild Navigation\Navigation.vcxproj /p:Configuration=Debug /p:Platform=Win32
```

`Loader` compiles against `nethost.h`, `hostfxr.h` and `coreclr_delegates.h` and links
`nethost.lib` from the .NET 9 **x86 host pack**, which the .NET 9 SDK installs at
`C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Host.win-x86\9.*\runtimes\win-x86\native`.
`Loader.vcxproj` finds that pack itself; pass `/p:NetHostDir=<dir containing nethost.h>` to point
it somewhere else. `nethost.lib` is an import library, so the build also copies `nethost.dll` into
the output folder next to `Loader.dll`. `FastCall`, `Navigation` and `NavigationTests` are
unchanged from the .NET Framework tree and need nothing beyond the v143 toolset.

## Running it

There is exactly one executable in the solution: **`Bootstrapper.exe`**. Every other managed
project is a `Library`, `BloogBot` included - under .NET Framework it was a `WinExe`, but `hostfxr`
loads an assembly rather than running one, so the bot is a DLL that lives inside `WoW.exe`. Trying
to start `BloogBot`, a shell or a bot plugin will not work; none of them has an entry point.

### Where the exe ends up

Every managed project and all four native projects write into **one flat folder**, which is what
lets `Bootstrapper.exe` find `Loader.dll` beside itself and lets `hostfxr` find `BloogBot.dll` and
its `runtimeconfig.json`:

| Configuration | Output folder | Set in |
| --- | --- | --- |
| Debug | `Bot\` | `Directory.Build.props` (managed), each `.vcxproj` (native) |
| Release | `Bot\Release\` | same |

There is no `bin\Debug\net9.0-windows\win-x86` anywhere - `AppendTargetFrameworkToOutputPath`
and `AppendRuntimeIdentifierToOutputPath` are both off, mirroring the original solution's layout so
an existing install can be pointed at the new build unchanged.

A complete folder holds `Bootstrapper.exe`, `BloogBot.dll` + `BloogBot.runtimeconfig.json`, the two
shells, the 17 plugin DLLs, `Loader.dll` + `nethost.dll`, `Navigation.dll`, `FastCall.dll`, both
settings files and both schema files.

### From the command line

```powershell
dotnet build BloogBot_net9.sln -c Debug
.\Bot\Bootstrapper.exe
```

Release is the same with `-c Release`, run from `Bot\Release\`. `dotnet run --project
Bootstrapper\Bootstrapper.csproj -c Debug` is equivalent to launching the exe directly.

`dotnet build` does **not** build the native projects - the .NET CLI cannot build `.vcxproj`. They
are built once with msbuild from a Developer Command Prompt, and only need rebuilding when their
sources change:

```powershell
msbuild Loader\Loader.vcxproj /p:Configuration=Debug /p:Platform=Win32
msbuild FastCall\FastCall.vcxproj /p:Configuration=Debug /p:Platform=Win32
msbuild Navigation\Navigation.vcxproj /p:Configuration=Debug /p:Platform=Win32
```

Without `Loader.dll` nothing is injected at all; without `Navigation.dll` and `FastCall.dll` the
bot window opens but pathing and the game-function trampolines fail.

### From Visual Studio

1. Open `BloogBot_net9.sln`.
2. Set the solution platform to **x86**. It is the only one that exists - the solution defines
   `Debug|x86` and `Release|x86` and nothing else - so there is no `Any CPU` to pick by mistake.
3. Right-click `Bootstrapper` -> **Set as Startup Project**.
4. F5.

**The four native projects are not in `BloogBot_net9.sln`** - it holds the 25 managed projects
only. `Loader`, `FastCall`, `Navigation` and `NavigationTests` still exist as `.vcxproj` files in
the tree and still build, but you have to build them with msbuild as above before the first F5, or
`Bootstrapper.exe` will start WoW and then fail to inject because `Loader.dll` is not in the output
folder. The original `BloogBot.sln` did include them; see "Deviations from the original" in
`PORT_STATUS.md`.

### Settings and database

Both settings files are read from the folder the assembly that reads them sits in - that is `Bot\`
or `Bot\Release\`, not the project directory. Edit the copies under `BloogBot\` and
`Bootstrapper\`; they are copied to the output on build (`PreserveNewest`), so editing the output
copy directly works until the next build overwrites it.

| File | Read by | Required |
| --- | --- | --- |
| `bootstrapperSettings.json` | `Bootstrapper.exe` at startup | **yes** - one key, `PathToWoW` |
| `botSettings.json` | `MainViewModel`'s constructor, so as the UI opens | **yes** - the whole bot config |
| `SqliteSchema.SQL` | `SqliteRepository` on first run | yes, when `DatabaseType` is `sqlite` |
| `TSqlSchema.SQL` | `TSqlRepository` on **every** start | yes, when `DatabaseType` is `mssql` |
| `db.db` | `SqliteRepository` | created automatically - see below |
| `mmaps\` | `Navigation.dll` | for pathfinding; generate them yourself, see the FAQ below |
| `FASM.DLL` | the `Fasm.NET` shim, lazily | only if something calls `MemoryManager.InjectAssembly`. Nothing does in the stock configuration - but a `-p:UseCustomChanges=false` build takes the upstream branch of `WardenDisabler.Initialize`, which **does** call it, so such a build needs FASM.DLL or it throws `DllNotFoundException` before the window opens |

The database needs no setup in the default configuration. `DatabaseType` is `sqlite`, and
`SqliteRepository.Initialize` **ignores the connection string it is handed**: it creates `db.db`
next to `BloogBot.dll` on first run and executes `SqliteSchema.SQL` against it. So the Azure SQL
connection string that ships in `DatabasePath` is dead config while `DatabaseType` is `sqlite` -
leave it, or replace it, nothing reads it.

Set `DatabaseType` to `mssql` and it is the other way round: `DatabasePath` is used as the
connection string, and `TSqlSchema.SQL` is executed on every start (the script checks for each
table before creating it). Any other value throws `NotImplementedException` from
`Repository.Initialize`.

Discord is off out of the box (`DiscordBotEnabled: false`), so the token and the three ID keys can
stay as they are.

### You do not start the UI yourself

The bot window opens on its own once injection succeeds. Nothing is launched by hand after
`Bootstrapper.exe`, and there is no second executable to run:

| # | What happens | Where |
| --- | --- | --- |
| 1 | `Bootstrapper.exe` starts `WoW.exe` suspended-ish, writes the path to `Loader.dll` into it and `CreateRemoteThread`s at `LoadLibraryW` | `Bootstrapper/Program.cs` |
| 2 | WoW loads `Loader.dll`, which opens a console window and resolves `hostfxr` through `nethost` | `Loader/dllmain.cpp` |
| 3 | `hostfxr` reads `BloogBot.runtimeconfig.json`, starts .NET 9 **inside the WoW process** and calls `BloogBot.Loader.Load` | `Loader/dllmain.cpp` |
| 4 | `Load` starts an STA thread on the selected shell's `App.Main`, found by reflection | `BloogBot/Loader.cs` |
| 5 | `App.OnStartup` calls `WardenDisabler.Initialize()`, constructs `MainWindow` and shows it | `BloogBot.UI.Wpf/App.xaml.cs` |

So the window is a child of `WoW.exe`, not a separate process - it will not appear in the taskbar
as its own app, and closing it calls `Environment.Exit(0)`, which takes the game client down with
it.

### In a Debug build the window is gated twice

Both gates are inherited from the original and are absent from Release builds:

1. **A 10-second native wait.** `Loader.dll` prints `Attach a debugger now to WoW.exe if you want
   to debug Loader.dll. Waiting 10 seconds...` to the console it just allocated, and blocks. Wait
   it out or attach Visual Studio to `WoW.exe`.
   With `USE_CUSTOM_CHANGES` on - the default - the console *also* prints
   `Skipping attaching debugger...` immediately before it. That message is misleading: the flag it
   reports on is `int skipDebug = 0`, so the wait still happens. Ported as-is from the original.
2. **A managed `Debugger.Launch()`.** `App.OnStartup` opens the Windows JIT-debugger dialog before
   anything else. **The UI does not appear until you answer it** - attach a debugger, or dismiss
   the dialog to carry on without one.

A Release build (`-c Release`, output in `Bot\Release\`) has neither, and the window comes up
directly.

### Which shell you get

`BloogBot.UI.Wpf.dll` and `BloogBot.UI.Avalonia.dll` can both sit in `Bot\` at once; the one that
starts is whichever `UI_WPF` / `UI_AVALONIA` constant was compiled into `BloogBot.dll`. Rebuild
with `-p:Ui=Avalonia` to switch, then run `Bootstrapper.exe` again.

### When nothing happens

- **`Access is denied.` from `Process.get_Handle` at `Program.cs:41`** - this almost always means
  `PathToWoW` is wrong, not that you need elevation. `CreateProcess`'s return value is never
  checked, so a bad path leaves `dwProcessId` at 0, and `Process.GetProcessById(0)` is the System
  Idle Process, which cannot be opened. Check the path first; only if it is correct does this mean
  WoW is running at a higher integrity level than `Bootstrapper.exe`, which elevation fixes.
- **A message box from `Loader.dll`** - `Could not locate hostfxr`, `Invalid runtimeconfig.json`
  and friends come from the native loader and name the step that failed. See
  "Runtime requirements for injection" below.
- **A console window but no bot window** - you are in a Debug build and have not answered the
  `Debugger.Launch()` dialog yet.
- **The bot window opens but navigation fails** - movemaps are missing; they belong in `Bot\mmaps`.

## Runtime requirements for injection

`Loader.dll` resolves `hostfxr` through `nethost` for the bitness of the process it is loaded into,
and `WoW.exe` is 32-bit. Injection therefore needs the **x86 .NET 9 Desktop Runtime** installed
under `C:\Program Files (x86)\dotnet\` (`Microsoft.NETCore.App` and `Microsoft.WindowsDesktop.App`,
9.0.x). A 64-bit-only .NET install builds the solution fine but cannot host inside `WoW.exe`.
A self-contained x86 publish with `hostfxr.dll` beside `Loader.dll` is the fallback if you would
rather not install a runtime.

The original referenced a prebuilt `Fasm.NET.dll`, Binarysharp's mixed-mode C++/CLI wrapper around
the flat assembler, built for `net461`. Mixed-mode assemblies built against .NET Framework cannot
load on .NET 9, so the `Fasm.NET` project here exposes the same namespace and members and
P/Invokes the assembler instead. Put the **32-bit `FASM.DLL`** from
[flatassembler.net](https://flatassembler.net) in the `Bot\` folder next to `BloogBot.dll`. It is
loaded lazily on the first assemble, so the solution builds without it, and so does any run that
never calls `MemoryManager.InjectAssembly` - which in the default configuration is every run, since
`SignalEventManager`'s hooks are commented out and `WardenDisabler.Initialize` sits behind
`useWarden = false`.

## Potential bugs in the original

Where the original code looks like a real defect it is ported unchanged and tagged, so the two
trees stay comparable:

```bash
grep -rn "POTENTIAL BUG FOUND" .
```

Each tag says what looks wrong, names the original `file:line`, and confirms the code was ported
as-is. `PORT_STATUS.md` keeps the same list as a table.

---

## Original BloogBot README


Join the [BloogBot Discord Server](https://discord.gg/YfNqMgfFBh) to chat with other folks hacking on BloogBot!

BloogBot is an in-process bot for the Vanilla (v 1.12.1), Burning Crusade (v 2.4.3), and Wrath of the Lich King (v 3.3.5) clients.

I have written extensively about the project [on my website](https://drewkestell.us/Article/6/Chapter/1).

*IMPORTANT NOTE*: Due to implementation differences between the various WoW server emulators out there (MaNGOS, TrinityCore, AzerothCore, etc), I can't promise the bot will work consistently across all servers. As of 12/3/2022, I've successfully tested the bot against the following servers:

- Kronos (Vanilla 1.12.1)
- TurtleWoW (Vanilla 1.12.1)
- Atlantiss (TBC 2.4.3)
- Warmane (WotLK 3.3.5)

A few more important notes:
- This is a hobby project, and as such, the quality of the code is as you'd expect. If you find bugs, fix 'em (and submit a PR)!
- This does **NOT WORK ON RETAIL**. As mentioned in the writing on my website, the purpose of this project is intellectual exploration, not exploitation. I have no interest in monetizing the bot for current versions of the WoW client. And in fact, Blizzard's anticheat has likely gotten so sophisticated that it's beyond my technical ability. So this bot will only work on the old versions of the WoW client. It'll work on the various Vanilla/TBC/WotLK private servers out there, or on a MaNGOS install you set up yourself.
- I used to have two completely separate code bases - one for v1.12.1, and one for v2.4.3 of the WoW client. I recently merged them into a single codebase. TBC should be fairly stable. Vanilla and WotLK have been tested with a few basic scenarios using a few class profiles, but there are likely bugs that need to be fixed.
- There are a few external dependencies that you'll need to wire up if you want to compile and run this yourself:
  - You'll need to compile movemaps to facilitate the bot's navigation through the game world. See [this article from my website](https://drewkestell.us/Article/6/Chapter/20) for more info
  - You'll need to modify some values in `botSettings.json`. Note that you can disable Discord integration by setting `"DiscordBotEnabled"` to `false` in botSettings.json.
    - DatabasePath
    - DiscordBotToken
    - DiscordGuildId
    - DiscordRoleId
    - DiscordChannelId
- The code, in its current state, depends on an Azure SQL database with a few tables created. Sorry, but you can't connect to mine. So to run this yourself, you'll have to either disable/modify that code, or create the required Azure infrastructure yourself. `Repository.cs` is a good place to start if you want to understand which tables need to be created (and their schemas). Eventually, I want to create an [ARM Template](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/overview) to simplify deploying all the required cloud infrastructure, but it doesn't exist yet. In the meantime, you can reference SqlSchema.SQL in the repo root to see the schema for all the tables you'll need.'
- All of the bot profiles (ie: FrostMageBot, etc) should _mostly_ work. But due to emulation inconsistencies across the various private servers out there, you may run into issues. I suggest creating your own bot profile and experimenting with creating your own combat rotation yourself.
- The Vanilla implementation assumes you have the Auto-Attack spell in your far right spot on your first, default action bar.

## Getting Started

- To get started working with this yourself:
  - Clone the repo and open BloogBot.sln in Visual Studio (v2022 ideally)
  - Build the solution - if you get compiler errors, you're likely missing some SDK / framework dependencies. Check the errors, consult Google, and use the Visual Studio installer to install any missing dependencies. For example, you'll definitely need some C++ Build Tools if you don't have them installed already.
  - Create required Azure infrastructure (alternatively, you can use a local sql database, or sqlite). Add your connection string to botSettings.json and a script should run to scaffold the necessary tables the first time you run the bot. Ask in Discord if this doesn't work for you.
  - Install version 1.12.1, 2.4.3, or 3.5.5 of the WoW client.
  - Update values in `bootstrapperSettings.json` and `botSettings.json`
  - Generate movemaps and dump them into <repo>\Bot\mmaps (see FAQ for details).
  - Set Bootstrapper as your startup project, and fire up your debugger. You should see Wow.exe launch, and then you'll be prompted to attach a debugger to Visual Studio. To learn more about the overall flow of how the bot attached to the WoW process, [read my website](https://drewkestell.us/Article/6/Chapter/1)
  - View the documentation in the Docs folder to learn more.
  - Watch the [tutorial video](https://www.youtube.com/watch?v=g3jYHiajQdk).
  - Read the [FAQ](https://github.com/DrewKestell/BloogBot/blob/main/Docs/FAQ.md) for common troubleshooting answers.
  - Join the Discord and ask for help if you're stuck.

## Motivation
  
To explain why I did this, I refer to the first chapter of my website:

> "Low-level programming is good for the programmer's soul." - John Carmack

> I love video games. I remember when my dad brought home a Commadore 64, but I was still too young to use it without his help. From there, our first console was a Nintendo Entertainment System. But things really got serious when we got our first PC. Some of my fondest memories in gaming come from games that ran on MS-DOS. Of course we had Doom, which was fantastic. But I especially loved this "1001 Games" CD. Plenty of the games were trash, and some didn't run on our hardware, but there was a seemingly infinite amount of entertainment to be had. Eventually my mind was blown by titles like Quake, Warcraft, Diablo, and Ultima Online, all of which had incredibly innovative multiplayer experiences.

> Cheaters have existed for as long as games have. Anybody that has played a multiplayer game has likely been exposed to a hacker taking some form or another. These hackers, and the tools they used, had always seemed to me like an enigmatic underbelly of the internet. There's nothing worse than getting wrecked by a cheater in a competitive match. But disdain wasn't the only emotion those experiences evoked. They also made me curious. How did these cheats work? Who was building them? I had experimented with Diablo trainers and the like when I was younger, but the knowledge to learn how to create something like that was far beyond my capabilities.

> Having made a career of Web Development, with about 5 years of programming experience under my belt, I decided to take another crack at it. After some initial research, I started poking around on some forums, and I was blown away by just how sophisticated some of these techniques truly are, and I gained a new respect for these hackers. So much of what they were talking about was still way over my head. There was some seriously low level computer science concepts involved. Abstraction is a powerful thing - I was amazed at how far I had gotten in my career without truly understanding some of the fundamental concepts that formed the foundation of every piece of software I built. It's easy to take for granted just how good our high level programming languages are, and how impressive modern compilers have become.

> I found some great resources - the x86 Assembly Wikibook helped me understand the basics of CPU architecture, and the journey your source code takes before it's executed by your CPU. The x86 Disassembly Wikibook is a great crash course in disassembly and reverse engineering. Ownedcore has some fantastic discussions about concepts such as DLL injection, assembly injection, interoperability, function detouring, and interacting with the Windows API. Learncpp has deep and thorough explanations of how pointers and memory management work (admittedly, I've read the book twice and my C++ still stinks, but some fundamental knowledge helps a lot with reverse engineering).

> After many late nights, and plenty of borrowed code from all over the web, I had built a fully functional World of Warcraft bot capable of fighting monsters for hours on end without any human interaction. Having made it this far, it's astonishing how much there is to learn. Many of these concepts were totally foreign to me coming from a background in Web Development, and I've barely scratched the surface. But the process has been extremely gratifying, and I believe without a doubt that it has made me a better developer. Thinking about how to make a bot behave more like a human was also an interesting glimpse into the world of AI.

> I had started and stopped this intellectual journey a number of times before I found a project that kept my interest for long enough to make any significant progress. Without a formal education in computer science, the task was incredibly daunting. So my goal is to try to distill some of the lessons I've learned over the past year, hopefully making the journey a little easier for anybody else that feels the same way.

> DISCLAIMER: I have serious ethical concerns about cheating in gaming. Especially with the blossoming world of eSports and competitive gaming, using hacks in games is guaranteed to ruin the experience for other players. That being said, I think there's a difference between a bot in World of Warcraft who runs around fighting with the AI, and an aim bot in Counter-Strike that instantly destroys other players. I chose World of Warcraft not only because it's a game I loved when I was younger, but also because it's fairly (not completely) harmless to other players. I made a conscious effort to design the bot in such a way that it won't damage the experience of other players on the server. I also tested the bot exclusively on a free, private server not run by Blizzard. Ultimately my motivation was curiosity, not to "get ahead" in the game, and I think that's an important distinction. Unfortunately, cheating in gaming isn't going anywhere. The bright side is that while the techniques discussed in this article can most certainly be used to create hacks, the same concepts can be used to inform the design of anticheat software, so I think it's information worth sharing.

> Before taking a look under the hood of the WoW bot, we'll first examine a rudamentary game engine I wrote in C++ called BloogsQuest that we'll use as a contrived example to explore some fundamental concepts that are important to understand before diving fully into bot development.

> I also want to mention that the code snippets found here will be truncated, and will most certainly deviate from best practices in software development. This is less a step-by-step tutorial in bot development, and more an exploration of the high level concepts involved. Some prerequisite knowledge of C# and C++ are necessary, including understanding pointers, but I'll do my best to explain things as they come up.
  
___

![image](https://user-images.githubusercontent.com/6411339/120980933-f7368a80-c72b-11eb-97ec-d82dd02094dc.png)

![image](https://user-images.githubusercontent.com/6411339/120980947-fbfb3e80-c72b-11eb-8aa1-35d24ebd5310.png)


## Notes

You might need to Right click Fastcall -> properties -> Platform Active(Win32) -> Precompiled headers

Then:

Create StuckLog.txt in Bot dir.

Copy relevant mmaps to Bot dir.

Change bootstrapperSettings.json.

Set Bootstrapper as startup project.

Run VS as admin!

Also see: botSettings.json
My changes:

  "LootPoor": false,
  "LootCommon": false,
  "LootUncommon": false,
  "GrindingHotspotId": 1,
  *Turn off autoloot!
  *Rare loot is turned of for now in LootState (only epics are looted)
  
  Also see:
  TargetingExcludedNames, Food, Drink
