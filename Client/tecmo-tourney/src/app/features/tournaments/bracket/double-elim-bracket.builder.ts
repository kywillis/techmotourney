import { BracketMatch, FeedRef } from './double-elim-bracket.types';

/** Next power of two in [4, 32]. */
export function bracketSizeForEntrantCount(n: number): number {
  if (n < 4) return 4;
  let b = 4;
  while (b < n && b < 32) b *= 2;
  if (b < n) b = 32;
  return b;
}

/** Classic tournament leaf ordering: seed ranks 0..B-1 at WB round-1 leaves (0 = best). */
export function bracketLeafRanks(size: number): number[] {
  if (size <= 1) return [0];
  const half = size / 2;
  const prev = bracketLeafRanks(half);
  const result: number[] = [];
  for (let i = 0; i < half; i++) {
    result.push(prev[i]);
    result.push(size - 1 - prev[i]);
  }
  return result;
}

function lbRoundMatchCount(B: number, j: number): number {
  if (j === 0) return B / 4;
  if (j % 2 === 1) {
    return B / Math.pow(2, (j + 1) / 2 + 1);
  }
  return B / Math.pow(2, j / 2 + 2);
}

function wbLeafFeed(N: number, B: number, leafIndex: number, leafRanks: number[]): FeedRef {
  const rank = leafRanks[leafIndex];
  if (rank >= N) return { kind: 'bye' };
  return { kind: 'seed', seedSlot: rank };
}

export function buildDoubleEliminationMatches(N: number): { B: number; matches: BracketMatch[] } {
  const B = bracketSizeForEntrantCount(N);
  const k = Math.log2(B);
  const leafRanks = bracketLeafRanks(B);
  const matches: BracketMatch[] = [];

  for (let r = 0; r < k; r++) {
    const count = B / Math.pow(2, r + 1);
    for (let i = 0; i < count; i++) {
      const top: FeedRef =
        r === 0
          ? wbLeafFeed(N, B, 2 * i, leafRanks)
          : { kind: 'winner', matchId: `wb-${r - 1}-${2 * i}` };
      const bottom: FeedRef =
        r === 0
          ? wbLeafFeed(N, B, 2 * i + 1, leafRanks)
          : { kind: 'winner', matchId: `wb-${r - 1}-${2 * i + 1}` };
      matches.push({
        id: `wb-${r}-${i}`,
        segment: 'WB',
        round: r,
        indexInRound: i,
        top,
        bottom
      });
    }
  }

  const lbRounds = 2 * k - 2;
  for (let j = 0; j < lbRounds; j++) {
    const count = lbRoundMatchCount(B, j);
    for (let i = 0; i < count; i++) {
      let top: FeedRef;
      let bottom: FeedRef;
      if (j === 0) {
        top = { kind: 'loser', matchId: `wb-0-${2 * i}` };
        bottom = { kind: 'loser', matchId: `wb-0-${2 * i + 1}` };
      } else if (j % 2 === 1) {
        const wbRound = (j + 1) / 2;
        // Winner of previous LB round match i (not 2*i — e.g. j=1,i=1 must be lb-0-1, not lb-0-2).
        top = { kind: 'winner', matchId: `lb-${j - 1}-${i}` };
        bottom = { kind: 'loser', matchId: `wb-${wbRound}-${i}` };
      } else {
        top = { kind: 'winner', matchId: `lb-${j - 1}-${2 * i}` };
        bottom = { kind: 'winner', matchId: `lb-${j - 1}-${2 * i + 1}` };
      }
      matches.push({
        id: `lb-${j}-${i}`,
        segment: 'LB',
        round: j,
        indexInRound: i,
        top,
        bottom
      });
    }
  }

  matches.push({
    id: 'gf-0-0',
    segment: 'GF',
    round: 0,
    indexInRound: 0,
    top: { kind: 'winner', matchId: `wb-${k - 1}-0` },
    bottom: { kind: 'winner', matchId: `lb-${lbRounds - 1}-0` }
  });

  matches.push({
    id: 'gf-1-0',
    segment: 'GF',
    round: 1,
    indexInRound: 0,
    top: { kind: 'empty' },
    bottom: { kind: 'empty' }
  });

  return { B, matches };
}
