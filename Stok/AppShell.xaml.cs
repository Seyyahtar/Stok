using Microsoft.Maui.Controls;
using Stok.Pages;

namespace Stok
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute(nameof(StockPage), typeof(StockPage));
            Routing.RegisterRoute(nameof(CaseEntryPage), typeof(CaseEntryPage));
            Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
            Routing.RegisterRoute(nameof(ChecklistPage), typeof(ChecklistPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(StockManagementPage), typeof(StockManagementPage));
            Routing.RegisterRoute(nameof(DeviceCountsPage), typeof(DeviceCountsPage));
        }
    }
}
