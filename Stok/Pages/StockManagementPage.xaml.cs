using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Stok.Helpers;
using Stok.Models;
using Stok.Services;

namespace Stok.Pages
{
    [QueryProperty(nameof(MaterialId), nameof(MaterialId))]
    [QueryProperty(nameof(Operation), nameof(Operation))]
    public partial class StockManagementPage : ContentPage, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly AuthService _authService;
        private readonly ObservableCollection<Material> _materials = new();
        private readonly ObservableCollection<string> _users = new();
        private Material? _selectedMaterial;
        private string? _selectedFrom;
        private string? _selectedTo;
        private string _quantityText = "1";
        private string? _pendingMaterialId;
        private StockOperation? _requestedOperation;

        public StockManagementPage()
        {
            InitializeComponent();
            BindingContext = this;

            _databaseService = ServiceHelper.GetRequiredService<DatabaseService>();
            _authService = ServiceHelper.GetRequiredService<AuthService>();
        }

        public ObservableCollection<Material> Materials => _materials;

        public ObservableCollection<string> Users => _users;

        public Material? SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                if (SetProperty(ref _selectedMaterial, value))
                {
                    OnPropertyChanged(nameof(HasSelectedMaterial));
                }
            }
        }

        public bool HasSelectedMaterial => SelectedMaterial != null;

        public string? SelectedFrom
        {
            get => _selectedFrom;
            set
            {
                if (SetProperty(ref _selectedFrom, value))
                {
                    OnPropertyChanged(nameof(ActionButtonText));
                }
            }
        }

        public string? SelectedTo
        {
            get => _selectedTo;
            set
            {
                if (SetProperty(ref _selectedTo, value))
                {
                    OnPropertyChanged(nameof(ActionButtonText));
                }
            }
        }

        public string QuantityText
        {
            get => _quantityText;
            set => SetProperty(ref _quantityText, value);
        }

        public string ActionButtonText => DetermineOperation() == StockOperation.Out ? "Stok Çıkar" : "Stok Ekle";

        public string? MaterialId
        {
            get => _pendingMaterialId;
            set => _pendingMaterialId = value;
        }

        public string? Operation
        {
            get => _requestedOperation?.ToString();
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _requestedOperation = null;
                }
                else
                {
                    _requestedOperation = string.Equals(value, "Out", StringComparison.OrdinalIgnoreCase)
                        ? StockOperation.Out
                        : StockOperation.In;
                }

                OnPropertyChanged(nameof(ActionButtonText));
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
            ApplyPendingSelection();
        }

        private async Task LoadDataAsync()
        {
            await _authService.InitializeAsync();
            await _databaseService.InitializeAsync();
            var materials = await _databaseService.GetMaterialsAsync();
            SynchronizeCollection(_materials, materials);

            if (SelectedMaterial != null)
            {
                var refreshed = _materials.FirstOrDefault(m => m.Id == SelectedMaterial.Id);
                if (refreshed != null && !ReferenceEquals(refreshed, SelectedMaterial))
                {
                    SelectedMaterial = refreshed;
                }
            }

            var users = await _databaseService.GetUsersAsync();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _users.Clear();
            _users.Add("Depo");
            seen.Add("Depo");

            foreach (var user in users)
            {
                if (!seen.Contains(user.Username))
                {
                    _users.Add(user.Username);
                    seen.Add(user.Username);
                }
            }

            if (_authService.CurrentUser != null && !seen.Contains(_authService.CurrentUser.Username))
            {
                _users.Add(_authService.CurrentUser.Username);
            }

            OnPropertyChanged(nameof(Users));
        }

        private void ApplyPendingSelection()
        {
            if (string.IsNullOrWhiteSpace(_pendingMaterialId))
            {
                return;
            }

            if (Guid.TryParse(_pendingMaterialId, out var materialId))
            {
                var match = _materials.FirstOrDefault(m => m.Id == materialId);
                if (match != null)
                {
                    SelectedMaterial = match;
                }
            }
        }

        private async void OnSubmit(object sender, EventArgs e)
        {
            if (SelectedMaterial == null)
            {
                await DisplayAlert("Uyarı", "Lütfen bir malzeme seçin.", "Tamam");
                return;
            }

            if (!int.TryParse(QuantityText, out var quantity) || quantity <= 0)
            {
                await DisplayAlert("Uyarı", "Geçerli bir miktar girin.", "Tamam");
                return;
            }

            var operation = DetermineOperation();
            if (operation == StockOperation.Out && SelectedMaterial.Quantity < quantity)
            {
                await DisplayAlert("Uyarı", "Stoktaki miktardan fazla çıkış yapılamaz.", "Tamam");
                return;
            }

            if (operation == StockOperation.Out)
            {
                SelectedMaterial.Quantity -= quantity;
            }
            else
            {
                SelectedMaterial.Quantity += quantity;
            }

            SelectedMaterial.UpdatedAt = DateTime.UtcNow;
            await _databaseService.SaveMaterialAsync(SelectedMaterial);

            await DisplayAlert("Başarılı", operation == StockOperation.Out ? "Stoktan çıkış yapıldı." : "Stoka giriş yapıldı.", "Tamam");
            await Shell.Current.GoToAsync("..");
        }

        private StockOperation DetermineOperation()
        {
            var currentUser = _authService.CurrentUser?.Username;

            if (!string.IsNullOrWhiteSpace(currentUser))
            {
                if (!string.IsNullOrWhiteSpace(SelectedFrom) && SelectedFrom.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                {
                    return StockOperation.Out;
                }

                if (!string.IsNullOrWhiteSpace(SelectedTo) && SelectedTo.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
                {
                    return StockOperation.In;
                }
            }

            if (!string.IsNullOrWhiteSpace(SelectedFrom) && SelectedFrom.Equals("Depo", StringComparison.OrdinalIgnoreCase))
            {
                return StockOperation.Out;
            }

            if (!string.IsNullOrWhiteSpace(SelectedTo) && SelectedTo.Equals("Depo", StringComparison.OrdinalIgnoreCase))
            {
                return StockOperation.In;
            }

            return _requestedOperation ?? StockOperation.In;
        }

        private static void SynchronizeCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            var items = source.ToList();
            for (var i = target.Count - 1; i >= 0; i--)
            {
                if (!items.Contains(target[i]))
                {
                    target.RemoveAt(i);
                }
            }

            foreach (var item in items)
            {
                if (!target.Contains(item))
                {
                    target.Add(item);
                }
            }
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private enum StockOperation
        {
            In,
            Out
        }
    }
}
