using TecmoTourney;
using TecmoTourney.Models;

namespace TecmoTourney.Bracket;

/// <summary>Server-side mirror of <c>double-elim-bracket.resolve.ts</c>.</summary>
public static class DoubleElimBracketResolver
{
    private const int MaxPasses = 64;

    public static List<BracketParticipant> BuildEntrantsFromStandings(IReadOnlyList<TournamentStandingModel> standings)
    {
        var sorted = standings.OrderBy(s => s.PreliminaryPosition).ToList();
        return sorted.Select(s => new BracketParticipant
        {
            PlayerId = s.PlayerId,
            Name = s.PlayerName,
            Seed = s.PreliminaryPosition
        }).ToList();
    }

    public static (int B, List<ResolvedMatch> Resolved, List<BracketMatch> Matches) ResolveDoubleElimination(
        int entrantCount,
        IReadOnlyList<BracketParticipant> entrants,
        IReadOnlyList<BracketGameSnapshot> tournamentGames)
    {
        var (b, matches) = DoubleElimBracketBuilder.BuildDoubleEliminationMatches(entrantCount);
        var bracketGames = tournamentGames
            .OrderBy(g => g.DateAdded)
            .ThenBy(g => g.GameResultId)
            .ToList();

        var byId = new Dictionary<string, ResolvedMatch>();
        foreach (var def in matches)
        {
            byId[def.Id] = new ResolvedMatch
            {
                Def = def,
                Top = new ResolvedSlot(),
                Bottom = new ResolvedSlot(),
                GameResultId = null,
                Status = null,
                TopScore = null,
                BottomScore = null,
                WinnerId = null,
                IsPending = false,
                TopSourceLabel = null,
                BottomSourceLabel = null,
                WbMatchLabel = null
            };
        }

        ApplyWbAndLbSourceLabels(matches, byId);

        var gamesById = bracketGames.ToDictionary(g => g.GameResultId);
        var (canWin, canLose) = PrecomputeStructuralCapabilities(matches, entrantCount);

        void FullPropagate()
        {
            for (var pass = 0; pass < MaxPasses; pass++)
            {
                var changed = false;
                foreach (var def in matches)
                {
                    if (def.Id == "gf-1-0")
                        continue;
                    var rm = byId[def.Id];
                    var top = ResolveFeedToSlot(def.Top, byId, entrants);
                    var bottom = ResolveFeedToSlot(def.Bottom, byId, entrants);
                    if (!SlotEquals(rm.Top, top))
                    {
                        rm.Top = top;
                        changed = true;
                    }

                    if (!SlotEquals(rm.Bottom, bottom))
                    {
                        rm.Bottom = bottom;
                        changed = true;
                    }

                    var auto = StructuralAutoWinner(rm.Top, rm.Bottom, def, canWin, canLose, entrantCount);
                    if (auto != null && rm.WinnerId != auto)
                    {
                        rm.WinnerId = auto;
                        changed = true;
                    }

                    // No bound game: two human players cannot have a declared winner until a game exists
                    // (clears stale sole-advance WinnerId when the second feeder arrives).
                    var h1 = rm.Top.Participant?.PlayerId;
                    var h2 = rm.Bottom.Participant?.PlayerId;
                    if (rm.GameResultId == null &&
                        h1 != null &&
                        h2 != null &&
                        h1 != h2 &&
                        !rm.Top.IsBye &&
                        !rm.Bottom.IsBye &&
                        rm.WinnerId != null)
                    {
                        rm.WinnerId = null;
                        changed = true;
                    }
                }

                foreach (var def in matches)
                {
                    var rm = byId[def.Id];
                    if (rm.GameResultId == null)
                        continue;
                    if (!gamesById.TryGetValue(rm.GameResultId.Value, out var g))
                        continue;
                    rm.Status = g.Status;
                    var pTop = rm.Top.Participant?.PlayerId;
                    if (pTop != null)
                    {
                        rm.TopScore = pTop == g.Player1Id ? g.Player1Score : g.Player2Score;
                        rm.BottomScore = pTop == g.Player1Id ? g.Player2Score : g.Player1Score;
                    }
                    else
                    {
                        rm.TopScore = null;
                        rm.BottomScore = null;
                    }

                    if (g.Status == GameStatus.Completed)
                    {
                        var w = WinnerFromCompletedGame(g);
                        if (w != null && rm.WinnerId != w)
                        {
                            rm.WinnerId = w;
                            changed = true;
                        }
                    }
                    else if (g.Status == GameStatus.Waiting || g.Status == GameStatus.InProgress)
                    {
                        var structural = StructuralAutoWinner(rm.Top, rm.Bottom, def, canWin, canLose, entrantCount);
                        if (structural != null)
                        {
                            if (rm.WinnerId != structural)
                            {
                                rm.WinnerId = structural;
                                changed = true;
                            }
                        }
                        else if (rm.WinnerId != null)
                        {
                            rm.WinnerId = null;
                            changed = true;
                        }
                    }
                }

                if (!changed)
                    break;
            }
        }

        FullPropagate();

        var used = new HashSet<int>();

        foreach (var g in bracketGames)
        {
            if (used.Contains(g.GameResultId))
                continue;
            FullPropagate();
            var candidates = new List<(BracketMatch Def, ResolvedMatch Rm, int[] Pri)>();
            foreach (var def in matches)
            {
                if (def.Id == "gf-1-0")
                    continue;
                var rm = byId[def.Id];
                if (rm.GameResultId != null)
                    continue;
                var p1 = rm.Top.Participant?.PlayerId;
                var p2 = rm.Bottom.Participant?.PlayerId;
                if (p1 == null || p2 == null || p1 == p2)
                    continue;
                if (!SamePair(p1.Value, p2.Value, g.Player1Id, g.Player2Id))
                    continue;
                candidates.Add((def, rm, BracketPriority(def)));
            }

            candidates.Sort((a, b) => CmpPri(a.Pri, b.Pri));
            if (candidates.Count > 0)
            {
                var pick = candidates[0];
                pick.Rm.GameResultId = g.GameResultId;
                used.Add(g.GameResultId);
            }

            FullPropagate();
        }

        var gf0 = byId["gf-0-0"];
        var gf1 = byId["gf-1-0"];

        var wbFinalTop = gf0.Def.Top;
        var lbFinalBot = gf0.Def.Bottom;
        int? wbChampPlayerId = null;
        int? lbChampPlayerId = null;
        if (wbFinalTop.Kind == FeedKind.Winner && wbFinalTop.MatchId != null)
            wbChampPlayerId = byId[wbFinalTop.MatchId].WinnerId;
        if (lbFinalBot.Kind == FeedKind.Winner && lbFinalBot.MatchId != null)
            lbChampPlayerId = byId[lbFinalBot.MatchId].WinnerId;

        var showReset = false;
        if (gf0.Status == GameStatus.Completed &&
            gf0.WinnerId != null &&
            lbChampPlayerId != null &&
            gf0.WinnerId == lbChampPlayerId)
        {
            showReset = true;
            var pTop = gf0.Top.Participant;
            var pBot = gf0.Bottom.Participant;
            if (pTop != null && pBot != null)
            {
                gf1.Top = new ResolvedSlot { Participant = pTop };
                gf1.Bottom = new ResolvedSlot { Participant = pBot };
                FullPropagate();
                foreach (var g in bracketGames)
                {
                    if (used.Contains(g.GameResultId))
                        continue;
                    if (!SamePair(g.Player1Id, g.Player2Id, pTop.PlayerId, pBot.PlayerId))
                        continue;
                    gf1.GameResultId = g.GameResultId;
                    used.Add(g.GameResultId);
                    break;
                }

                FullPropagate();
                if (gf1.GameResultId == null)
                    gf1.IsPending = true;
            }
        }

        if (!showReset)
        {
            gf1.Top = new ResolvedSlot();
            gf1.Bottom = new ResolvedSlot();
            gf1.GameResultId = null;
            gf1.Status = null;
            gf1.TopScore = null;
            gf1.BottomScore = null;
            gf1.WinnerId = null;
            gf1.IsPending = false;
        }

        foreach (var def in matches)
        {
            var rm = byId[def.Id];
            var p1 = rm.Top.Participant?.PlayerId;
            var p2 = rm.Bottom.Participant?.PlayerId;
            if (p1 != null && p2 != null && p1 != p2)
                rm.IsPending = rm.GameResultId == null && rm.WinnerId == null;
            else
                rm.IsPending = false;
        }

        var ordered = matches
            .Where(m => m.Id != "gf-1-0" || showReset)
            .Select(m => byId[m.Id])
            .ToList();

        return (b, ordered, matches);
    }

