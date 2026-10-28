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
    static readonly Dictionary<string, string> VectorIcons = new()
    {
        ["Home"] = "M3 10L12 3 21 10 M5 9V21H10V15H14V21H19V9",
        ["Dashboard"] = "M3 3H10V10H3Z M14 3H21V10H14Z M3 14H10V21H3Z M14 14H21V21H14Z",
        ["Control"] = "M3 6H8 M12 6H21 M3 12H14 M18 12H21 M3 18H5 M9 18H21 M8 3V9 M14 9V15 M5 15V21",
        ["Shelf"] = "M5 4H19L22 17V21H2V17Z M3 15H8L10 18H14L16 15H21",
        ["Todo"] = "M3 5L5 7 8 3 M11 5H21 M3 13L5 15 8 11 M11 13H21 M3 21H7 M11 21H21",
        ["Notes"] = "M4 3H20V15L14 21H4Z M14 21V15H20",
        ["Pomodoro"] = "M9 2H15 M12 2V5 M18 6L20 4 M12 9V14 M20 14A8 8 0 1 1 4 14 8 8 0 1 1 20 14",
        ["Health"] = "M12 21L3 12C-3 4 7 0 12 6C17 0 27 4 21 12Z M3 12H8L10 8 13 16 15 12H21",
        ["Weather"] = "M9 5A5 5 0 1 1 17 11 M12 1V3 M3 5L5 6 M20 4L18 6 M22 10H20 M6 21H19A4 4 0 0 0 19 13A6 6 0 0 0 8 12A4.5 4.5 0 0 0 6 21Z",
        ["Screen Time"] = "M6 3H18 M6 21H18 M7 3C7 8 9 9 12 12C15 15 17 16 17 21 M17 3C17 8 15 9 12 12C9 15 7 16 7 21",
        ["Day Progress"] = "M4 3V21H22 M9 5H13 M9 10H18 M9 15H21",
        ["Connections"] = "M12 2V22L18 16 6 6 M12 2L18 8 6 18",
        ["System"] = "M20 12A8 8 0 1 1 4 12 8 8 0 1 1 20 12 M12 12L16 7 M7 8L8 9 M6 13H8 M16 15L18 17",
        ["AI Chat"] = "M4 20L17 7 M14 4L20 10 M5 2V8 M2 5H8 M19 15V21 M16 18H22",
        ["Calendar"] = "M3 5H21V22H3Z M3 10H21 M7 2V7 M17 2V7 M7 14H9 M12 14H14 M7 18H9 M12 18H14",
        ["wifi"] = "M2 8C8 2 16 2 22 8 M5 12C9 8 15 8 19 12 M8 16C11 13 13 13 16 16 M12 20H12.1",
        ["bluetooth"] = "M12 2V22L18 16 6 6 M12 2L18 8 6 18",
        ["coffee"] = "M3 9H17V16C17 22 3 22 3 16Z M17 10H19C24 10 24 16 17 16 M6 2C3 5 9 5 6 7 M12 2C9 5 15 5 12 7",
        ["moon"] = "M21 14A9 9 0 1 1 10 3A7 7 0 0 0 21 14Z",
        ["mic"] = "M9 4A3 3 0 0 1 15 4V12A3 3 0 0 1 9 12Z M5 10V12A7 7 0 0 0 19 12V10 M12 19V23 M8 23H16",
        ["eye"] = "M1 12C7 2 17 2 23 12C17 22 7 22 1 12Z M16 12A4 4 0 1 1 8 12 4 4 0 1 1 16 12",
        ["eye-off"] = "M3 3L21 21 M9 5C15 3 20 7 23 12L20 16 M15 19C8 22 3 17 1 12L5 7 M10 10L14 14",
        ["crop"] = "M6 2V18H22 M2 6H18V22",
        ["lock"] = "M5 10H19V22H5Z M8 10V6A4 4 0 0 1 16 6V10",
        ["volume"] = "M3 9H7L12 5V19L7 15H3Z M16 8C19 10 19 14 16 16 M19 4C25 8 25 16 19 20",
        ["sun"] = "M16 12A4 4 0 1 1 8 12 4 4 0 1 1 16 12 M12 1V4 M12 20V23 M1 12H4 M20 12H23 M4 4L6 6 M18 18L20 20 M4 20L6 18 M18 6L20 4",
        ["bell"] = "M4 17H20L18 14V9A6 6 0 0 0 6 9V14Z M10 21H14 M12 1V3",
        ["settings"] = "M9 2H15L16 6 20 7 22 12 19 15 19 20 14 22 11 19 6 20 3 16 5 12 3 8 7 6Z M16 12A4 4 0 1 1 8 12 4 4 0 1 1 16 12",
        ["plus"] = "M12 4V20 M4 12H20",
        ["minus"] = "M4 12H20",
        ["close"] = "M5 5L19 19 M19 5L5 19",
        ["trash"] = "M3 6H21 M9 6V3H15V6 M6 6V22H18V6 M10 10V18 M14 10V18",
        ["play"] = "M7 3L21 12 7 21Z",
        ["pause"] = "M7 4V20 M17 4V20",
        ["next"] = "M4 5L15 12 4 19Z M19 5V19",
        ["previous"] = "M20 5L9 12 20 19Z M5 5V19",
        ["refresh"] = "M20 7A9 9 0 1 0 21 15 M20 2V8H14",
        ["chevron"] = "M9 5L16 12 9 19",
        ["back"] = "M15 5L8 12 15 19",
        ["check"] = "M4 12L9 17 20 5",
        ["checks"] = "M2 12L7 17 18 5 M12 17L23 5",
        ["share"] = "M14 3L22 9 14 15V11C8 11 7 14 7 17C4 10 7 7 14 7Z M4 12H2V22H18V18",
        ["folder"] = "M2 6H9L11 9H22V21H2Z",
        ["download"] = "M12 2V16L6 10 M12 16L18 10 M3 17V22H21V17",
        ["calculator"] = "M5 2H19V22H5Z M8 5H16V9H8Z M8 13H9 M15 13H16 M8 17H9 M15 17H16",
        ["music"] = "M9 18V5L20 2V15 M9 18A3 3 0 1 1 3 18 3 3 0 1 1 9 18 M20 15A3 3 0 1 1 14 15 3 3 0 1 1 20 15",
        ["clock"] = "M21 12A9 9 0 1 1 3 12 9 9 0 1 1 21 12 M12 6V12L16 15",
        ["pin"] = "M12 22C12 22 4 13 4 9A8 8 0 0 1 20 9C20 13 12 22 12 22Z M15 9A3 3 0 1 1 9 9 3 3 0 1 1 15 9",
        ["water"] = "M6 3H18L16 22H8Z M7 9H17",
        ["walk"] = "M14 2H14.1 M7 10L11 6 15 8 18 12H21 M11 6L10 14 6 22 M10 14L15 17 16 22",
        ["leaf"] = "M12 20C0 20 0 10 2 8C9 8 12 14 12 20 M12 20C12 9 19 4 22 4C25 13 19 20 12 20",
        ["headphones"] = "M3 14V11A9 9 0 0 1 21 11V14 M3 12H7V21H3Z M17 12H21V21H17Z",
        ["cpu"] = "M6 6H18V18H6Z M9 9H15V15H9Z M9 2V6 M15 2V6 M9 18V22 M15 18V22 M2 9H6 M2 15H6 M18 9H22 M18 15H22",
        ["memory"] = "M2 6H22V18H2Z M6 9V14 M10 9V14 M14 9V14 M18 9V14 M6 18V22 M12 18V22 M18 18V22",
        ["disk"] = "M3 4H21V20H3Z M3 14H21 M7 17H7.1",
        ["temperature"] = "M9 15V5A3 3 0 0 1 15 5V15A5 5 0 1 1 9 15 M12 8V18",
        ["send"] = "M12 21V3 M5 10L12 3 19 10",
        ["edit"] = "M4 16L17 3 21 7 8 20H4Z M14 6L18 10",
        ["external"] = "M14 3H22V11 M22 3L12 13 M10 4H3V21H20V14",
        ["chart"] = "M4 21V15H8V21 M10 21V7H14V21 M16 21V2H20V21"
    };
    new FrameworkElement Icon(string name, double size = 20, Brush? color = null)
    {
        var path = new System.Windows.Shapes.Path { Data = Geometry.Parse(VectorIcons.GetValueOrDefault(name, VectorIcons["Dashboard"])), Stroke = color ?? Fg, StrokeThickness = 2, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
        return new Viewbox { Width = size * S.IconSize / 18, Height = size * S.IconSize / 18, Child = new Canvas { Width = 24, Height = 24, Children = { path } }, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
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
        b.Content = Row(Icon(icon, 16, primary ? Brushes.White : Fg), Space(7), T(label, 14, primary ? Brushes.White : Fg, true));
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
        b.Content = Icon(icon, 23, active ? Brush("#001A28") : Fg);
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
            var more = IconButton("chevron", detail, "Choose " + caption, active ? Brush("#001A28") : Fg, 18);
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
        slider.PreviewMouseLeftButtonUp += (_, _) => onChange(slider.Value);
        slider.PreviewKeyUp += (_, _) => onChange(slider.Value);
        Put(grid, slider, 1);
        Put(grid, number, 2);
        return grid;
    }
}
