# Static assets (Angular 19)

Place **`faces.png`** here: `public/assets/faces.png`

**`stars.png`** — Tecmo-style star strip (`src/assets/stars.png`, served as `/assets/stars.png`): header clusters in main nav and place-wager title, and any `.tecmo-star-divider` full-bleed repeat.

That URL in the browser is **`/assets/faces.png`**. No Angular **routing** is involved—the dev server and build copy files from `public/` (and from `src/assets/` per `angular.json`) into the app output.

If you see **Cannot GET /assets/faces.png**:

1. **`angular.json` must set `servePath: "/"`** when using `<base href="/">`. Otherwise the Angular 19 Vite dev server can derive an empty serve path and break **every** static URL (only the last character of the path is used for lookups).
2. Confirm the file is **`public/assets/faces.png`** or **`src/assets/faces.png`** under `Client/tecmo-wager/`.
3. **Restart `ng serve`** after adding new asset files.

Replace the placeholder `faces.png` with your full Tecmo player sprite sheet when ready.
