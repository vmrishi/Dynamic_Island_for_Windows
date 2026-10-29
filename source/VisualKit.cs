namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
public partial class Island
{
    static readonly Dictionary<string, string> VectorIcons = LoadVectorIcons();
    static Dictionary<string, string> LoadVectorIcons()
    {
        using var stream = typeof(Island).Assembly.GetManifestResourceStream("OrbitIsland.Assets.Icons.json") ?? throw new InvalidOperationException("Icon assets are missing.");
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? throw new InvalidOperationException("Icon assets are invalid.");
    }
    static readonly Dictionary<string, Geometry> geometryCache = new();
    new FrameworkElement Icon(string name, double size = 20, Brush? color = null)
    {
        string key = VectorIcons.ContainsKey(name) ? name : "Dashboard";
        if (!geometryCache.TryGetValue(key, out var geometry))
        {
            geometry = Geometry.Parse(VectorIcons[key]).Clone();
            geometry.Freeze();
            geometryCache[key] = geometry;
        }
        var path = new System.Windows.Shapes.Path { Data = geometry, Stroke = color ?? Fg, StrokeThickness = 1.8, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
        return new Viewbox { Width = size * S.IconSize / 18, Height = size * S.IconSize / 18, Child = new Canvas { Width = 24, Height = 24, Children = { path } }, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
    }
    FrameworkElement IconLabel(string icon, string text, double size = 14, Brush? color = null, bool bold = false)
    {
        var g = new Grid();
        g.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(25)
        });
        g.ColumnDefinitions.Add(new());
        Put(g, Icon(icon, 17, color ?? Muted), 0);
        Put(g, Line(text, size, color, bold), 1);
        return g;
    }
    Button IconButton(string icon, Action action, string tip, Brush? color = null, double size = 20)
    {
        var b = Btn("", action, false, tip);
        b.Content = Icon(icon, size, color ?? Muted);
        b.Background = Brushes.Transparent;
        b.Tag = new CornerRadius(99);
        b.Padding = new Thickness(10, 8, 10, 8);
        b.Margin = new Thickness(0, 0, 5, 0);
        return b;
    }
    Button Pill(string label, string icon, Action action, bool primary = false)
    {
        var b = primary ? Primary("", action) : Btn("", action);
        var labelGrid = new Grid();
        labelGrid.ColumnDefinitions.Add(new()
        {
            Width = GridLength.Auto
        });
        labelGrid.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(7)
        });
        labelGrid.ColumnDefinitions.Add(new());
        Put(labelGrid, Icon(icon, 16, primary ? Brushes.White : Fg), 0);
        Put(labelGrid, Line(label, 14, primary ? Brushes.White : Fg, true), 2);
        b.Content = labelGrid;
        b.Padding = new Thickness(14, 8, 14, 8);
        return b;
    }
    Border Space(double w) => new() { Width = w };
    TextBlock Line(string text, double size = 14, Brush? color = null, bool bold = false)
    {
        var t = T(text, size, color, bold);
        t.TextWrapping = TextWrapping.NoWrap;
        t.TextTrimming = TextTrimming.CharacterEllipsis;
        return t;
    }
    Border PanelCard(UIElement content, int padding = 18)
    {
        var card = Card(content, padding);
        card.Margin = new Thickness(0);
        return card;
    }
    Border Divider(bool vertical = true) => new() { Background = CardBorder, Width = vertical ? 1 : double.NaN, Height = vertical ? double.NaN : 1, Margin = vertical ? new Thickness(18, 0, 20, 0) : new Thickness(0, 12, 0, 12) };
    Grid SpacedColumns(params double[] sizes)
    {
        var grid = Columns(sizes);
        return grid;
    }
    UIElement Center(UIElement child)
    {
        var g = new Grid();
        g.Children.Add(child);
        if (child is FrameworkElement f)
        {
            f.HorizontalAlignment = HorizontalAlignment.Center;
            f.VerticalAlignment = VerticalAlignment.Center;
        }
        return g;
    }
    UIElement EmptyState(string icon, string text, string? detail = null)
    {
        var p = Col(Icon(icon, 32, Brush(S.Light ? "#9C9CA5" : "#454548")));
        Gap(p, 12);
        var title = T(text, 16, Muted);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        p.Children.Add(title);
        if (detail != null)
        {
            Gap(p, 8);
            var d = T(detail, 12, Muted);
            d.HorizontalAlignment = HorizontalAlignment.Center;
            p.Children.Add(d);
        }
        return Center(p);
    }
    void ToggleAwake()
    {
        keepAwake = !keepAwake;
        Native.SetThreadExecutionState(keepAwake ? 0x80000001 : 0x80000000);
        Build();
    }
    void ChangeAppTheme()
    {
        S.Light = !S.Light;
        Save();
        SetExpanded(true);
    }
    FrameworkElement LabeledTile(string icon, string caption, Action action, bool active = false, Action? detail = null)
    {
        var p = new DockPanel { Margin = new Thickness(0, 0, 14, 10) };
        var label = Line(caption, 13, Muted);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Margin = new Thickness(0, 9, 0, 0);
        DockPanel.SetDock(label, Dock.Bottom);
        p.Children.Add(label);
        var g = new Grid { Height = 62 };
        var b = Btn("", action, false, caption);
        b.Content = Icon(icon, 23, active ? Brushes.Black : Fg);
        b.Background = active ? Brush("#4EC2F4") : Surface;
        b.Margin = new Thickness(0);
        g.Children.Add(b);
        if (detail != null)
        {
            g.ColumnDefinitions.Add(new());
            g.ColumnDefinitions.Add(new()
            {
                Width = new GridLength(66)
            });
            var more = IconButton("chevron", detail, "Choose " + caption, active ? Brushes.Black : Fg, 18);
            more.Tag = new CornerRadius(0, 16, 16, 0);
            b.Tag = new CornerRadius(16, 0, 0, 16);
            more.Background = active ? Brush("#4EC2F4") : Surface;
            more.Margin = new Thickness(2, 0, 0, 0);
            Grid.SetColumn(more, 1);
            g.Children.Add(more);
        }
        p.Children.Add(g);
        return p;
    }
    FrameworkElement SliderControl(string icon, string name, double value, Action<double> onChange)
    {
        var grid = new Grid { Height = 40 };
        grid.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(44)
        });
        grid.ColumnDefinitions.Add(new());
        grid.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(48)
        });
        Put(grid, Icon(icon, 22), 0);
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = value, VerticalAlignment = VerticalAlignment.Center, IsSnapToTickEnabled = true, TickFrequency = 1 };
        AutomationProperties.SetName(slider, name);
        var number = Line(((int)value).ToString(), 13, Muted);
        number.HorizontalAlignment = HorizontalAlignment.Right;
        slider.ValueChanged += (_, _) => number.Text = ((int)slider.Value).ToString();
        slider.PreviewMouseLeftButtonUp += (_, _) => Dispatcher.BeginInvoke(new Action(() => onChange(slider.Value)));
        slider.PreviewKeyUp += (_, _) => onChange(slider.Value);
        Put(grid, slider, 1);
        Put(grid, number, 2);
        return grid;
    }
}
