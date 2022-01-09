using pax.BlazorChess.Models;

namespace pax.BlazorChess.Services;

public static class ChartService
{
    public static Chart GetRatingChart()
    {
        Chart chart = new Chart()
        {
            type = "line",
            data = new Data()
            {
                labels = new List<string>(),
                datasets = new List<Dataset>()
                {
                    new Dataset()
                    {
                        label = "Rating",
                        data = new List<double>(),
                        backgroundColor = "white",
                        borderColor = "#ffffff",
                        fill = true,
                        pointBackgroundColor = "white",
                        pointBorderColor = "yellow",
                        pointRadius = 3,
                        pointBorderWidth = 2,
                        pointHitRadius = 5,
                        tension = 0.4
                    }
                }
            },
            options = new Options()
            {
                responsive = true,
                plugins = new Plugins()
                {
                    legend = new Legend()
                    {
                        position = "top"
                    },
                    title = new Title()
                    {
                        display = true,
                        text = "Game Evaluation",
                        color = "yellow",
                        font = new Font()
                        {
                            size = 16
                        }
                    },
                    arbitraryLine = new ArbitraryLine()
                    {
                        arbitraryLineColor = "blue",
                        xPosition = 0,
                        xWidth = 3
                    }
                },
                scales = new Scales()
                {
                    x = new X()
                    {
                        display = true,
                        title = new Title()
                        {
                            display = true,
                            text = "Moves",
                            color = "yellow"
                        },
                        ticks = new Ticks()
                        {
                            color = "yellow",
                            beginAtZero = true
                        },
                        grid = new Grid()
                        {
                            color = "grey"
                        }
                    },
                    y = new Y()
                    {
                        display = true,
                        title = new Title()
                        {
                            display = true,
                            text = "Evaluation",
                            color = "yellow"
                        },
                        ticks = new Ticks()
                        {
                            color = "yellow",
                            beginAtZero = true
                        },
                        grid = new Grid()
                        {
                            color = "grey"
                        }
                    }
                }
            }
        };
        return chart;
    }
}
