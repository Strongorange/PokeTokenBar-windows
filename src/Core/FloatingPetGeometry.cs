namespace PokeTokenBar.Core;

public static class FloatingPetGeometry
{
    public const double ClickThresholdSquared = 16;
    public const double BubbleHeadroom = 72;
    public const double BubbleMinWidth = 180;
    public const double BubbleHorizontalPadding = 8;
    public const double BubbleContentWidth = BubbleMinWidth - BubbleHorizontalPadding * 2;
    public const int BubbleBodyLineLimit = 2;

    public static bool IsClick(double dx, double dy, double thresholdSquared = ClickThresholdSquared) =>
        dx * dx + dy * dy < thresholdSquared;

    public static (double Width, double Height) PanelSize(double petSize, bool showingBubble) =>
        showingBubble
            ? (Math.Max(petSize, BubbleMinWidth), petSize + BubbleHeadroom)
            : (petSize, petSize);

    public static double PanelXInset(double petSize, double panelWidth) =>
        Math.Max(0, (panelWidth - petSize) / 2);
}
