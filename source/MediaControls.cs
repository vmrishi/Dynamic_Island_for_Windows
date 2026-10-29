namespace OrbitIsland;

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
public partial class Island
{
    void ShowMediaSources()
    {
        var content = Col();
        if (mediaSnapshot?.Sources.Count > 0)
        {
            foreach (var source in mediaSnapshot.Sources)
            {
                var b = Pill(source.Name + (source.Playing ? " · Playing" : ""), "music", async () => { mediaSessions.SelectedId = source.Id; CloseOverlay(); await RefreshMedia(); });
                b.HorizontalContentAlignment = HorizontalAlignment.Left;
                b.Margin = new Thickness(0, 0, 0, 8);
                content.Children.Add(b);
            }
        }
        else
        {
            content.Children.Add(T(mediaError.Length > 0 ? mediaError : "No Windows media sessions found. Start playback in Chrome, Edge, Firefox, Spotify or a compatible Windows player. The app or browser must expose Windows media controls; private/DRM sessions can restrict them.", 14, Muted));
        }
        Gap(content, 12);
        content.Children.Add(Pill("Refresh sources", "refresh", async () => { await RefreshMedia(); ShowMediaSources(); }));
        ShowOverlay("Media sources", content);
    }
    FrameworkElement MediaSeekBar()
    {
        var token = mediaSnapshot?.Token;
        var slider = new Slider { Minimum = 0, Maximum = Math.Max(1, MediaNumber("duration")), Value = MediaNumber("position"), IsEnabled = mediaSnapshot?.CanSeek == true, VerticalAlignment = VerticalAlignment.Center, IsMoveToPointEnabled = true };
        mediaTimeline = slider;
        slider.PreviewMouseLeftButtonDown += (_, _) => mediaScrubbing = true;
        slider.PreviewMouseLeftButtonUp += async (_, _) => { try { await MediaCommand("seek", slider.Value, token); } finally { mediaScrubbing = false; await RefreshMedia(); } };
        slider.LostMouseCapture += (_, _) => mediaScrubbing = false;
        slider.PreviewKeyUp += async (_, _) => await MediaCommand("seek", slider.Value, token);
        return slider;
    }
}
