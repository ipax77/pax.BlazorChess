using pax.BlazorChartJs;
using pax.BlazorChess.Models;

namespace pax.BlazorChess.Services;

public static class ChartService
{
    public static ChartJsConfig GetRatingChart()
    {
        return new()
        {
            Type = ChartType.line,
            Data = new ChartJsData()
            {
                Labels = new List<string>(),
                Datasets = new List<ChartJsDataset>()
                {
                    new LineDataset()
                    {
                        Label = "Rating",
                        BackgroundColor = "white",
                        BorderColor = "#ffffff",
                        Fill = new
                            {
                                target = "origin",
                                above = "rgb(255, 255, 255, 0.6)",
                                below = "rgb(0, 0, 0, 0.6)"
                            },
                        PointBackgroundColor = new IndexableOption<string>("white"),
                        PointBorderColor = new IndexableOption<string>("yellow"),
                        PointRadius = new IndexableOption<double>(3),
                        PointBorderWidth = new IndexableOption<double>(2),
                        PointHitRadius = new IndexableOption<double>(5),
                        Tension = 0.4
                    }
                }
            },
            Options = new ChartJsOptions()
            {
                Responsive = true,
                OnClickEvent = true,
                Plugins = new Plugins()
                {
                    Legend = new Legend()
                    {
                        Position = "top"
                    },
                    Title = new Title()
                    {
                        Display = true,
                        Text = new IndexableOption<string>("Game Evaluation"),
                        Color = "yellow",
                        Font = new Font()
                        {
                            Size = 16
                        }
                    },
                    ArbitraryLines = new List<ArbitraryLineConfig>()
                    {
                        new ArbitraryLineConfig()
                        {
                            ArbitraryLineColor = "blue",
                            XPosition = 0,
                            XWidth = 3,
                            Text = "Current"
                        }
                    }
                },
                Scales = new ChartJsOptionsScales()
                {
                    X = new LinearAxis()
                    {
                        Display = true,
                        Title = new Title()
                        {
                            Display = true,
                            Text = new IndexableOption<string>("Moves"),
                            Color = "yellow"
                        },
                        Ticks = new LinearAxisTick()
                        {
                            Color = "yellow",
                        },
                        Grid = new ChartJsGrid()
                        {
                            Color = "grey"
                        }
                    },
                    Y = new LinearAxis()
                    {
                        Display = true,
                        BeginAtZero = true,
                        Title = new Title()
                        {
                            Display = true,
                            Text = new IndexableOption<string>("Evaluation"),
                            Color = "yellow"
                        },
                        Ticks = new LinearAxisTick()
                        {
                            Color = "yellow"
                        },
                        Grid = new ChartJsGrid()
                        {
                            Color = "grey"
                        }
                    }
                }
            }
        };
    }
}
