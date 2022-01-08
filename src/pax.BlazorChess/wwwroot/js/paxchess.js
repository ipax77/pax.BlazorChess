window.addEventListener("wheel", e => e.preventDefault(), { passive: false })
var blazorModal = null;

function getElementSize(elementId) {
    var element = document.getElementById(elementId);
    if (element != null) {
        var size = element.getBoundingClientRect();
        return [size.x, size.y, size.width, size.height];
    }
};

function reportSize() {
    var size = getElementSize('board');
    window.Dotnet.invokeMethodAsync('ResizeEvent', size);
}

function resizeEvents(ref, toggle) {
    if (toggle) {
        window.Dotnet = ref;
        window.onresize = reportSize;
    }
    else {
        window.Dotnet = null;
        window.onresize = null;
    }
}

function modalClose() {
    if (blazorModal) {
        blazorModal.hide();
    }
}

function modalOpen(name) {

    const modalElement = document.getElementById(name);

    if (!modalElement) {
        return;
    }

    blazorModal = new bootstrap.Modal(modalElement);
    blazorModal.show();
}

function promoteModalOpen(ref) {
    promoteRef = ref;
    const modalElement = document.getElementById('promoteModal');
    if (!modalElement) {
        return;
    }

    Modal = new bootstrap.Modal(modalElement);
    Modal.show();
    modalElement.addEventListener('hide.bs.modal', function () {
        promoteRef.invokeMethodAsync('PromoteClose');
        this.removeEventListener('hide.bs.modal', arguments.callee);
        promoteRef = null;
    });
}

function drawChart(id, chart, ref) {
    window.DotNetRef = ref;

    const arbitraryLine = {
        id: 'arbitraryLine',
        // beforeDraw(chart, args, options) {
        afterDraw(chart, args, options) {
            const { ctx, chartArea: { top, right, bottom, left, width, height }, scales: { x, y } } = chart;

            ctx.save();

            ctx.fillStyle = options.arbitraryLineColor;
            const xWidth = options.xWidth;
            x0 = x.getPixelForValue(options.xPosition) - (xWidth / 2);
            y0 = top;
            x1 = xWidth;
            y1 = height;

            ctx.fillRect(x0, y0, x1, y1);

            ctx.restore();
        }
    }

    const config = {
        type: chart.type,
        data: chart.data,
        options: chart.options,
        plugins: [arbitraryLine]
    };

    config.options.onClick = (e) => {
        const points = window.myChart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, true);

        if (points.length) {
            const firstPoint = points[0];
            const label = window.myChart.data.labels[firstPoint.index];
            const value = window.myChart.data.datasets[firstPoint.datasetIndex].data[firstPoint.index];
            reportChartClick(label);
        }
    }
    if (window.myChart) {
        window.myChart.clear();
    }
    window.myChart = new Chart(document.getElementById(id), config);
}

function updateChartLables(labels) {
    window.myChart.config.data.labels = labels;
    window.myChart.update();
}

function updateChartDataset(dataset) {
    window.myChart.config.data.datasets[0] = dataset;
    window.myChart.update();
}

function drawChart2(id, labels, data, ref) {
    window.DotNetRef = ref;

    const arbitraryLine = {
        id: 'arbitraryLine',
        // beforeDraw(chart, args, options) {
        afterDraw(chart, args, options) {
            const { ctx, chartArea: { top, right, bottom, left, width, height }, scales: { x, y } } = chart;

            ctx.save();

            ctx.fillStyle = options.arbitraryLineColor;
            const xWidth = options.xWidth;
            x0 = x.getPixelForValue(options.xPosition) - (xWidth / 2);
            y0 = top;
            x1 = xWidth;
            y1 = height;

            ctx.fillRect(x0, y0, x1, y1);

            ctx.restore();
        }

    }

    const ctx = document.getElementById(id).getContext('2d');
    window.myChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Rating',
                data: data,
                backgroundColor: 'white',
                borderColor: '#ffffff',
                fill: true,
                pointBackgroundColor: 'white',
                pointBorderColor: 'yellow',
                pointRadius: 3,
                pointBorderWidth: 2,
                pointHitRadius: 5,
                tension: 0.4
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    position: 'top',
                },
                title: {
                    display: true,
                    text: 'Game Evaluation',
                    color: 'yellow',
                    font: {
                        size: 16
                    }
                },
                arbitraryLine: {
                    arbitraryLineColor: 'blue',
                    xPosition: 0,
                    xWidth: 3
                }
            },
            scales: {
                x: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Moves',
                        color: 'yellow',
                    },
                    ticks: {
                        color: 'yellow',
                        beginAtZero: true
                    },
                    grid: {
                        color: 'grey'
                    }
                },
                y: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Evaluation',
                        color: 'yellow'
                    },
                    ticks: {
                        color: 'yellow',
                        beginAtZero: true
                    },
                    grid: {
                        color: 'grey'
                    }
                }
            },
            onClick: (e) => {
                const points = window.myChart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, true);

                if (points.length) {
                    const firstPoint = points[0];
                    const label = window.myChart.data.labels[firstPoint.index];
                    const value = window.myChart.data.datasets[firstPoint.datasetIndex].data[firstPoint.index];
                    reportChartClick(label);
                }
            }
        },
        plugins: [arbitraryLine]
    });

    var config = JSON.stringify(myChart.config, undefined, 2);
    console.log(config);

}

function drawHorizontalLine(x) {
    window.myChart.options.plugins.arbitraryLine.xPosition = x;
    window.myChart.update();
}

function reportChartClick(point) {
    if (window.DotNetRef) {
        window.DotNetRef.invokeMethodAsync("ChartClick", point);
    }
}

window.SetFocusToElement = (element) => {
    if (element) {
        element.focus();
    }
}

function scrollToElement(id) {
    let element = document.getElementById(id);
    if (element) {
        element.scrollIntoViewIfNeeded();
    }
}