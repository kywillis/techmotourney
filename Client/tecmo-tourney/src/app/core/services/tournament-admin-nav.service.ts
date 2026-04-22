import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

export interface TournamentAdminMenuFlags {
  resetEntire: boolean;
  restartPhase: boolean;
  /** Debug: fill incomplete prelim games with random stats (preliminaries phase only). */
  fillFakePrelimResults: boolean;
  /** Double-elim viewer: server-side bracket reconciliation (tournament phase, non-legacy). */
  recalculateBracket: boolean;
}

/**
 * Coordinates tournament admin actions triggered from the global header hamburger
 * with {@link ViewTournamentComponent} (modals and API calls).
 */
@Injectable({ providedIn: 'root' })
export class TournamentAdminNavService {
  private readonly resetEntire$ = new Subject<void>();
  private readonly restartPhase$ = new Subject<void>();
  private readonly fillFakePrelim$ = new Subject<void>();
  private readonly recalculateBracket$ = new Subject<void>();
  readonly resetEntireRequested$ = this.resetEntire$.asObservable();
  readonly restartPhaseRequested$ = this.restartPhase$.asObservable();
  readonly fillFakePrelimRequested$ = this.fillFakePrelim$.asObservable();
  readonly recalculateBracketRequested$ = this.recalculateBracket$.asObservable();

  private readonly menuFlags = new BehaviorSubject<TournamentAdminMenuFlags>({
    resetEntire: false,
    restartPhase: false,
    fillFakePrelimResults: false,
    recalculateBracket: false
  });
  readonly menuFlags$ = this.menuFlags.asObservable();

  setTournamentMenuFlags(flags: TournamentAdminMenuFlags): void {
    this.menuFlags.next(flags);
  }

  clearTournamentMenuFlags(): void {
    this.menuFlags.next({
      resetEntire: false,
      restartPhase: false,
      fillFakePrelimResults: false,
      recalculateBracket: false
    });
  }

  requestResetEntireTournament(): void {
    this.resetEntire$.next();
  }

  requestRestartToPreliminaries(): void {
    this.restartPhase$.next();
  }

  requestFillFakePrelimResults(): void {
    this.fillFakePrelim$.next();
  }

  requestRecalculateBracket(): void {
    this.recalculateBracket$.next();
  }
}
