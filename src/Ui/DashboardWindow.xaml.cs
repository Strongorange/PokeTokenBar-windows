using System.Windows;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

public partial class DashboardWindow : Window
{
    public DashboardWindow()
    {
        InitializeComponent();
    }

    public void Update(UsageDisplayState state)
    {
        ProvidersList.Items.Clear();
        foreach (var provider in state.Providers)
        {
            var availability = provider.Available ? "" : "  (not found)";
            ProvidersList.Items.Add(
                $"{provider.DisplayName}: today {TokenFormatter.Grouped(provider.TodayTokens)} · " +
                $"month {TokenFormatter.Grouped(provider.MonthTokens)}{availability}");
        }
        CombinedText.Text =
            $"Today {TokenFormatter.Grouped(state.TodayTokens)} · Month {TokenFormatter.Grouped(state.MonthTokens)}";
        RefreshedText.Text = $"Refreshed {state.AsOfUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
    }

    public void ShowRefreshing()
    {
        RefreshedText.Text = "Refreshing…";
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            await app.RefreshFromUiAsync();
    }
}
