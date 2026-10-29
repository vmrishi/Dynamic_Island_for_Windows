namespace OrbitIsland;

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
public sealed class RoundedProgress : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(RoundedProgress), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender, null, (_, value) => double.IsFinite((double)value) ? Math.Clamp((double)value, 0, 1) : 0));
    public double Value
    {
        get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value);
    }
    public Brush Fill { get; set; } = Brushes.DodgerBlue;
    public Brush Track { get; set; } = new SolidColorBrush(Color.FromRgb(42, 42, 46));
    public double Radius { get; set; } = double.NaN;
    public bool Timeline
    {
        get; set;
    }
    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0)
            return;
        double r = double.IsNaN(Radius) ? Math.Min(h / 2, 16) : Radius;
        var clip = new RectangleGeometry(new Rect(0, 0, w, h), r, r);
        dc.PushClip(clip);
        dc.DrawRoundedRectangle(Track, null, new Rect(0, 0, w, h), r, r);
        double filled = w * Value;
        if (filled > 0)
            dc.DrawRoundedRectangle(Fill, null, new Rect(0, 0, filled, h), Math.Min(r, filled / 2), r);
        if (Timeline)
        {
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)), 1);
            for (int i = 1; i < 12; i++)
            {
                double x = i * w / 12;
                dc.DrawLine(pen, new Point(x, 10), new Point(x, h - 10));
            }
        }
        dc.Pop();
    }
    public void AnimateTo(double target)
    {
        double from = Value;
        BeginAnimation(ValueProperty, null);
        Value = Math.Clamp(target, 0, 1);
        BeginAnimation(ValueProperty, new DoubleAnimation(from, Value, TimeSpan.FromMilliseconds(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop });
    }
}
