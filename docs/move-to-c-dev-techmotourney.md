# Having the repo at `C:\Dev\techmotourney`

The Git repository includes **Service**, **Client**, and **ScoreGrabber** in one tree. You can keep **`C:\Dev\tecmoRestore\techmotourney`** as your “authoritative” clone and **copy** it to **`C:\Dev\techmotourney`** so that path matches your mental model—no need to *move* (rename) unless you want only one folder.

**Close apps that lock files under either path** before copying (Cursor workspace, terminals `cd`-ed into those folders, `dotnet run`, etc.).

## Option A — Copy (recommended; keeps `tecmoRestore` intact)

From PowerShell:

```powershell
robocopy "C:\Dev\tecmoRestore\techmotourney" "C:\Dev\techmotourney" /E `
  /XD node_modules bin obj .vs publish /R:2 /W:2 /MT:8
```

This copies **everything** except **`node_modules`**, **`bin`**, **`obj`**, **`.vs`**, and **`publish`** (large or regenerable). **`.git` is included** so the destination is still a Git repo.

- **robocopy exit code `0`–`7`** = completed without hard failure (Microsoft’s quirky success range).
- **Leftover files** may still exist under `C:\Dev\techmotourney` if that folder had *extra* files not in the source; robocopy does not delete them unless you use `/MIR` (mirror), which can be destructive—only use mirror if you want the destination to match the source exactly.

After copy, in each Angular app folder under `Client\` run **`npm install`** (or `npm ci`) if you need `node_modules` at the new path.

## Option B — Move / rename (single folder only)

If you want **one** checkout and are fine removing the old path:

1. Close everything using `C:\Dev\techmotourney` and `C:\Dev\tecmoRestore\techmotourney`.
2. Rename or remove the existing `C:\Dev\techmotourney` if you need the name free.
3. Move the repo:
   ```powershell
   Move-Item -Path "C:\Dev\tecmoRestore\techmotourney" -Destination "C:\Dev\techmotourney"
   ```

## After either option

1. Open Cursor/Visual Studio from **`C:\Dev\techmotourney`** when you work there.
2. **ScoreGrabber:** copy `ScoreGrabber\appsettings.example.json` to `ScoreGrabber\appsettings.json` and set secrets locally (those files are **gitignored** on purpose).

**`Service\TecmoTourney.sln`** already references **`..\ScoreGrabber\ScoreGrabber.csproj`**; no path edits are needed.
