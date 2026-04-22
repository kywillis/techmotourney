import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type ActivityLogLevel = 'info' | 'error' | 'success';

export interface ActivityLogEntry {
  id: string;
  at: string;
  level: ActivityLogLevel;
  text: string;
}

const STORAGE_KEY = 'tecmo-tourney-activity-log';
/** ISO timestamp: user last opened the activity log modal (persisted across refresh). */
const STORAGE_KEY_LAST_VIEW = 'tecmo-tourney-activity-log-last-view';
const MAX_ENTRIES = 20;

@Injectable({
  providedIn: 'root'
})
export class NotificationLogService {
  private _entries: ActivityLogEntry[] = [];
  /** When set, error entries at or before this time are treated as read. */
  private _lastViewedAt: string | null = null;
  private readonly _hasUnreadError = new BehaviorSubject<boolean>(false);

  /** True when there are error entries newer than the last time the log was opened. */
  readonly hasUnreadError$: Observable<boolean> = this._hasUnreadError.asObservable();

  constructor() {
    this.loadFromStorage();
    this._hasUnreadError.next(this.computeHasUnreadError());
  }

  get entries(): ReadonlyArray<ActivityLogEntry> {
    return this._entries;
  }

  /** Call when the user opens the activity log so the unread-error badge clears. */
  markActivityLogViewed(): void {
    this._lastViewedAt = new Date().toISOString();
    this.persistLastViewed();
    this._hasUnreadError.next(false);
  }

  add(entry: { level: ActivityLogLevel; text: string }): void {
    const e: ActivityLogEntry = {
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`,
      at: new Date().toISOString(),
      level: entry.level,
      text: entry.text
    };
    this._entries = [e, ...this._entries].slice(0, MAX_ENTRIES);
    this.persist();
    this._hasUnreadError.next(this.computeHasUnreadError());
  }

  clear(): void {
    this._entries = [];
    this.persist();
    this._hasUnreadError.next(false);
  }

  private computeHasUnreadError(): boolean {
    return this._entries.some(
      (e) => e.level === 'error' && (!this._lastViewedAt || e.at > this._lastViewedAt)
    );
  }

  private loadFromStorage(): void {
    try {
      this._lastViewedAt = localStorage.getItem(STORAGE_KEY_LAST_VIEW);
    } catch {
      this._lastViewedAt = null;
    }
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return;
      const parsed = JSON.parse(raw) as ActivityLogEntry[];
      if (Array.isArray(parsed)) {
        this._entries = parsed.slice(0, MAX_ENTRIES);
      }
    } catch {
      this._entries = [];
    }
  }

  private persistLastViewed(): void {
    try {
      if (this._lastViewedAt) {
        localStorage.setItem(STORAGE_KEY_LAST_VIEW, this._lastViewedAt);
      }
    } catch {
      /* ignore quota */
    }
  }

  private persist(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this._entries));
    } catch {
      /* ignore quota */
    }
  }
}
