namespace pax.BlazorChess.Models;
# nullable disable
using System;
using System.Collections.Generic;

public class Chart
{
    public string type { get; set; } = "line";
    public Data data { get; set; }
    public Options options { get; set; }
}

public class Dataset
{
    public string label { get; set; }
    public List<double> data { get; set; }
    public string backgroundColor { get; set; }
    public string borderColor { get; set; }
    public bool fill { get; set; }
    public string pointBackgroundColor { get; set; }
    public string pointBorderColor { get; set; }
    public int pointRadius { get; set; }
    public int pointBorderWidth { get; set; }
    public int pointHitRadius { get; set; }
    public double tension { get; set; }
}

public class Data
{
    public List<string> labels { get; set; }
    public List<Dataset> datasets { get; set; }
}

public class Legend
{
    public string position { get; set; } = "top";
}

public class Font
{
    public int size { get; set; } = 12;
}

public class Title
{
    public bool display { get; set; } = true;
    public string text { get; set; } = String.Empty;
    public string color { get; set; } = "yellow";
    public Font font { get; set; } = new Font();
    public Padding padding { get; set; }
}

public class ArbitraryLine
{
    public string arbitraryLineColor { get; set; } = "blue";
    public int xPosition { get; set; } = 0;
    public int xWidth { get; set; } = 3;
}

public class Plugins
{
    public Legend legend { get; set; }
    public Title title { get; set; }
    public ArbitraryLine arbitraryLine { get; set; }
}

public class Padding
{
    public int top { get; set; } = 4;
    public int bottom { get; set; } = 4;
}

public class Ticks
{
    public string color { get; set; } = "yellow";
    public bool beginAtZero { get; set; } = true;
    public int minRotation { get; set; } = 0;
    public int maxRotation { get; set; } = 50;
    public bool mirror { get; set; } = false;
    public int textStrokeWidth { get; set; } = 0;
    public string textStrokeColor { get; set; } = String.Empty;
    public int padding { get; set; } = 3;
    public bool display { get; set; } = true;
    public bool autoSkip { get; set; } = true;
    public int autoSkipPadding { get; set; } = 3;
    public int labelOffset { get; set; } = 0;
    public string align { get; set; } = "center";
    public string crossAlign { get; set; } = "near";
    public bool showLabelBackdrop { get; set; } = false;
    public string backdropColor { get; set; } = "rgba(255, 255, 255, 0.75)";
    public int backdropPadding { get; set; } = 2;
}

public class Grid
{
    public string color { get; set; } = "grey";
    public bool display { get; set; } = true;
    public int lineWidth { get; set; } = 1;
    public bool drawBorder { get; set; } = true;
    public bool drawOnChartArea { get; set; } = true;
    public bool drawTicks { get; set; } = true;
    public int tickLength { get; set; } = 8;
    public int tickWidth { get; set; } = 1;
    public string tickColor { get; set; } = "yellow";
    public bool offset { get; set; } = false;
    public List<object> borderDash { get; set; } = new List<object>();
    public int borderDashOffset { get; set; } = 0;
    public int borderWidth { get; set; } = 1;
    public string borderColor { get; set; } = "rgba(0,0,0,0.1)";
}

public class X
{
    public string axis { get; set; } = "x";
    public bool display { get; set; } = true;
    public Title title { get; set; } = new Title();
    public Ticks ticks { get; set; } = new Ticks();
    public Grid grid { get; set; } = new Grid();
    public string type { get; set; } = "category";
    public bool offset { get; set; } = false;
    public bool reverse { get; set; } = false;
    public bool beginAtZero { get; set; } = true;
    public string bounds { get; set; } = "ticks";
    public int grace { get; set; } = 0;
    public string id { get; set; } = "x";
    public string position { get; set; } = "bottom";
}

public class Y
{
    public string axis { get; set; } = "y";
    public bool display { get; set; } = true;
    public Title title { get; set; } = new Title();
    public Ticks ticks { get; set; } = new Ticks();
    public Grid grid { get; set; } = new Grid();
    public string type { get; set; } = "linear";
    public bool offset { get; set; } = false;
    public bool reverse { get; set; } = false;
    public bool beginAtZero { get; set; } = true;
    public string bounds { get; set; } = "ticks";
    public int grace { get; set; } = 0;
    public string id { get; set; } = "y";
    public string position { get; set; } = "left";
}

public class Scales
{
    public X x { get; set; } = new X();
    public Y y { get; set; } = new Y();
}

public class Options
{
    public bool responsive { get; set; } = true;
    public Plugins plugins { get; set; } = new Plugins();
    public Scales scales { get; set; } = new Scales();
}