    private static void ApplyWbAndLbSourceLabels(List<BracketMatch> matches, Dictionary<string, ResolvedMatch> byId)
    {
        var wbOrd = BuildWbGameOrdinalMap(matches);
        foreach (var def in matches)
        {
            var rm = byId[def.Id];
            if (def.Segment == "WB")
            {
                rm.WbMatchLabel = wbOrd.TryGetValue(def.Id, out var n) ? $"WB{n}" : null;
            }
            else if (def.Segment == "LB")
            {
                rm.TopSourceLabel = WbLoserSlotLabel(def.Top, wbOrd);
                rm.BottomSourceLabel = WbLoserSlotLabel(def.Bottom, wbOrd);
            }
        }
    }

    private static Dictionary<string, int> BuildWbGameOrdinalMap(List<BracketMatch> matches)
    {
        var wb = matches
            .Where(m => m.Segment == "WB")
            .OrderBy(m => m.Round)
            .ThenBy(m => m.IndexInRound)
            .ToList();
        var map = new Dictionary<string, int>();
        for (var i = 0; i < wb.Count; i++)
            map[wb[i].Id] = i + 1;
        return map;
    }

    private static string? WbLoserSlotLabel(FeedRef feed, Dictionary<string, int> wbOrd)
    {
        if (feed.Kind != FeedKind.Loser || feed.MatchId == null || !feed.MatchId.StartsWith("wb-", StringComparison.Ordinal))
            return null;
        return wbOrd.TryGetValue(feed.MatchId, out var n) ? $"WB{n}" : null;
    }

