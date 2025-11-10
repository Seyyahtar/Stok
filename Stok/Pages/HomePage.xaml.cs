using Microsoft.Maui.Controls;

namespace Stok.Pages
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private async void OnStockClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(StockPage));
        }

        private async void OnCaseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CaseEntryPage));
        }

        private async void OnHistoryClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(HistoryPage));
        }

        private async void OnChecklistClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ChecklistPage));
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }
    }
}
