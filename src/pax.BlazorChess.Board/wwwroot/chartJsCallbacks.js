function getAnalysisTooltipPoint(context) {
    return context?.dataset?.tooltipPoints?.[context.dataIndex];
}

const callbacks = Object.freeze({
    analysisTooltipTitle(items) {
        const first = items?.[0];
        const point = first ? getAnalysisTooltipPoint(first) : null;
        return point?.title ?? first?.label ?? "";
    },
    analysisTooltipLabel(context) {
        const point = getAnalysisTooltipPoint(context);
        if (!point) {
            return `${context.dataset.label}: ${context.formattedValue ?? context.raw}`;
        }

        return `${point.rawScoreText} | ${point.winningChanceText} | chart ${point.displayScoreText}`;
    }
});

export const chartJsCallbacks = callbacks;