    public static bool SamePair(int a, int b, int p1, int p2) =>
        (a == p1 && b == p2) || (a == p2 && b == p1);

    private static ResolvedSlot SlotFromParticipant(BracketParticipant? p, bool isBye) =>
        new() { Participant = p, IsBye = isBye };

    private static BracketParticipant? ParticipantById(IReadOnlyList<BracketParticipant> ents, int? id)
    {
        if (id == null)
            return null;
        return ents.FirstOrDefault(e => e.PlayerId == id);
    }

    private static ResolvedSlot ResolveFeedToSlot(
        FeedRef feed,
        Dictionary<string, ResolvedMatch> byId,
        IReadOnlyList<BracketParticipant> entrants)
    {
        if (feed.Kind == FeedKind.Seed && feed.SeedSlot != null)
        {
            var slot = feed.SeedSlot.Value;
            if (slot >= 0 && slot < entrants.Count)
                return SlotFromParticipant(entrants[slot], false);
            return new ResolvedSlot { IsBye = true };
        }

        if (feed.Kind == FeedKind.Bye)
            return new ResolvedSlot { IsBye = true };
        if (feed.Kind == FeedKind.Empty)
            return new ResolvedSlot();
        if ((feed.Kind == FeedKind.Winner || feed.Kind == FeedKind.Loser) && feed.MatchId != null)
        {
            if (!byId.TryGetValue(feed.MatchId, out var m))
                return new ResolvedSlot();
            var wid = m.WinnerId;
            if (wid == null)
                return new ResolvedSlot();
            var topId = m.Top.Participant?.PlayerId;
            var botId = m.Bottom.Participant?.PlayerId;
            int? loseId = topId != null && botId != null
                ? (wid == topId ? botId : topId)
                : null;
            var id = feed.Kind == FeedKind.Winner ? wid : loseId;
            if (id == null)
                return new ResolvedSlot();
            return SlotFromParticipant(ParticipantById(entrants, id), false);
        }

        return new ResolvedSlot();
    }

