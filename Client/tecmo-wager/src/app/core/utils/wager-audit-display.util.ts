import { WagerAuditEntry } from '../models/wager-audit-entry.model';
import { MyWager } from '../models/my-wager.model';
import { BettableGame } from '../models/bettable-game.model';
import { WagerGamesBoard } from '../models/wager-games-board.model';
import { formatWagerPick } from './wager-display.util';

export function wagerAuditActionLabel(action: string): string {
  switch (action) {
    case 'PlaceWager':
      return 'Wager Placed';
    case 'CancelWager':
    case 'AdminCancelWager':
      return 'Wager Cancelled';
    case 'SettleWagerWin':
      return 'Wager Won';
    case 'SettleWagerLose':
      return 'Wager Lost';
    case 'VoidWager':
      return 'Wager Void';
    case 'ReverseSettlement':
      return 'Settlement Reversed';
    case 'GameResultRemoved':
      return 'Game Removed';
    case 'BalanceAdd':
      return 'Funds added';
    case 'BalanceSet':
      return 'Balance set';
    case 'BalanceSetToZero':
      return 'Balance cleared';
    default:
      return action.replace(/([A-Z])/g, ' $1').trim();
  }
}

export function formatWagerAuditMoney(n: number): string {
  const r = Math.round(n * 100) / 100;
  return Number.isInteger(r) ? `$${r}` : `$${r.toFixed(2)}`;
}

export interface AuditGameInfo {
  player1Name: string;
  player2Name: string;
  tournamentId?: number | null;
}

export interface AuditMatchupLine {
  text: string;
  routerLink: string[] | null;
  queryParams: Record<string, string> | null;
}

export function auditEntryHasGame(e: WagerAuditEntry): boolean {
  return e.gameResultId != null && e.gameResultId > 0;
}

export function isAuditSettleOutcome(action: string): boolean {
  return action === 'SettleWagerWin' || action === 'SettleWagerLose';
}

export function indexWagersById(wagers: MyWager[]): Map<number, MyWager> {
  const m = new Map<number, MyWager>();
  for (const w of wagers) {
    if (w.wagerId > 0) {
      m.set(w.wagerId, w);
    }
  }
  return m;
}

export function indexGamesFromWagers(wagers: MyWager[]): Map<number, AuditGameInfo> {
  const m = new Map<number, AuditGameInfo>();
  for (const w of wagers) {
    if (w.gameResultId > 0) {
      m.set(w.gameResultId, {
        player1Name: w.player1Name,
        player2Name: w.player2Name,
        tournamentId: w.tournamentId
      });
    }
  }
  return m;
}

export function indexGamesFromBettableGames(games: BettableGame[]): Map<number, AuditGameInfo> {
  const m = new Map<number, AuditGameInfo>();
  for (const g of games) {
    m.set(g.gameResultId, {
      player1Name: g.player1Name,
      player2Name: g.player2Name,
      tournamentId: g.tournamentId
    });
  }
  return m;
}

export function indexGamesFromBoard(board: WagerGamesBoard): Map<number, AuditGameInfo> {
  return indexGamesFromBettableGames([
    ...board.openForBetting,
    ...board.inProgress,
    ...board.completed
  ]);
}

export function indexGamesFromAdminResults(
  rows: {
    gameResultId: number;
    tournamentId: number;
    player1?: { playerName?: string };
    player2?: { playerName?: string };
  }[]
): Map<number, AuditGameInfo> {
  const m = new Map<number, AuditGameInfo>();
  for (const r of rows) {
    m.set(r.gameResultId, {
      player1Name: r.player1?.playerName ?? '',
      player2Name: r.player2?.playerName ?? '',
      tournamentId: r.tournamentId
    });
  }
  return m;
}

export function mergeGameInfoMaps(...maps: Map<number, AuditGameInfo>[]): Map<number, AuditGameInfo> {
  const out = new Map<number, AuditGameInfo>();
  for (const map of maps) {
    map.forEach((v, k) => out.set(k, v));
  }
  return out;
}

