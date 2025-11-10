using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Stok.Models;

namespace Stok.Services
{
    public class AuthService
    {
        private const string CurrentUserPreferenceKey = "stok.currentUser";

        private readonly DatabaseService _databaseService;
        private User? _currentUser;

        public AuthService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public User? CurrentUser => _currentUser;

        public async Task InitializeAsync()
        {
            await _databaseService.InitializeAsync();
            await EnsureDepotUserAsync();

            if (_currentUser != null)
            {
                return;
            }

            if (!Preferences.ContainsKey(CurrentUserPreferenceKey))
            {
                return;
            }

            var stored = Preferences.Get(CurrentUserPreferenceKey, string.Empty);
            if (!Guid.TryParse(stored, out var userId))
            {
                return;
            }

            var user = await _databaseService.GetUserAsync(userId);
            _currentUser = user;
        }

        public async Task<bool> RegisterAsync(string username, string password, string email)
        {
            await _databaseService.InitializeAsync();
            var normalizedUsername = username.Trim();
            var existing = await _databaseService.FindUserByUsernameAsync(normalizedUsername);
            if (existing != null)
            {
                return false;
            }

            var user = new User
            {
                Username = normalizedUsername,
                PasswordHash = HashPassword(password),
                Email = email.Trim()
            };

            await _databaseService.SaveUserAsync(user);
            _currentUser = user;
            Preferences.Set(CurrentUserPreferenceKey, user.Id.ToString());
            return true;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            await _databaseService.InitializeAsync();
            var user = await _databaseService.FindUserByUsernameAsync(username.Trim());
            if (user == null)
            {
                return false;
            }

            var hash = HashPassword(password);
            if (!string.Equals(hash, user.PasswordHash, StringComparison.Ordinal))
            {
                return false;
            }

            _currentUser = user;
            Preferences.Set(CurrentUserPreferenceKey, user.Id.ToString());
            return true;
        }

        public void Logout()
        {
            _currentUser = null;
            Preferences.Remove(CurrentUserPreferenceKey);
        }

        public async Task<bool> UpdateEmailAsync(string newEmail)
        {
            if (_currentUser == null)
            {
                return false;
            }

            _currentUser.Email = newEmail.Trim();
            await _databaseService.SaveUserAsync(_currentUser);
            return true;
        }

        public async Task<bool> UpdatePasswordAsync(string currentPassword, string newPassword)
        {
            if (_currentUser == null)
            {
                return false;
            }

            var currentHash = HashPassword(currentPassword);
            if (!string.Equals(currentHash, _currentUser.PasswordHash, StringComparison.Ordinal))
            {
                return false;
            }

            _currentUser.PasswordHash = HashPassword(newPassword);
            await _databaseService.SaveUserAsync(_currentUser);
            return true;
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes);
        }

        private async Task EnsureDepotUserAsync()
        {
            var depot = await _databaseService.FindUserByUsernameAsync("Depo");
            if (depot != null)
            {
                return;
            }

            var user = new User
            {
                Username = "Depo",
                Email = "depo@stok.local",
                PasswordHash = HashPassword(Guid.NewGuid().ToString("N"))
            };

            await _databaseService.SaveUserAsync(user);
        }
    }
}
