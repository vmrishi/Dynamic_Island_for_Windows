namespace OrbitIsland;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
public sealed class NotchSurface : FrameworkElement
{
    public Brush Fill { get; set; } = Brushes.Black;
    public double BottomRadius { get; set; } = 38;
    public static string Outline(double width, double height, double radius) => NotchOutline.Create(width, height, radius);
    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth < 80 || ActualHeight < 36)
            return;
        dc.DrawGeometry(Fill, null, Geometry.Parse(Outline(ActualWidth, ActualHeight, BottomRadius)));
    }
}
