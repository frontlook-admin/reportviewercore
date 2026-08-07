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
    }
}
