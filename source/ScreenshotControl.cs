namespace OrbitIsland;

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
public partial class Island
{
    async Task CaptureScreen()
    {
        Hide();
        try
        {
            await Task.Delay(200);
            var screens = System.Windows.Forms.Screen.AllScreens;
            var bounds = screens[Math.Clamp(S.Monitor, 0, screens.Length - 1)].Bounds;
            using var bitmap = new System.Drawing.Bitmap(bounds.Width, bounds.Height);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                graphics.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
            using var memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = memory;
            image.EndInit();
            image.Freeze();
            string folder = Path.Combine(store.DirectoryPath, "Screenshots");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".png");
            File.WriteAllBytes(path, memory.ToArray());
            Show();
            SetExpanded(true);
            var preview = new Image { Source = image, MaxHeight = 250, Stretch = System.Windows.Media.Stretch.Uniform };
            ShowOverlay("Screenshot saved", Col(preview, T(path, 11, Muted), Pill("Copy image", "share", () => Clipboard.SetImage(image))));
        }
        catch (Exception e) { Show(); SetExpanded(true); ShowIslandError("Screenshot", e.Message); }
    }
}
