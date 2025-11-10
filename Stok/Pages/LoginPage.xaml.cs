using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Stok.Helpers;
using Stok.Services;

namespace Stok.Pages
{
    public partial class LoginPage : ContentPage, INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private bool _isRegistering;
        private string _username = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string? _message;
        private Color _messageColor = Colors.Red;

        public LoginPage()
        {
            InitializeComponent();
            BindingContext = this;

            _authService = ServiceHelper.GetRequiredService<AuthService>();
            UpdateButtonColors();
        }

        public bool IsRegistering
        {
            get => _isRegistering;
            set
            {
                if (SetProperty(ref _isRegistering, value))
                {
                    OnPropertyChanged(nameof(LoginButtonColor));
                    OnPropertyChanged(nameof(RegisterButtonColor));
                }
            }
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public string? Message
        {
            get => _message;
            private set
            {
                if (SetProperty(ref _message, value))
                {
                    OnPropertyChanged(nameof(HasMessage));
                }
            }
        }

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public Color MessageColor
        {
            get => _messageColor;
            private set => SetProperty(ref _messageColor, value);
        }

        public Color LoginButtonColor => IsRegistering ? Colors.Gray : Color.FromArgb("#512BD4");

        public Color RegisterButtonColor => IsRegistering ? Color.FromArgb("#512BD4") : Colors.Gray;

        public new event PropertyChangedEventHandler? PropertyChanged;

        private async void OnSubmit(object sender, EventArgs e)
        {
            ClearMessage();
            await _authService.InitializeAsync();

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ShowMessage("Kullanıcı adı ve şifre gereklidir.", true);
                return;
            }

            if (IsRegistering)
            {
                if (!IsValidEmail(Email))
                {
                    ShowMessage("Geçerli bir e-posta girin.", true);
                    return;
                }

                if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
                {
                    ShowMessage("Şifreler uyuşmuyor.", true);
                    return;
                }

                var success = await _authService.RegisterAsync(Username, Password, Email);
                if (!success)
                {
                    ShowMessage("Kullanıcı adı zaten kayıtlı.", true);
                    return;
                }

                ShowMessage("Kayıt başarılı, yönlendiriliyor...", false);
                await Task.Delay(500);
                Application.Current.MainPage = new AppShell();
                return;
            }

            var result = await _authService.LoginAsync(Username, Password);
            if (!result)
            {
                ShowMessage("Kullanıcı adı veya şifre hatalı.", true);
                return;
            }

            Application.Current.MainPage = new AppShell();
        }

        private void OnShowLogin(object sender, EventArgs e)
        {
            IsRegistering = false;
            UpdateButtonColors();
        }

        private void OnShowRegister(object sender, EventArgs e)
        {
            IsRegistering = true;
            UpdateButtonColors();
        }

        private void UpdateButtonColors()
        {
            OnPropertyChanged(nameof(LoginButtonColor));
            OnPropertyChanged(nameof(RegisterButtonColor));
        }

        private void ShowMessage(string text, bool isError)
        {
            MessageColor = isError ? Colors.Red : Color.FromArgb("#2E7D32");
            Message = text;
        }

        private void ClearMessage()
        {
            Message = null;
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return Regex.IsMatch(email, "^[^@\s]+@[^@\s]+\\.[^@\s]+$");
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