/** Net profit if the wager wins (potentialPayout − stake). */
export function wagerProfitToWin(w: MyWager): number | null {
  const payout = w.potentialPayout;
  const stake = w.stakeAmount;
  if (!Number.isFinite(payout) || !Number.isFinite(stake)) {
    return null;
  }
  const profit = Math.round((payout - stake) * 100) / 100;
  return profit > 0 ? profit : null;
}

export function auditPlaceWagerHeadline(e: WagerAuditEntry, w: MyWager | undefined): string {
  const label = wagerAuditActionLabel('PlaceWager');
  const stake =
    w != null && Number.isFinite(w.stakeAmount)
      ? w.stakeAmount
      : e.amount != null && !Number.isNaN(e.amount)
        ? Math.abs(e.amount)
        : null;
  if (stake == null) {
    return label;
  }
  const stakeTxt = formatWagerAuditMoney(stake);
  if (w) {
    const profit = wagerProfitToWin(w);
    if (profit != null) {
      return `${label} · Stake ${stakeTxt} to win ${formatWagerAuditMoney(profit)}`;
    }
  }
  return `${label} · Stake ${stakeTxt}`;
}

export function auditPickDescription(w: MyWager | undefined): string | null {
  if (!w) {
    return null;
  }
  const d = (w.pickDescription || '').trim();
  if (d) {
    return d;
  }
  return formatWagerPick(w.marketType, w.side, w.player1Name, w.player2Name);
}

export function buildAuditMatchupLine(
  e: WagerAuditEntry,
  wagersById: Map<number, MyWager>,
  gamesById: Map<number, AuditGameInfo>,
  mode: 'activity' | 'admin'
): AuditMatchupLine | null {
  if (!auditEntryHasGame(e)) {
    return null;
  }
  const gid = e.gameResultId!;
  const w = e.wagerId != null && e.wagerId > 0 ? wagersById.get(e.wagerId) : undefined;
  const game = gamesById.get(gid);
  let p1 = (w?.player1Name || game?.player1Name || '').trim();
  let p2 = (w?.player2Name || game?.player2Name || '').trim();
  const hasNames = p1.length > 0 || p2.length > 0;
  const knownGame = game != null || hasNames;

  if (!knownGame) {
    return { text: `game deleted #${gid}`, routerLink: null, queryParams: null };
  }

  const text = `${p1 || '—'} vs ${p2 || '—'} #${gid}`;
  const tid = e.tournamentId ?? w?.tournamentId ?? game?.tournamentId ?? null;

  if (mode === 'activity') {
    return { text, routerLink: ['/wagers/games', String(gid)], queryParams: null };
  }

  const queryParams =
    tid != null && tid > 0 ? ({ tournamentId: String(tid) } as Record<string, string>) : null;
  return {
    text,
    routerLink: ['/admin/snapshot/wagers', 'game', String(gid)],
    queryParams
  };
}

export function wagerAuditAmountDetail(e: WagerAuditEntry): string | null {
  switch (e.action) {
    case 'PlaceWager':
      return null;
    case 'CancelWager':
    case 'AdminCancelWager':
      if (e.amount == null || Number.isNaN(e.amount)) return null;
      return `Refund ${formatWagerAuditMoney(Math.abs(e.amount))}`;
    case 'SettleWagerWin':
      if (e.amount == null || Number.isNaN(e.amount)) return null;
      return `Payout ${formatWagerAuditMoney(Math.abs(e.amount))}`;
    case 'SettleWagerLose':
      return 'Stake lost';
    case 'VoidWager':
      if (e.amount == null || Number.isNaN(e.amount)) return null;
      return `Refund ${formatWagerAuditMoney(Math.abs(e.amount))}`;
    case 'ReverseSettlement':
      if (e.amount == null || Number.isNaN(e.amount)) return null;
      return `Adjustment ${formatWagerAuditMoney(e.amount)}`;
    case 'BalanceAdd':
      if (e.amount == null || Number.isNaN(e.amount)) return null;
      return `Added ${formatWagerAuditMoney(Math.abs(e.amount))}`;
    case 'BalanceSet':
      if (e.amount == null || Number.isNaN(e.amount)) return null;
      return `Set to ${formatWagerAuditMoney(Math.abs(e.amount))}`;
    case 'BalanceSetToZero':
      return 'Set to $0';
    default:
      if (e.amount == null || Number.isNaN(e.amount)) return null;
      return formatWagerAuditMoney(Math.abs(e.amount));
  }
}

