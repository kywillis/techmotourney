# Tecmo Score Grabber

Windows desktop app that captures the primary (or selected) monitor, detects the Tecmo final score screen (FSS) by comparing to a reference image, parses scores and stats with OpenAI vision, matches an in-progress game from `GET /api/game-station/games`, and saves with `PUT /api/results/{gameResultId}`.

## Requirements

- .NET 8 **desktop** runtime on the target machine (framework-dependent publish), **or** use a self-contained publish if you prefer no runtime install.
- Default FSS reference image is **`Assets/fss-reference.png`** in the project (copied next to the exe at build). Override `Capture:FssReferenceImagePath` in `appsettings.json` if needed. Use **the same resolution and layout** as at the event.
- Copy `appsettings.example.json` to `appsettings.json` and set your API base URL and OpenAI key (or use environment variable `OPENAI_API_KEY`). The repo ignores `appsettings.json` so keys are not committed.
- **Release builds:** `appsettings.Production.json` is merged on top of `appsettings.json` and sets **`ApiBaseUrl`** to **`https://tecmo.azurewebsites.net/api`** (same host as the deployed SPAs). Debug builds optionally load `appsettings.Development.json` for local overrides.
- Main area uses **Log**, **Last capture**, and **Vision input** tabs: **Vision input** shows the full-capture grayscale image sent to the LLM after an FSS hit.

## Portable deployment (no installer)

From the `ScoreGrabber` directory in this repository (same folder as `ScoreGrabber.csproj`):

```powershell
dotnet publish .\ScoreGrabber.csproj -c Release -r win-x64 --self-contained false -o .\publish\ScoreGrabber
```

Copy the entire `publish\ScoreGrabber` folder to the station PC. Edit `appsettings.json` beside `TecmoScoreGrabber.exe` as needed.

## Server database

Run `AddGameResultSaveAudit.sql` from your TecmoTourney API repo (`Service/Scripts/`) against your tournament database so save audits are recorded.

## Configuration (`appsettings.json`)

Layer order: **`appsettings.json`** then **`appsettings.Production.json`** (Release) or **`appsettings.Development.json`** (optional, Debug).

| Key | Purpose |
|-----|---------|
| `ApiBaseUrl` | Base URL including `/api` (e.g. `https://your-host/api`) |
| `OpenAI:ApiKey` | OpenAI key (omit if using `OPENAI_API_KEY`) |
| `OpenAI:Model` | Vision-capable model (default `gpt-4o`) |
| `OpenAI:FewShotExampleImagePath` | Optional few-shot PNG (default `Assets/fss-reference.png`); must match the baked-in golden JSON or set empty to disable |
| `Capture:IntervalSeconds` | Screenshot interval in seconds (default `60`) |
| `Capture:MonitorIndex` | `0` = primary; use the **Capture monitor** dropdown in the UI to pick a display (saved to `appsettings.json`). |
| `Capture:FssReferenceImagePath` | Reference FSS image path |
| `Capture:FssSimilarityThreshold` | 0–1; higher = stricter match |
| `Capture:DebugFailedCaptureCount` | Max PNGs kept in `debug-captures` |
| `SaveSource` | Sent as `saveSource` on each save (audit) |
| `LogMaxBytes` | Rolling file log cap (`logs/score-grabber.log`) |
| `Ui:StyledDialogAutoCloseSeconds` | **Game Saved** / **Game Not Found** dialogs: seconds until auto-dismiss; first button is **Keep Open** then **Close**. Use `0` to disable countdown (single **Close**, no timer). |

## Tie games

- When the parsed score is a tie, the grabber sends `allowTieScore: true`, `status` **In Progress** (game stays open), and `accumulateStatsFromTieLeg: false`. Sudden-death / overtime completion and any final tournament save are done manually (not by chaining captures in the grabber).

## Secrets

The UI does not display API keys. Prefer environment variables on shared machines.
