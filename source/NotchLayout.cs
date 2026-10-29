namespace OrbitIsland;

using System;
public static class NotchLayout
{
    public const double DesignWidth = 1080;
    public static double DesignHeight(string module, double textScale, double iconSize) => ((module is "Home" or "Dashboard" or "Pomodoro" or "System" or "Shelf") ? 294 : 402) * Math.Max(1, Math.Max(textScale, iconSize / 18));
    public static (double Width, double Height, double Scale) Size(double desired, double screenWidth, string module, double textScale, double iconSize, bool expanded, double modalHeight = 0)
    {
        if (!expanded)
            return (360, 70, 1);
        double w = Math.Min(desired + 60, screenWidth - 24);
        double scale = (w - 60) / DesignWidth;
        return (w, Math.Max(DesignHeight(module, textScale, iconSize), modalHeight) * scale + 22, scale);
    }
}
