# SinOfSloth

A Windows service that closes League of Legends when it opens. It runs in the background as SYSTEM, starts at boot, and has no window or tray icon. Only administrators can change its settings or stop it.

## Branches

- **`master`** blocks League **and** Riot Vanguard. VALORANT needs Vanguard, so VALORANT is blocked too.
- **`league-only`** blocks League only. Vanguard keeps running, so VALORANT can be played.

Both branches share the same code. They differ only in the default watch list in `Settings.cs` and in the watch-list section of this README.

Use it only on computers you administer, and let the people who use them know it is there.

## Installation guide (league-only branch)

This branch closes League of Legends only. Riot Vanguard is not on the watch list, so VALORANT keeps working. Follow the steps in order.

### 1. Before you start

- **Build machine:** Windows with the .NET 10 SDK. `build-kit.ps1` looks for it at `%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe`. Get the `league-only` branch:

  ```powershell
  git clone https://github.com/PotatoPeaSea/SinOfSloth.git
  cd SinOfSloth
  git checkout league-only
  ```

- **Target PC:** Windows 10 or 11, 64-bit, with an administrator account. .NET does not need to be installed there.
- Install only on computers you administer, and let their users know the guard is there.

### 2. Build the install kit

```powershell
git branch --show-current    # must print league-only
.\build-kit.ps1
```

This produces `dist\SinOfSloth-Install.zip`, which holds `SinOfSloth.exe` and `Install.bat`. The watch list is compiled into the exe, so build from `league-only` to get the League-only list.

### 3. Install on the target PC

1. Copy `SinOfSloth-Install.zip` to the PC and extract it, for example to the Desktop.
2. Double-click `Install.bat`.
3. Click **Yes** when Windows asks for administrator permission.
4. The window installs the service, shows its status, and waits for a key press.

The service starts at every boot in `instant` mode, so LoL is closed as soon as it opens.

### 4. Check that it works

Open PowerShell with **Run as administrator** and run:

```powershell
$sos = "$env:ProgramFiles\SinOfSloth\SinOfSloth.exe"
& $sos status
```

On this branch, the output should look like this (LoL not running):

```text
Service:   Running
Mode:      instant
Allowed:   no
Watching:  League of Legends.exe, LeagueClient.exe, LeagueClientUx.exe, LeagueClientUxRender.exe
Running:   none
Config:    C:\ProgramData\SinOfSloth\config.json
```

Optional test: open League. It should close within about a second. Each close is written to `C:\ProgramData\SinOfSloth\guard.log`.

If `Watching` lists `vgc.exe`, `vgtray.exe` or `vgm.exe`, the PC still has a config from the `master` build. Follow [Upgrading a PC](#upgrading-a-pc) below.

### 5. Choose a mode (optional)

Run these from an administrator PowerShell, after `$sos` is set as in step 4:

| Command | Effect |
| --- | --- |
| `& $sos instant` | Close LoL as soon as it opens (the default) |
| `& $sos delay 10` | Close LoL 10 minutes after it opens |
| `& $sos allow 30` | Let LoL run for 30 minutes, then enforce again |
| `& $sos allow off` | Cancel an allowance |

Running `Install.bat` again keeps the current mode and config.

### Upgrading a PC

**From an older build named LolGuard:** the service name has changed, so remove the old service first. In an administrator PowerShell:

```powershell
& "$env:ProgramFiles\LolGuard\LolGuard.exe" uninstall
Remove-Item -Recurse -Force "$env:ProgramFiles\LolGuard", "$env:ProgramData\LolGuard"
```

Then install the new build as above. Settings are not carried over, so set the mode again.

**From the `master` build:** `Install.bat` keeps the existing `config.json`, so the Vanguard entries stay on the list and VALORANT stays blocked. To reset to this branch's list, run these in an administrator PowerShell:

```powershell
Remove-Item "$env:ProgramData\SinOfSloth\config.json"
& $sos status
```

With no config file, the guard uses this branch's defaults (instant mode, League-only list). Check that `Watching` now lists only the four League processes. The file is created again the next time you run an admin command such as `instant`. If you want to keep your mode and other settings, open `C:\ProgramData\SinOfSloth\config.json` in Notepad (run as administrator) and remove the `vgc.exe`, `vgtray.exe` and `vgm.exe` lines from `processNames` instead.

### Troubleshooting

- **"needs an Administrator prompt":** open PowerShell with **Run as administrator**. Every command needs it.
- **`Install.bat` reports "Install failed":** read the message above the `Press any key` prompt, fix it, and run `Install.bat` again.
- **`Service: Stopped` or `not installed`:** check `C:\ProgramData\SinOfSloth\guard.log`, then run `Start-Service SinOfSloth` in an administrator PowerShell.
- **LoL is not closed:** while LoL is open, run `& $sos status` and compare `Running:` with `Watching:`. If LoL runs under other process names, find them in Task Manager and add them to `processNames` (see [Limits](#limits)).
- **VALORANT is blocked:** the PC still has Vanguard on its list. See [Upgrading a PC](#upgrading-a-pc).

### Uninstall

```powershell
& $sos uninstall
Remove-Item -Recurse -Force "$env:ProgramFiles\SinOfSloth", "$env:ProgramData\SinOfSloth"
```

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
