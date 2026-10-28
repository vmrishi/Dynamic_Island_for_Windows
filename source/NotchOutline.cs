namespace OrbitIsland;

using System;
public static class NotchOutline
{
    public static string Create(double width, double height, double radius)
    {
        double wing = 18, r = Math.Min(radius, height / 2);
        return FormattableString.Invariant($"M 0,0 Q {wing},0 {wing},{wing} L {wing},{height - r} Q {wing},{height} {wing + r},{height} L {width - wing - r},{height} Q {width - wing},{height} {width - wing},{height - r} L {width - wing},{wing} Q {width - wing},0 {width},0 Z");
    }
}