export type WagerHistoryLinkMode = 'activity' | 'admin';

export type WagerHistoryCardKind = 'place' | 'settle' | 'other';

export type WagerOutcomeTone = 'won' | 'lost' | 'void' | 'cancelled' | null;

export interface WagerHistoryCardView {
  kind: WagerHistoryCardKind;
  action: string;
  placeHeadline?: string;
  amountDetail?: string | null;
  pickLine?: string | null;
  /** Pre-formatted money string for template. */
  balanceAfter?: string | null;
  when: string;
  matchup: AuditMatchupLine | null;
  outcomeTone?: WagerOutcomeTone;
}

export function indexAuditByWagerId(entries: WagerAuditEntry[]): Map<number, WagerAuditEntry[]> {
  const m = new Map<number, WagerAuditEntry[]>();
  for (const e of entries) {
    if (e.wagerId != null && e.wagerId > 0) {
      const list = m.get(e.wagerId) ?? [];
      list.push(e);
      m.set(e.wagerId, list);
    }
  }
  return m;
}

function latestAuditRow(rows: WagerAuditEntry[], actions?: string[]): WagerAuditEntry | undefined {
  const filtered = actions?.length ? rows.filter((e) => actions.includes(e.action)) : rows;
  if (filtered.length === 0) {
    return undefined;
  }
  return [...filtered].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  )[0];
}

function formatBalanceAfter(e: WagerAuditEntry | undefined): string | null {
  if (e?.balanceAfter == null || Number.isNaN(e.balanceAfter)) {
    return null;
  }
  return formatWagerAuditMoney(e.balanceAfter);
}

function outcomeToneForAction(action: string): WagerOutcomeTone {
  switch (action) {
    case 'SettleWagerWin':
      return 'won';
    case 'SettleWagerLose':
      return 'lost';
    case 'VoidWager':
      return 'void';
    case 'CancelWager':
    case 'AdminCancelWager':
      return 'cancelled';
    default:
      return null;
  }
}

function settleActionForWagerStatus(status: MyWager['status']): string {
  switch (status) {
    case 'Won':
      return 'SettleWagerWin';
    case 'Lost':
      return 'SettleWagerLose';
    case 'Void':
      return 'VoidWager';
    case 'Cancelled':
      return 'CancelWager';
    default:
      return 'SettleWagerWin';
  }
}

export function buildAuditMatchupLineFromWager(
  w: MyWager,
  gamesById: Map<number, AuditGameInfo>,
  mode: WagerHistoryLinkMode
): AuditMatchupLine | null {
  const stub: WagerAuditEntry = {
    auditId: 0,
    tournamentId: w.tournamentId,
    targetPlayerId: w.playerId,
    actorPlayerId: null,
    action: 'PlaceWager',
    wagerId: w.wagerId,
    gameResultId: w.gameResultId,
    amount: null,
    balanceBefore: null,
    balanceAfter: null,
    createdAt: w.createdAt
  };
  return buildAuditMatchupLine(stub, indexWagersById([w]), gamesById, mode);
}

export function buildViewFromAuditEntry(
  e: WagerAuditEntry,
  wagersById: Map<number, MyWager>,
  gamesById: Map<number, AuditGameInfo>,
  linkMode: WagerHistoryLinkMode
): WagerHistoryCardView {
  const w = e.wagerId != null && e.wagerId > 0 ? wagersById.get(e.wagerId) : undefined;
  const matchup = buildAuditMatchupLine(e, wagersById, gamesById, linkMode);

  if (e.action === 'PlaceWager') {
    return {
      kind: 'place',
      action: e.action,
      placeHeadline: auditPlaceWagerHeadline(e, w),
      pickLine: auditPickDescription(w),
      balanceAfter: formatBalanceAfter(e),
      when: e.createdAt,
      matchup,
      outcomeTone: null
    };
  }

  if (
    isAuditSettleOutcome(e.action) ||
    e.action === 'VoidWager' ||
    e.action === 'CancelWager' ||
    e.action === 'AdminCancelWager'
  ) {
    return {
      kind: 'settle',
      action: e.action,
      amountDetail: wagerAuditAmountDetail(e),
      pickLine: null,
      balanceAfter: formatBalanceAfter(e),
      when: e.createdAt,
      matchup,
      outcomeTone: outcomeToneForAction(e.action)
    };
  }

  return {
    kind: 'other',
    action: e.action,
    amountDetail: wagerAuditAmountDetail(e),
    pickLine: null,
    balanceAfter: formatBalanceAfter(e),
    when: e.createdAt,
    matchup,
    outcomeTone: null
  };
}

