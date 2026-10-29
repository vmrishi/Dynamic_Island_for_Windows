namespace OrbitIsland;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
public partial class Island
{
    bool HasViewError(DependencyObject parent)
    {
        if (parent is System.Windows.Controls.TextBlock text && text.Text == "This view is unavailable")
            return true;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            if (HasViewError(VisualTreeHelper.GetChild(parent, i)))
                return true;
        return false;
    }
    System.Collections.Generic.IEnumerable<DependencyObject> VisualDescendants(DependencyObject root)
    {
        yield return root;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in VisualDescendants(VisualTreeHelper.GetChild(root, i)))
                yield return child;
    }
    internal async Task CaptureVisualChecks(string directory)
    {
        Directory.CreateDirectory(directory);
        var results = new System.Collections.Generic.List<string>();
        foreach (var dimensions in new[] { (Width: 336d, Height: 48d, Radius: 18d), (Width: 1116d, Height: 294d, Radius: 38d), (Width: 1116d, Height: 402d, Radius: 38d) })
        {
            var clip = NotchSurface.CreateClip(dimensions.Width, dimensions.Height, dimensions.Radius);
            if (!clip.IsFrozen || Math.Abs(clip.Transform.Value.OffsetX + 18) > .001)
                throw new Exception("Notch clip is not safely transformed and frozen.");
        }
        results.Add("PASS: collapsed and expanded clips can be transformed without mutating frozen parsed geometry");
        foreach (var icon in VectorIcons)
            Geometry.Parse(icon.Value);
        results.Add("PASS: all vector icon paths parse");
        radios["WiFi"] = "On";
        tab = "Dashboard";
        expanded = true;
        Place();
        Build();
        UpdateLayout();
        var toggle = VisualDescendants(body).OfType<System.Windows.Controls.Button>().First(b => System.Windows.Automation.AutomationProperties.GetName(b) == "Wi-Fi");
        if (toggle.Tag is not CornerRadius corners || Math.Abs(corners.TopLeft - 16) > 0.01 || toggle.Background is not SolidColorBrush background || background.Color != Colors.White)
            throw new Exception("Dashboard toggles must be rounded rectangles with a white ON state.");
        results.Add("PASS: dashboard ON toggle uses a rounded rectangle and white highlight");
        foreach (double width in new[] { 800d, 1500d })
        {
            S.Width = width;
            Place();
            Build();
            UpdateLayout();
            if (designRoot == null || Math.Abs(designRoot.ActualWidth - NotchLayout.DesignWidth) > 1)
                throw new Exception("Responsive layout lost its design surface.");
        }
        S.Width = 1080;
        results.Add("PASS: content uses the same design surface at compact and wide sizes");
        var progress = new RoundedProgress { Width = 120, Height = 8, Value = 1, Fill = Brushes.White, Track = Brushes.Black };
        progress.Measure(new Size(120, 8));
        progress.Arrange(new Rect(0, 0, 120, 8));
        progress.UpdateLayout();
        var rounded = new RenderTargetBitmap(120, 8, 96, 96, PixelFormats.Pbgra32);
        rounded.Render(progress);
        var pixels = new byte[120 * 8 * 4];
        rounded.CopyPixels(pixels, 120 * 4, 0);
        if (pixels[3] >= 64 || pixels[(4 * 120 + 60) * 4 + 3] < 200)
            throw new Exception("Progress bar does not render rounded clipped corners.");
        results.Add("PASS: progress track renders rounded corners");
        foreach (var name in S.Modules.Concat(new[] { "Notifications" }))
        {
            tab = name;
            expanded = true;
            Place();
            Build();
            UpdateLayout();
            await Dispatcher.Yield(DispatcherPriority.Render);
            if (HasViewError(body))
                throw new Exception(name + ": view failed while building");
            if (body.ActualWidth < 200 || body.ActualHeight < 100)
                throw new Exception(name + ": content has no usable layout");
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(ActualWidth), (int)Math.Ceiling(ActualHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(notchScene);
            using var file = File.Create(Path.Combine(directory, name.Replace(" ", "-") + ".png"));
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(file);
            results.Add("PASS: " + name + " measured and rendered");
        }
        SetExpanded(false);
        await Task.Delay(320);
        UpdateLayout();
        await Dispatcher.Yield(DispatcherPriority.Render);
        if (Math.Abs(notchScene.ActualHeight - 48) > 1)
            throw new Exception("Collapsed notch height must be 48 DIPs");
        var image = new RenderTargetBitmap((int)Math.Ceiling(notchScene.ActualWidth), 48, 96, 96, PixelFormats.Pbgra32);
        image.Render(notchScene);
        using (var file = File.Create(Path.Combine(directory, "Collapsed.png")))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(file);
        }
        results.Add("PASS: clock-only notch is 48 DIPs tall");
        File.WriteAllLines(Path.Combine(directory, "visual-checks.txt"), results);
    }
}
