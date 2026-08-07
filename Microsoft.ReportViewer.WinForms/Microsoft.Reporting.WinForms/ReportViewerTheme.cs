using System.Drawing;

namespace Microsoft.Reporting.WinForms
{
    /// <summary>
    /// Colors used by the report viewer chrome and report canvas.
    /// </summary>
    public sealed class ReportViewerTheme
    {
        private static readonly ReportViewerTheme s_light = new ReportViewerTheme(
            Color.FromArgb(248, 250, 252),
            Color.FromArgb(221, 226, 232),
            Color.FromArgb(243, 246, 250),
            Color.FromArgb(125, 135, 148),
            Color.FromArgb(205, 211, 219),
            Color.White,
            Color.FromArgb(35, 113, 194),
            Color.FromArgb(38, 46, 56),
            Color.FromArgb(157, 165, 174),
            Color.FromArgb(255, 248, 248),
            Color.FromArgb(132, 30, 30));

        private static readonly ReportViewerTheme s_dark = new ReportViewerTheme(
            Color.FromArgb(35, 40, 48),
            Color.FromArgb(67, 76, 89),
            Color.FromArgb(28, 32, 39),
            Color.FromArgb(131, 145, 163),
            Color.FromArgb(12, 15, 20),
            Color.FromArgb(48, 55, 65),
            Color.FromArgb(93, 170, 245),
            Color.FromArgb(232, 237, 243),
            Color.FromArgb(137, 148, 161),
            Color.FromArgb(62, 38, 41),
            Color.FromArgb(255, 190, 190));

        private static readonly ReportViewerTheme s_highContrast = new ReportViewerTheme(
            Color.Black,
            Color.White,
            Color.Black,
            Color.White,
            Color.Black,
            Color.Black,
            Color.Yellow,
            Color.White,
            Color.LightGray,
            Color.Black,
            Color.Yellow);

        public ReportViewerTheme(
            Color toolbarBackground,
            Color toolbarBorder,
            Color canvasBackground,
            Color pageBorder,
            Color pageShadow,
            Color inputBackground,
            Color accent,
            Color foreground,
            Color disabledForeground,
            Color errorBackground,
            Color errorForeground)
        {
            ToolbarBackground = toolbarBackground;
            ToolbarBorder = toolbarBorder;
            CanvasBackground = canvasBackground;
            PageBorder = pageBorder;
            PageShadow = pageShadow;
            InputBackground = inputBackground;
            Accent = accent;
            Foreground = foreground;
            DisabledForeground = disabledForeground;
            ErrorBackground = errorBackground;
            ErrorForeground = errorForeground;
        }

        public Color ToolbarBackground { get; }

        public Color ToolbarBorder { get; }

        public Color CanvasBackground { get; }

        public Color PageBorder { get; }

        public Color PageShadow { get; }

        public Color InputBackground { get; }

        public Color Accent { get; }

        public Color Foreground { get; }

        public Color DisabledForeground { get; }

        public Color ErrorBackground { get; }

        public Color ErrorForeground { get; }

        public static ReportViewerTheme Light => s_light;

        public static ReportViewerTheme Dark => s_dark;

        public static ReportViewerTheme HighContrast => s_highContrast;

        internal Color MapReportForeground(Color source)
        {
            if (source.IsEmpty || source == Color.Transparent || !IsDarkCanvas)
            {
                return source;
            }

            // Report definitions commonly use black as the default text color.
            // Keep colors that already contrast with the themed canvas, but move
            // dark colors to the theme foreground when they would disappear.
            if (GetContrastRatio(source, CanvasBackground) >= 3.0)
            {
                return source;
            }

            return Color.FromArgb(source.A, Foreground.R, Foreground.G, Foreground.B);
        }

        internal Color MapReportBackground(Color source)
        {
            if (source.IsEmpty || source == Color.Transparent || !IsDarkCanvas)
            {
                return source;
            }

            // A white report surface is the light-theme equivalent of the
            // canvas surface. Re-map it so dark mode remains a real dark mode.
            if (GetRelativeLuminance(source) >= 0.95)
            {
                return CanvasBackground;
            }

            return source;
        }

        private bool IsDarkCanvas => GetRelativeLuminance(CanvasBackground) < 0.5;

        private static double GetContrastRatio(Color first, Color second)
        {
            double firstLuminance = GetRelativeLuminance(first);
            double secondLuminance = GetRelativeLuminance(second);
            double brighter = firstLuminance > secondLuminance ? firstLuminance : secondLuminance;
            double darker = firstLuminance > secondLuminance ? secondLuminance : firstLuminance;
            return (brighter + 0.05) / (darker + 0.05);
        }

        private static double GetRelativeLuminance(Color color)
        {
            static double Linearize(byte channel)
            {
                double value = channel / 255.0;
                return value <= 0.03928 ? value / 12.92 : System.Math.Pow((value + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Linearize(color.R) + 0.7152 * Linearize(color.G) + 0.0722 * Linearize(color.B);
        }
    }
}
