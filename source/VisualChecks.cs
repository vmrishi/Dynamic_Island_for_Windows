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
    bool HasViewError(DependencyObject parent){if(parent is System.Windows.Controls.TextBlock text&&text.Text=="This view is unavailable")return true;for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)if(HasViewError(VisualTreeHelper.GetChild(parent,i)))return true;return false;}
    internal async Task CaptureVisualChecks(string directory)
    {
        Directory.CreateDirectory(directory);
        var results = new System.Collections.Generic.List<string>();
        foreach(var dimensions in new[]{(Width:336d, Height:48d, Radius:18d), (Width:1116d, Height:294d, Radius:38d), (Width:1116d, Height:402d, Radius:38d)})
        {
            var clip = NotchSurface.CreateClip(dimensions.Width, dimensions.Height, dimensions.Radius);
            if(!clip.IsFrozen || Math.Abs(clip.Transform.Value.OffsetX + 18) > .001)
                throw new Exception("Notch clip is not safely transformed and frozen.");
        }
        results.Add("PASS: collapsed and expanded clips can be transformed without mutating frozen parsed geometry");
        foreach (var icon in VectorIcons)
            Geometry.Parse(icon.Value);
        results.Add("PASS: all vector icon paths parse");
        foreach (var name in S.Modules.Concat(new[] { "Notifications" }))
        {
            tab = name;
            expanded = true;
            Place();
            Build();
            UpdateLayout();
            await Dispatcher.Yield(DispatcherPriority.Render);
            if (HasViewError(body))throw new Exception(name+": view failed while building");
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
        await Task.Delay(220);
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
