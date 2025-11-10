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
            DetermineStartupPage();
        }

        private async void DetermineStartupPage()
        {
            var authService = ServiceHelper.GetRequiredService<AuthService>();
            await authService.InitializeAsync();

            if (authService.CurrentUser == null)
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
