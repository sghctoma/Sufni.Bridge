using System;
using System.Collections.Generic;
using ScottPlot;

namespace Sufni.Bridge.Views.Plots;

public class Text : IPlottable
{
    public Label Label { get; } = new();
    public FontStyle Font => Label.Font;
    public string Content 
    {
        get => Label.Text;
        set => Label.Text = value;
    }
    
    public Alignment Alignment 
    {
        get => Label.Alignment;
        set => Label.Alignment = value;
    }

    public int XOffset = 0;
    public int YOffset = 0;

    public bool IsVisible { get; set; } = true;
    public IAxes Axes { get; set; } = new Axes();
    public IEnumerable<LegendItem> LegendItems { get; } = Array.Empty<LegendItem>();

    public Coordinates Position { get; set; }

    public AxisLimits GetAxisLimits() => AxisLimits.NoLimits;

    public void Render(RenderPack rp)
    {
        var px = Axes.GetPixel(Position);
        px.X += XOffset;
        px.Y -= YOffset;
        Label.Draw(rp.Canvas, px);
    }
}