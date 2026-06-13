using System.Globalization;

namespace pax.BlazorChess.Board;

public enum AnalysisMoveQuality
{
    Blunder,
    Mistake,
    Inaccuracy,
    Good,
    Best
}

public static class AnalysisEvaluationMetrics
{
    public const double MateDisplayScore = 10;

    public static double GetDisplayScore(int score, int? mate)
    {
        if (mate is { } mateValue && mateValue != 0)
            return mateValue > 0 ? MateDisplayScore : -MateDisplayScore;

        return MateDisplayScore * Math.Tanh(score / 500.0);
    }

    public static double GetWinningChance(int score, int? mate)
    {
        if (mate is { } mateValue && mateValue != 0)
            return mateValue > 0 ? 100 : 0;

        return 50 + 50 * ((2 / (1 + Math.Exp(-0.004 * score))) - 1);
    }

    public static string GetRawScoreText(int score, int? mate)
    {
        if (mate is { } mateValue && mateValue != 0)
            return string.Create(CultureInfo.InvariantCulture, $"M{mateValue:+0;-0}");

        return (score / 100.0).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
    }

    public static AnalysisMoveQuality ClassifyLoss(double winningChanceLoss)
        => winningChanceLoss >= 30 ? AnalysisMoveQuality.Blunder
            : winningChanceLoss >= 20 ? AnalysisMoveQuality.Mistake
            : winningChanceLoss >= 10 ? AnalysisMoveQuality.Inaccuracy
            : winningChanceLoss >= 3 ? AnalysisMoveQuality.Good
            : AnalysisMoveQuality.Best;

    public static AnalysisMoveQuality ClassifyMoveQuality(
        int moveNumber,
        double previousWinningChance,
        double currentWinningChance)
    {
        var loss = moveNumber % 2 != 0
            ? previousWinningChance - currentWinningChance
            : currentWinningChance - previousWinningChance;

        return ClassifyLoss(loss);
    }

    public static double?[] BuildAverageDisplayScores(
        IReadOnlyList<IReadOnlyList<double?>> engineDisplayScores,
        int moveCount)
    {
        ArgumentNullException.ThrowIfNull(engineDisplayScores);

        var averages = new double?[Math.Max(0, moveCount)];
        for (var moveIndex = 0; moveIndex < averages.Length; moveIndex++)
        {
            double sum = 0;
            var count = 0;
            foreach (var engineScores in engineDisplayScores)
            {
                if (moveIndex >= engineScores.Count || engineScores[moveIndex] is not { } score)
                    continue;

                sum += score;
                count++;
            }

            averages[moveIndex] = count == 0 ? null : sum / count;
        }

        return averages;
    }

    public static List<double?[]> BuildAdjustedDisplayScores(
        IReadOnlyList<IReadOnlyList<double?>> engineDisplayScores,
        int moveCount)
    {
        ArgumentNullException.ThrowIfNull(engineDisplayScores);

        var averages = BuildAverageDisplayScores(engineDisplayScores, moveCount);
        List<double?[]> adjusted = new(engineDisplayScores.Count);

        foreach (var engineScores in engineDisplayScores)
        {
            var line = new double?[averages.Length];
            for (var moveIndex = 0; moveIndex < averages.Length; moveIndex++)
            {
                if (moveIndex >= engineScores.Count
                    || engineScores[moveIndex] is not { } score
                    || averages[moveIndex] is not { } average)
                {
                    continue;
                }

                line[moveIndex] = score - average;
            }

            adjusted.Add(line);
        }

        return adjusted;
    }
}
