# LolGuard

A Windows service that closes League of Legends when it opens. It runs in the background as SYSTEM, starts at boot, and has no window or tray icon. Only administrators can change its settings or stop it.

## Build

```powershell
dotnet publish -c Release -o publish
```

This produces `publish\LolGuard.exe`, a single self-contained file. The target PC does not need .NET installed.

## Install (on the target PC)

1. Copy `LolGuard.exe` to the PC, in any folder.
2. Open PowerShell with **Run as administrator**.
3. Run:

```powershell
cd <folder with LolGuard.exe>
.\LolGuard.exe install
.\LolGuard.exe status
```

`install` copies the exe to `C:\Program Files\LolGuard\`, creates `C:\ProgramData\LolGuard\`, registers the service, and starts it.

## Commands (administrator only)

| Command | What it does |
| --- | --- |
| `LolGuard.exe status` | Service state, current mode, allowance, watch list, and any running LoL processes |
| `LolGuard.exe instant` | Close LoL as soon as it opens |
| `LolGuard.exe delay <minutes>` | Close LoL this many minutes after it opens (example: `delay 10`) |
| `LolGuard.exe allow <minutes>` | Let LoL run for this long, then enforce again (example: `allow 30`) |
| `LolGuard.exe allow off` | Cancel an allowance |
| `LolGuard.exe uninstall` | Stop and remove the service |

Every command must be run from an administrator PowerShell. Without that, it exits with an error and changes nothing.

## How it works

- Checks the process list every 500 ms.
- **Instant:** closes a watched process as soon as it is seen.
- **Delay:** starts a countdown when a watched process first appears, then closes every watched process when the countdown ends. If the countdown has finished and LoL reopens, it is closed again straight away.
- **Allowance:** pauses enforcement until the time you set. When it ends, enforcement resumes with a fresh countdown in delay mode.
- Watched processes are closed with their child processes.

## Files

- `C:\ProgramData\LolGuard\config.json`: settings. Changes take effect within about a second. Keys are camelCase. Use `processNames` to change which processes are watched.
- `C:\ProgramData\LolGuard\guard.log`: log of starts, closes, and config errors.

Default watch list: `League of Legends.exe`, `LeagueClient.exe`, `LeagueClientUx.exe`, `LeagueClientUxRender.exe`.

## Access control

- Every management command checks for an elevated token before it does anything.
- The config folder is readable and writable only by SYSTEM and Administrators.
- Standard users cannot stop or reconfigure the service under default Windows permissions.

## Limits

- **Not a security boundary against an administrator.** An administrator account can always stop, reconfigure, or remove the service. So can anyone who boots the PC into Safe Mode or from other media.
- **Not invisible.** The service appears in `services.msc` and Task Manager for anyone who looks. It has no user interface.
- **Process names are not yet verified against a real install.** The defaults follow Riot's usual names. Check Task Manager while LoL is open and edit `processNames` if they differ.
- **The Riot launcher is not on the list.** `RiotClientServices.exe` also starts VALORANT. Adding it would block VALORANT too.

## Uninstall

```powershell
.\LolGuard.exe uninstall
```

Then delete `C:\Program Files\LolGuard\` and `C:\ProgramData\LolGuard\` as an administrator.
