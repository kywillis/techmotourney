# Moving the repo to `C:\Dev\techmotourney` (Option A)

The Git repository now includes **Service**, **Client**, and **ScoreGrabber** under one tree. If you still keep the checkout under `C:\Dev\tecmoRestore\techmotourney`, you can optionally **rename the folder on disk** so everything lives at `C:\Dev\techmotourney`.

**Do this when nothing is using the folders** (close Cursor/VS, stop `dotnet run`, etc.).

1. **Rename the old external tree** (if it still exists and you want a backup):
   ```powershell
   Rename-Item -Path "C:\Dev\techmotourney" -NewName "techmotourney_legacy"
   ```

2. **Move this repository** to `C:\Dev\techmotourney`:
   ```powershell
   Move-Item -Path "C:\Dev\tecmoRestore\techmotourney" -Destination "C:\Dev\techmotourney"
   ```

3. **Optional:** Copy anything you still need from `C:\Dev\techmotourney_legacy` (e.g. old experiments), then remove that folder after you are sure you do not need it.

4. **Re-open** Cursor/Visual Studio **from** `C:\Dev\techmotourney`.

5. **ScoreGrabber:** After cloning or moving, copy `ScoreGrabber\appsettings.example.json` to `ScoreGrabber\appsettings.json` and set secrets locally (those files are **gitignored**).

The solution **`Service\TecmoTourney.sln`** already references **`..\ScoreGrabber\ScoreGrabber.csproj`** so no path edits are needed after the move.
