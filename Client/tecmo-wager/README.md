# Tecmo Wager

Mobile-first Angular app for the wagering feature. Uses Google Sign-In and the same Service API as the main tournament app.

## Setup

1. **API URL** – Edit `src/environments/environment.ts` (and `environment.prod.ts` for production). Set `apiUrl` to your Service API base (e.g. `https://localhost:7043/api`).
2. **Google Client ID** – Set `googleClientId` in the same environment files to the same Google OAuth client ID used by the Service (required for Sign in with Google to work).

## Session persistence

After a successful Google sign-in, the **Google ID token** is stored in `localStorage` under `tecmo-wager.google-id-token`. On each full page load, the app restores the session by calling `POST /wager/auth/google` with that token before routing, so refresh keeps you signed in until the token expires or you clear site data.

**Token lifetime:** Google ID tokens usually expire in about **one hour**. After expiry, the stored token is cleared and you’ll need to sign in again. A true multi-hour session would require the API to issue its own JWT after validating Google once.

**Logout:** Call `WagerAuthService.logout()` when you add a sign-out control; it clears memory and `localStorage`.

## Development server

Run from this folder:

```bash
ng serve
```

The app runs at **http://localhost:4201/** (port 4201 so it can run alongside the main tecmo-tourney app on 4200). The application will automatically reload when you change source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