/** My Wagers: open = place layout; settled = single settle-style card (spec A). */
export function buildViewFromMyWager(
  w: MyWager,
  auditByWagerId: Map<number, WagerAuditEntry[]>,
  wagersById: Map<number, MyWager>,
  gamesById: Map<number, AuditGameInfo>,
  linkMode: WagerHistoryLinkMode
): WagerHistoryCardView {
  const rows = auditByWagerId.get(w.wagerId) ?? [];
  const placeEntry = latestAuditRow(rows, ['PlaceWager']);
  const matchup = buildAuditMatchupLineFromWager(w, gamesById, linkMode);

  if (w.status === 'Pending') {
    const placeAudit =
      placeEntry ??
      ({
        auditId: 0,
        tournamentId: w.tournamentId,
        targetPlayerId: w.playerId,
        actorPlayerId: null,
        action: 'PlaceWager',
        wagerId: w.wagerId,
        gameResultId: w.gameResultId,
        amount: w.stakeAmount,
        balanceBefore: null,
        balanceAfter: null,
        createdAt: w.createdAt
      } satisfies WagerAuditEntry);

    return {
      kind: 'place',
      action: 'PlaceWager',
      placeHeadline: auditPlaceWagerHeadline(placeAudit, w),
      pickLine: auditPickDescription(w),
      balanceAfter: formatBalanceAfter(placeEntry),
      when: placeEntry?.createdAt ?? w.createdAt,
      matchup,
      outcomeTone: null
    };
  }

  const settleActions = [
    'SettleWagerWin',
    'SettleWagerLose',
    'VoidWager',
    'CancelWager',
    'AdminCancelWager'
  ];
  let settleEntry = latestAuditRow(rows, settleActions);
  const action = settleEntry?.action ?? settleActionForWagerStatus(w.status);

  if (!settleEntry) {
    settleEntry = {
      auditId: 0,
      tournamentId: w.tournamentId,
      targetPlayerId: w.playerId,
      actorPlayerId: null,
      action,
      wagerId: w.wagerId,
      gameResultId: w.gameResultId,
      amount: null,
      balanceBefore: null,
      balanceAfter: null,
      createdAt: w.settledAt ?? w.createdAt
    };
  }

  const balanceSource =
    latestAuditRow(rows, settleActions.filter((a) => a !== 'CancelWager' && a !== 'AdminCancelWager')) ??
    settleEntry ??
    placeEntry;

  return {
    kind: 'settle',
    action: settleEntry.action,
    amountDetail: wagerAuditAmountDetail(settleEntry),
    pickLine: auditPickDescription(w),
    balanceAfter: formatBalanceAfter(balanceSource),
    when: settleEntry.createdAt ?? w.settledAt ?? w.createdAt,
    matchup,
    outcomeTone: outcomeToneForAction(settleEntry.action)
  };
}

/** Summary for audit scope (single tournament or all). */
export function buildAuditScopeSummary(
  entries: WagerAuditEntry[],
  scopeLabel: string
): { scopeLabel: string; wins: number; losses: number; netAmount: number } {
  const wins = entries.filter((e) => e.action === 'SettleWagerWin').length;
  const losses = entries.filter((e) => e.action === 'SettleWagerLose').length;
  const withBalance = entries
    .filter((e) => e.balanceAfter != null && !Number.isNaN(e.balanceAfter))
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
  const netAmount = withBalance[0]?.balanceAfter ?? 0;
  return { scopeLabel, wins, losses, netAmount };
}
