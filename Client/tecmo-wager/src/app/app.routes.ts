import { Routes } from '@angular/router';
import {
  wagerAuthGuard,
  guestOnlyGuard,
  wagerAdminGuard,
  playerRoutesExcludeAdminGuard
} from './core/guards/wager-auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login/login.component').then(m => m.LoginComponent), canActivate: [guestOnlyGuard] },
  { path: 'pending', loadComponent: () => import('./features/pending/pending.component').then(m => m.PendingComponent), canActivate: [wagerAuthGuard('pending')] },
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/landing/landing.component').then(m => m.LandingComponent),
    canActivate: [wagerAuthGuard('active'), playerRoutesExcludeAdminGuard]
  },
  {
    path: 'wagers',
    loadComponent: () => import('./features/my-wagers/my-wagers.component').then(m => m.MyWagersComponent),
    canActivate: [wagerAuthGuard('active'), playerRoutesExcludeAdminGuard]
  },
  {
    path: 'wagers/games',
    loadComponent: () => import('./features/games/games-list/games-list.component').then(m => m.GamesListComponent),
    canActivate: [wagerAuthGuard('active'), playerRoutesExcludeAdminGuard]
  },
  {
    path: 'wagers/games/:gameResultId',
    loadComponent: () => import('./features/games/game-detail/game-detail.component').then(m => m.GameDetailComponent),
    canActivate: [wagerAuthGuard('active'), playerRoutesExcludeAdminGuard]
  },
  {
    path: 'games',
    loadComponent: () => import('./features/landing/landing.component').then(m => m.LandingComponent),
    canActivate: [wagerAuthGuard('active'), playerRoutesExcludeAdminGuard]
  },
  {
    path: 'activity',
    loadComponent: () => import('./features/activity/activity.component').then(m => m.ActivityComponent),
    canActivate: [wagerAuthGuard('active'), playerRoutesExcludeAdminGuard]
  },
  {
    path: 'admin',
    canActivate: [wagerAuthGuard('active'), wagerAdminGuard],
    loadComponent: () => import('./features/admin/admin-shell/admin-shell.component').then(m => m.AdminShellComponent),
    children: [
      { path: '', loadComponent: () => import('./features/admin/admin-home/admin-home.component').then(m => m.AdminHomeComponent) },
      {
        path: 'players',
        loadComponent: () =>
          import('./features/admin/admin-players-list/admin-players-list.component').then(m => m.AdminPlayersListComponent)
      },
      {
        path: 'players/:playerId',
        loadComponent: () =>
          import('./features/admin/admin-player-audit/admin-player-audit.component').then(m => m.AdminPlayerAuditComponent)
      },
      { path: 'balance', loadComponent: () => import('./features/admin/admin-balance/admin-balance.component').then(m => m.AdminBalanceComponent) },
      { path: 'wagers', loadComponent: () => import('./features/admin/admin-wagers/admin-wagers.component').then(m => m.AdminWagersComponent) },
      { path: 'games', loadComponent: () => import('./features/admin/admin-games/admin-games.component').then(m => m.AdminGamesComponent) },
      { path: 'games/:gameResultId', loadComponent: () => import('./features/admin/admin-game-edit/admin-game-edit.component').then(m => m.AdminGameEditComponent) },
      {
        path: 'pending-players',
        loadComponent: () =>
          import('./features/admin/admin-pending-players/admin-pending-players.component').then(m => m.AdminPendingPlayersComponent)
      },
      {
        path: 'snapshot',
        loadComponent: () => import('./features/admin/admin-snapshot/admin-snapshot.component').then(m => m.AdminSnapshotComponent)
      },
      {
        path: 'snapshot/wagers/:kind/:id',
        loadComponent: () =>
          import('./features/admin/admin-tournament-wagers-list/admin-tournament-wagers-list.component').then(
            m => m.AdminTournamentWagersListComponent
          )
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