    private static int? ByeWinner(ResolvedSlot top, ResolvedSlot bottom)
    {
        if (top.IsBye && bottom.Participant != null)
            return bottom.Participant.PlayerId;
        if (bottom.IsBye && top.Participant != null)
            return top.Participant.PlayerId;
        return null;
    }

    /// <summary>Structural bye slot: explicit bye or seed past entrant count N (matches builder wbLeafFeed).</summary>
    private static bool FeedIsStructuralBye(FeedRef feed, int entrantCount)
    {
        if (feed.Kind == FeedKind.Bye)
            return true;
        if (feed.Kind == FeedKind.Seed && feed.SeedSlot != null)
            return feed.SeedSlot.Value >= entrantCount;
        return false;
    }

    /// <summary>
    /// True if this feed can eventually supply a human to <see cref="ResolveFeedToSlot"/>,
    /// independent of game rows — detects feeders that can never fill.
    /// </summary>
    private static bool FeedCanEventuallyProduceHuman(
        FeedRef feed,
        Dictionary<string, bool> canWin,
        Dictionary<string, bool> canLose,
        int entrantCount)
    {
        if (feed.Kind == FeedKind.Seed && feed.SeedSlot != null)
            return feed.SeedSlot.Value < entrantCount;
        if (feed.Kind == FeedKind.Bye)
            return false;
        if (feed.Kind == FeedKind.Empty)
            return false;
        if (feed.Kind == FeedKind.Winner && feed.MatchId != null)
            return canWin.TryGetValue(feed.MatchId, out var w) && w;
        if (feed.Kind == FeedKind.Loser && feed.MatchId != null)
            return canLose.TryGetValue(feed.MatchId, out var l) && l;
        return false;
    }

    /// <summary>
    /// Precompute whether each match can ever declare a winner, and each WB match can ever declare a loser,
    /// in dependency order (WB rounds → LB rounds → GF). Mirrors <c>precomputeStructuralCapabilities</c> in TS.
    /// </summary>
    private static (Dictionary<string, bool> CanWin, Dictionary<string, bool> CanLose) PrecomputeStructuralCapabilities(
        List<BracketMatch> matches,
        int entrantCount)
    {
        var canWin = new Dictionary<string, bool>();
        var canLose = new Dictionary<string, bool>();

        var wb = matches.Where(m => m.Segment == "WB")
            .OrderBy(m => m.Round)
            .ThenBy(m => m.IndexInRound)
            .ToList();
        var lb = matches.Where(m => m.Segment == "LB")
            .OrderBy(m => m.Round)
            .ThenBy(m => m.IndexInRound)
            .ToList();
        var gf = matches.Where(m => m.Segment == "GF")
            .OrderBy(m => m.Round)
            .ThenBy(m => m.IndexInRound)
            .ToList();

        foreach (var def in wb)
        {
            var tH = FeedCanEventuallyProduceHuman(def.Top, canWin, canLose, entrantCount);
            var bH = FeedCanEventuallyProduceHuman(def.Bottom, canWin, canLose, entrantCount);
            var tB = FeedIsStructuralBye(def.Top, entrantCount);
            var bB = FeedIsStructuralBye(def.Bottom, entrantCount);
            var win = (tH && bH) || (tH && bB) || (tB && bH);
            canWin[def.Id] = win;
            canLose[def.Id] = tH && bH;
        }

        foreach (var def in lb)
        {
            var ta = FeedCanEventuallyProduceHuman(def.Top, canWin, canLose, entrantCount);
            var tb = FeedCanEventuallyProduceHuman(def.Bottom, canWin, canLose, entrantCount);
            canWin[def.Id] = ta || tb;
        }

        foreach (var def in gf)
        {
            if (def.Id == "gf-1-0")
            {
                canWin[def.Id] = false;
                continue;
            }

            var ta = FeedCanEventuallyProduceHuman(def.Top, canWin, canLose, entrantCount);
            var tb = FeedCanEventuallyProduceHuman(def.Bottom, canWin, canLose, entrantCount);
            canWin[def.Id] = ta || tb;
        }

        return (canWin, canLose);
    }

