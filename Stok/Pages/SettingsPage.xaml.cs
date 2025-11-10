using System;
using System.IO;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Stok.Helpers;
using Stok.Models;
using Stok.Pages;
using Stok.Services;

namespace Stok.Pages
{
    public partial class SettingsPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly DatabaseService _databaseService;
        private readonly ExcelService _excelService;

        public SettingsPage()
        {
            InitializeComponent();
            _authService = ServiceHelper.GetRequiredService<AuthService>();
            _databaseService = ServiceHelper.GetRequiredService<DatabaseService>();
            _excelService = ServiceHelper.GetRequiredService<ExcelService>();
        }

        private async void OnChangeEmail(object sender, EventArgs e)
        {
            await _authService.InitializeAsync();
            if (_authService.CurrentUser == null)
            {
                await DisplayAlert("Bilgi", "Önce giriş yapmalısınız.", "Tamam");
                return;
            }

            var newEmail = await DisplayPromptAsync("E-posta Güncelle", "Yeni e-posta adresinizi girin:", placeholder: _authService.CurrentUser.Email);
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                return;
            }

            var updated = await _authService.UpdateEmailAsync(newEmail);
            await DisplayAlert("Sonuç", updated ? "E-posta güncellendi." : "E-posta güncellenemedi.", "Tamam");
        }

        private async void OnChangePassword(object sender, EventArgs e)
        {
            await _authService.InitializeAsync();
            if (_authService.CurrentUser == null)
            {
                await DisplayAlert("Bilgi", "Önce giriş yapmalısınız.", "Tamam");
                return;
            }

            var current = await DisplayPromptAsync("Şifre Güncelle", "Mevcut şifrenizi girin:", isPassword: true);
            if (current == null)
            {
                return;
            }

            var newPassword = await DisplayPromptAsync("Şifre Güncelle", "Yeni şifreyi girin:", isPassword: true);
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return;
            }

            var updated = await _authService.UpdatePasswordAsync(current, newPassword);
            await DisplayAlert("Sonuç", updated ? "Şifreniz güncellendi." : "Şifre güncellenemedi.", "Tamam");
        }

        private async void OnLogout(object sender, EventArgs e)
        {
            _authService.Logout();
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }

        private async void OnClearStock(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Stok Temizle", "Tüm stok kayıtlarını silmek istediğinize emin misiniz?", "Evet", "Hayır");
            if (!confirm)
            {
                return;
            }

            await _databaseService.InitializeAsync();
            var materials = await _databaseService.GetMaterialsAsync();
            foreach (var material in materials)
            {
                await _databaseService.DeleteMaterialAsync(material);
            }

            await DisplayAlert("Bilgi", "Tüm stok kayıtları silindi.", "Tamam");
        }

        private async void OnClearHistory(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Geçmiş Temizle", "Tüm geçmiş kayıtlarını silmek istediğinize emin misiniz?", "Evet", "Hayır");
            if (!confirm)
            {
                return;
            }

            await _databaseService.InitializeAsync();
            var history = await _databaseService.GetHistoryAsync();
            foreach (var item in history)
            {
                await _databaseService.DeleteHistoryItemAsync(item);
            }

            await DisplayAlert("Bilgi", "Geçmiş kayıtları temizlendi.", "Tamam");
        }

        private async void OnExportData(object sender, EventArgs e)
        {
            try
            {
                await _databaseService.InitializeAsync();
                var materials = await _databaseService.GetMaterialsAsync();
                var cases = await _databaseService.GetCaseRecordsAsync();
                var history = await _databaseService.GetHistoryAsync();
                var checklist = await _databaseService.GetChecklistItemsAsync();

                var stream = await _excelService.ExportAllDataAsync(materials, cases, history, checklist);
                var fileName = $"stok-yedek-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                await using (var fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fileStream);
                }

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "S-Tok Yedek",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Dışa aktarım başarısız oldu: {ex.Message}", "Tamam");
            }
        }
    }
}
