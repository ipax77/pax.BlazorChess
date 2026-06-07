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
}
