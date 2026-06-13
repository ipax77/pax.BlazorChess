using pax.chess;

namespace pax.BlazorChess.Board;

public readonly record struct MoveEvaluationAnnotation(Square Square, AnalysisMoveQuality Quality);
