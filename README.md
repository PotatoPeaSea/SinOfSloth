# SinOfSloth

A Windows service that closes League of Legends when it opens. It runs in the background as SYSTEM, starts at boot, and has no window or tray icon. Only administrators can change its settings or stop it.

## Branches

- **`master`** blocks League **and** Riot Vanguard. VALORANT needs Vanguard, so VALORANT is blocked too.
- **`league-only`** blocks League only. Vanguard keeps running, so VALORANT can be played.

Both branches share the same code. They differ only in the default watch list in `Settings.cs` and in the watch-list section of this README.

Use it only on computers you administer, and let the people who use them know it is there.

## Build

```powershell
dotnet publish -c Release -o publish
```

This produces `publish\SinOfSloth.exe`, a single self-contained file. The target PC does not need .NET installed.

## One-click install (recommended)

`.\build-kit.ps1` builds `dist\SinOfSloth-Install.zip`, which holds `SinOfSloth.exe` and `Install.bat`.

On the target PC:

1. Copy the zip over and extract it anywhere, for example the Desktop.
2. Double-click `Install.bat`. Click **Yes** when Windows asks for administrator permission.
3. The window shows the status and waits for a key press.

It installs in the default mode (`instant`). Running it again updates the service and keeps the existing `config.json`. For a grace period, run `SinOfSloth.exe delay 10` from an administrator PowerShell.

## Manual install (on the target PC)

1. Copy `SinOfSloth.exe` to the PC, in any folder.
2. Open PowerShell with **Run as administrator**.
3. Run:

```powershell
cd <folder with SinOfSloth.exe>
.\SinOfSloth.exe install
.\SinOfSloth.exe status
```

`install` copies the exe to `C:\Program Files\SinOfSloth\`, creates `C:\ProgramData\SinOfSloth\`, registers the service, and starts it.

## Commands (administrator only)

| Command | What it does |
| --- | --- |
| `SinOfSloth.exe status` | Service state, current mode, allowance, watch list, and any running LoL processes |
| `SinOfSloth.exe instant` | Close LoL as soon as it opens |
| `SinOfSloth.exe delay <minutes>` | Close LoL this many minutes after it opens (example: `delay 10`) |
| `SinOfSloth.exe allow <minutes>` | Let LoL run for this long, then enforce again (example: `allow 30`) |
| `SinOfSloth.exe allow off` | Cancel an allowance |
| `SinOfSloth.exe uninstall` | Stop and remove the service |

Every command must be run from an administrator PowerShell. Without that, it exits with an error and changes nothing.

## How it works

- Checks the process list every 500 ms.
- **Instant:** closes a watched process as soon as it is seen.
- **Delay:** starts a countdown when a watched process first appears, then closes every watched process when the countdown ends. If the countdown has finished and LoL reopens, it is closed again straight away.
- **Allowance:** pauses enforcement until the time you set. When it ends, enforcement resumes with a fresh countdown in delay mode.
- Watched processes are closed with their child processes.

## Files

- `C:\ProgramData\SinOfSloth\config.json`: settings. Changes take effect within about a second. Keys are camelCase. Use `processNames` to change which processes are watched.
- `C:\ProgramData\SinOfSloth\guard.log`: log of starts, closes, and config errors.

The default watch list is used only when `config.json` does not exist yet. A PC that already has a config keeps its old list. To switch an installed PC to a different branch's list, edit `processNames` in `config.json`, or delete the file before reinstalling.

Default watch list:

- League: `League of Legends.exe`, `LeagueClient.exe`, `LeagueClientUx.exe`, `LeagueClientUxRender.exe`

Riot Vanguard is not on this branch's list, so VALORANT can run.

`SinOfSloth.exe status` prints the active list under "Watching".

## Access control

- Every management command checks for an elevated token before it does anything.
- The config folder is readable and writable only by SYSTEM and Administrators.
- Standard users cannot stop or reconfigure the service under default Windows permissions.

## Limits

- **Not a security boundary against an administrator.** An administrator account can always stop, reconfigure, or remove the service. So can anyone who boots the PC into Safe Mode or from other media.
- **Not invisible.** The service appears in `services.msc` and Task Manager for anyone who looks. It has no user interface.
- **VALORANT is not blocked on this branch.** Riot Vanguard is not on the list, so VALORANT and its Vanguard services keep running. The guard only closes League processes.
- **League's process names are unverified.** They follow Riot's usual naming and have not been checked against an install on the test PC. Check Task Manager while LoL is open and edit `processNames` if they differ.
- **The Riot launcher is not on the list.** `RiotClientServices.exe` also starts VALORANT. Adding it would block VALORANT too.

## Uninstall

```powershell
.\SinOfSloth.exe uninstall
```

Then delete `C:\Program Files\SinOfSloth\` and `C:\ProgramData\SinOfSloth\` as an administrator.
