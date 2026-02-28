namespace ReportViewerCore.Sample.MAUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute(nameof(ReportViewerPage), typeof(ReportViewerPage));
    }
}
