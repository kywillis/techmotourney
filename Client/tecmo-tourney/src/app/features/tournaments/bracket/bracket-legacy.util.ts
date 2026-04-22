import { ITournament } from 'src/app/core/models/tournament.model';

/** True when the jQuery iframe bracket should be used (historical tournaments with saved bracket JSON). */
export function tournamentUsesLegacyJqueryBracket(tournament: ITournament): boolean {
  const d = tournament.bracketData;
  if (d == null || d === '') return false;
  if (typeof d === 'object' && !Array.isArray(d) && Object.keys(d as object).length === 0) {
    return false;
  }
  if (Array.isArray(d) && d.length === 0) return false;
  return true;
}

export function tournamentHasBracketImage(tournament: ITournament): boolean {
  return tournament.bracketImage != null && String(tournament.bracketImage).trim() !== '';
}