    private static bool FeedCanNeverProduceParticipant(
        FeedRef feed,
        Dictionary<string, bool> canWin,
        Dictionary<string, bool> canLose,
        int entrantCount) =>
        !FeedCanEventuallyProduceHuman(feed, canWin, canLose, entrantCount);

    /// <summary>
    /// One side has a player, the other slot is still empty, and the empty side's feeder can never
    /// supply anyone — advance the lone player (all LB rounds + first grand final).
    /// </summary>
    private static int? SoleBracketAdvanceFromUnreachableFeed(
        ResolvedSlot top,
        ResolvedSlot bottom,
        BracketMatch def,
        Dictionary<string, bool> canWin,
        Dictionary<string, bool> canLose,
        int entrantCount)
    {
        if (!(def.Segment == "LB" || (def.Segment == "GF" && def.Id == "gf-0-0")))
            return null;
        if (top.IsBye || bottom.IsBye)
            return null;
        var t = top.Participant?.PlayerId;
        var b = bottom.Participant?.PlayerId;
        if (t != null && b != null)
            return null;
        if (t != null && b == null && FeedCanNeverProduceParticipant(def.Bottom, canWin, canLose, entrantCount))
            return t;
        if (t == null && b != null && FeedCanNeverProduceParticipant(def.Top, canWin, canLose, entrantCount))
            return b;
        return null;
    }

    private static int? StructuralAutoWinner(
        ResolvedSlot top,
        ResolvedSlot bottom,
        BracketMatch def,
        Dictionary<string, bool> canWin,
        Dictionary<string, bool> canLose,
        int entrantCount)
    {
        var bw = ByeWinner(top, bottom);
        if (bw != null)
            return bw;
        return SoleBracketAdvanceFromUnreachableFeed(top, bottom, def, canWin, canLose, entrantCount);
    }

    private static int? WinnerFromCompletedGame(BracketGameSnapshot g)
    {
        if (g.Status != GameStatus.Completed)
            return null;
        if (g.Player1Score > g.Player2Score)
            return g.Player1Id;
        if (g.Player2Score > g.Player1Score)
            return g.Player2Id;
        return null;
    }

    private static int[] BracketPriority(BracketMatch def)
    {
        if (def.Segment == "WB")
            return new[] { 0, def.Round, def.IndexInRound, 0 };
        if (def.Segment == "LB")
            return new[] { 1, def.Round, def.IndexInRound, 0 };
        return new[] { 2, def.Round, def.IndexInRound, 0 };
    }

    private static int CmpPri(int[] a, int[] b)
    {
        for (var i = 0; i < 4; i++)
        {
            var d = a[i] - b[i];
            if (d != 0)
                return d;
        }

        return 0;
    }

    private static bool SlotEquals(ResolvedSlot a, ResolvedSlot b) =>
        a.IsBye == b.IsBye && a.Participant?.PlayerId == b.Participant?.PlayerId;

    /// <summary>True if tournament should use legacy jQuery bracket (non-empty bracket JSON).</summary>
    public static bool TournamentUsesLegacyJqueryBracket(string? bracketData)
    {
        if (string.IsNullOrWhiteSpace(bracketData))
            return false;
        var t = bracketData.Trim();
        if (t == "{}" || t == "[]")
            return false;
        return true;
    }
}
