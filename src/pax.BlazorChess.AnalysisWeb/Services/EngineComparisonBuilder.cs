namespace pax.BlazorChess.AnalysisWeb.Services;

public static class EngineComparisonBuilder
{
    public static EngineComparisonSnapshot Build(IReadOnlyList<EngineAnalysisSnapshot> engines)
    {
        ArgumentNullException.ThrowIfNull(engines);

        Dictionary<string, CandidateAccumulator> candidates = new(StringComparer.Ordinal);
        Dictionary<string, int> bestMoveCounts = new(StringComparer.Ordinal);

        foreach (var engine in engines)
        {
            foreach (var line in engine.Lines.OrderBy(l => l.MultiPv))
            {
                var firstMove = line.Moves.FirstOrDefault();
                if (firstMove is null)
                    continue;

                var rank = line.MultiPv <= 1 ? 1 : line.MultiPv;
                if (!candidates.TryGetValue(firstMove.Uci, out var candidate))
                {
                    candidate = new CandidateAccumulator(firstMove.Uci, firstMove.San);
                    candidates.Add(firstMove.Uci, candidate);
                }

                if (!candidate.Cells.ContainsKey(engine.EngineId))
                {
                    candidate.Cells.Add(
                        engine.EngineId,
                        new CandidateMoveCell(line.ScoreText, FormatRank(rank), rank));
                }

                if (rank == 1)
                    bestMoveCounts[firstMove.Uci] = bestMoveCounts.GetValueOrDefault(firstMove.Uci) + 1;
            }
        }

        var rows = candidates.Values
            .OrderByDescending(c => c.Cells.Count(cell => cell.Value.Rank == 1))
            .ThenBy(c => c.Cells.Values.Min(cell => cell.Rank))
            .ThenBy(c => c.San, StringComparer.Ordinal)
            .Select(c => new CandidateMoveRow(c.Uci, c.San, c.Cells))
            .ToList();

        var consensus = bestMoveCounts.Count == 0
            ? string.Empty
            : bestMoveCounts
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => rows.FindIndex(r => r.Uci == kvp.Key))
                .Select(kvp => rows.FirstOrDefault(r => r.Uci == kvp.Key)?.San ?? kvp.Key)
                .First();

        return new EngineComparisonSnapshot(
            rows,
            consensus,
            bestMoveCounts.Count > 1);
    }

    private static string FormatRank(int rank)
        => rank <= 1 ? "best" : $"#{rank}";

    private sealed record CandidateAccumulator(string Uci, string San)
    {
        public Dictionary<Guid, CandidateMoveCell> Cells { get; } = [];
    }
}
