using CommunityToolkit.Mvvm.Input;

namespace ReportViewerCore.Sample.MAUI;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    [RelayCommand]
    private async Task ViewReport(string reportName)
    {
        try
        {
            await Shell.Current.GoToAsync($"{nameof(ReportViewerPage)}?ReportName={reportName}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to open report: {ex.Message}", "OK");
        }
    }
}
