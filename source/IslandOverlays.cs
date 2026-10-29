namespace OrbitIsland;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
public partial class Island
{
    void AnimateBody()
    {
        if (!IsLoaded)
            return;
        body.RenderTransform = new TranslateTransform(0, 5);
        body.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170)) { FillBehavior = FillBehavior.Stop });
        ((TranslateTransform)body.RenderTransform).BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(5, 0, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop });
    }
    void ShowOverlay(string title, UIElement content, Action? save = null, string saveLabel = "Save")
    {
        double w = ActualWidth, h = ActualHeight, l = Left;
        modalOpen = true;
        expanded = true;
        Place();
        Build();
        if (designRoot == null)
            return;
        overlay = new Grid { Background = Brush(S.Light ? "#DDF7F7FB" : "#DC000000"), Margin = new Thickness(24, 10, 24, 18) };
        var panel = new DockPanel { MaxHeight = 460, Width = 540 };
        var heading = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
        var close = IconButton("close", CloseOverlay, "Close");
        DockPanel.SetDock(close, Dock.Right);
        heading.Children.Add(close);
        heading.Children.Add(T(title, 22, Fg, true));
        DockPanel.SetDock(heading, Dock.Top);
        panel.Children.Add(heading);
        var error = T("", 12, Brush("#FF9A84"));
        error.Margin = new Thickness(0, 10, 0, 8);
        var footer = Col(error);
        if (save != null)
        {
            var button = Pill(saveLabel, "check", () => { try { save(); CloseOverlay(); } catch (Exception ex) { error.Text = ex.Message; } }, true);
            button.HorizontalAlignment = HorizontalAlignment.Right;
            footer.Children.Add(button);
        }
        DockPanel.SetDock(footer, Dock.Bottom);
        panel.Children.Add(footer);
        panel.Children.Add(Scroll(content));
        var card = PanelCard(panel, 22);
        card.HorizontalAlignment = HorizontalAlignment.Center;
        card.VerticalAlignment = VerticalAlignment.Center;
        overlay.Children.Add(card);
        designRoot.Children.Add(overlay);
        AnimateBounds(w, h, l);
    }
    void CloseOverlay()
    {
        double w = ActualWidth, h = ActualHeight, l = Left;
        modalOpen = false;
        overlay = null;
        Place();
        Build();
        AnimateBounds(w, h, l);
    }
    bool ToggleIsOn(string icon)
    {
        try
        {
            return icon switch
            {
                "wifi" => radios.GetValueOrDefault("WiFi") == "On",
                "bluetooth" => radios.GetValueOrDefault("Bluetooth") == "On",
                "coffee" => keepAwake,
                "moon" => !S.Light,
                "mic" => !Native.Audio(1).Muted,
                "volume" => !Native.Audio().Muted,
                _ => false
            };
        }
        catch { return false; }
    }
    FrameworkElement UsageAppLabel(string key)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new()
        {
            Width = new GridLength(31)
        });
        grid.ColumnDefinitions.Add(new());
        UIElement symbol = D.AppPaths.TryGetValue(key, out var path) ? AppIcon(path) : Icon("Dashboard", 22, Muted);
        if (symbol is FrameworkElement f)
        {
            f.Width = 23;
            f.Height = 23;
        }
        Put(grid, symbol, 0);
        Put(grid, Line(D.AppLabels.GetValueOrDefault(key, key), 14, Fg, true), 1);
        return grid;
    }
}
