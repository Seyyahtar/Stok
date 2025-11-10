using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Stok.Helpers;
using Stok.Pages;
using Stok.Services;

namespace Stok
{
    public partial class App : Application
    {
        private readonly AuthService _authService;

        public App(IServiceProvider serviceProvider, AuthService authService)
        {
            InitializeComponent();

            ServiceHelper.Initialize(serviceProvider);
            _authService = authService;

            MainPage = new ContentPage();
            _ = DetermineStartupPageAsync();
        }

        private async Task DetermineStartupPageAsync()
        {
            await _authService.InitializeAsync();

            if (_authService.CurrentUser == null)
            {
                MainPage = new NavigationPage(new LoginPage());
            }
            else
            {
                MainPage = new AppShell();
            }
        }
    }
}
