function getAnalysisTooltipPoint(context) {
    return context?.dataset?.tooltipPoints?.[context.dataIndex];
}

const callbacks = Object.freeze({
    analysisTooltipTitle(items) {
        const first = items?.[0];
        const point = first ? getAnalysisTooltipPoint(first) : null;
        const title = point?.title ?? first?.label ?? "";
        return title.split(" / ")[0] ?? title;
    },
    analysisTooltipLabel(context) {
        const point = getAnalysisTooltipPoint(context);
        if (!point) {
            return `${context.dataset.label}: ${context.formattedValue ?? context.raw}`;
        }

        const label = point.engineName || context.dataset.label || "Evaluation";
        const parts = [
            `${label}: ${point.rawScoreText}`,
            point.winningChanceText,
            `chart ${point.displayScoreText}`
        ];

        if (point.differenceText) {
            parts.push(`diff ${point.differenceText}`);
        }

        return parts.join(" | ");
    }
});

export const chartJsCallbacks = callbacks;
