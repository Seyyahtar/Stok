using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using Stok.Helpers;
using Stok.Models;
using Stok.Services;

namespace Stok.Pages
{
    public partial class StockPage : ContentPage, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly ExcelService _excelService;
        private readonly AuthService _authService;
        private readonly List<Material> _allMaterials = new();
        private readonly List<Material> _currentFiltered = new();
        private string _searchText = string.Empty;
        private bool _isMenuOpen;
        private SortState _expirySortState = SortState.Default;
        private SortState _quantitySortState = SortState.Default;
        private int _filteredTotal;

        public StockPage()
        {
            InitializeComponent();
            BindingContext = this;

            _databaseService = ServiceHelper.GetRequiredService<DatabaseService>();
            _excelService = ServiceHelper.GetRequiredService<ExcelService>();
            _authService = ServiceHelper.GetRequiredService<AuthService>();

            GroupedMaterials = new ObservableCollection<MaterialGroup>();
            FilterOptions = new ObservableCollection<FilterOption>();

            InitializeFilters();
        }

        public ObservableCollection<MaterialGroup> GroupedMaterials { get; }

        public ObservableCollection<FilterOption> FilterOptions { get; }

        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set => SetProperty(ref _isMenuOpen, value);
        }

        public Color ExpirySortColor => GetSortColor(_expirySortState);

        public Color QuantitySortColor => GetSortColor(_quantitySortState);

        public string TotalQuantityDisplay => $"Toplam Adet: {_filteredTotal}";

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadMaterialsAsync();
        }

        private async Task LoadMaterialsAsync()
        {
            await _databaseService.InitializeAsync();
            var materials = await _databaseService.GetMaterialsAsync();

            if (materials.Count == 0)
            {
                materials = await SeedInitialMaterialsAsync();
            }

            _allMaterials.Clear();
            _allMaterials.AddRange(materials);
            RefreshGroupedMaterials();
        }

        private async Task<IList<Material>> SeedInitialMaterialsAsync()
        {
            var seed = new List<Material>
            {
                new()
                {
                    Name = "Medtronic Solia S 60 cm",
                    Serial = "SOLIA-6001",
                    ExpiryDate = new DateTime(2026, 7, 31),
                    Quantity = 12,
                    OwnerUser = "Depo"
                },
                new()
                {
                    Name = "Biotronik Rivacor 7 HF-T",
                    Lot = "RV7HFT-2024",
                    ExpiryDate = new DateTime(2025, 12, 15),
                    Quantity = 3,
                    OwnerUser = "Depo"
                },
                new()
                {
                    Name = "Boston Scientific SafeSheath II",
                    Lot = "SS2-4589",
                    Quantity = 18,
                    OwnerUser = "Depo"
                },
                new()
                {
                    Name = "Medtronic Amvia Sky",
                    Serial = "AMV-SKY-01",
                    ExpiryDate = new DateTime(2027, 1, 5),
                    Quantity = 4,
                    OwnerUser = "Depo"
                }
            };

            foreach (var material in seed)
            {
                await _databaseService.SaveMaterialAsync(material);
            }

            return seed;
        }

        private void InitializeFilters()
        {
            FilterOptions.Clear();
            FilterOptions.Add(new FilterOption("Lead", "solia", "sentus", "plexa"));
            FilterOptions.Add(new FilterOption("Sheath", "safesheath", "adelante", "li-7", "li-8"));
            FilterOptions.Add(new FilterOption("Pacemaker", "amvia sky", "endicos", "enitra", "edora"));
            FilterOptions.Add(new FilterOption("ICD", "vr-t", "dr-t"));
            FilterOptions.Add(new FilterOption("CRT", "hf-t"));
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private void OnToggleMenu(object sender, EventArgs e)
        {
            IsMenuOpen = !IsMenuOpen;
        }

        private async void OnFilterIconClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Bilgi", "Filtre simgesi şu an bilgi amaçlıdır. Arama ve hazır filtreleri kullanabilirsiniz.", "Tamam");
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue ?? string.Empty;
            RefreshGroupedMaterials();
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not FilterOption option)
            {
                return;
            }

            var activate = !option.IsActive;
            foreach (var filter in FilterOptions)
            {
                filter.IsActive = false;
            }

            option.IsActive = activate;
            RefreshGroupedMaterials();
        }

        private void OnExpirySortClicked(object sender, EventArgs e)
        {
            _expirySortState = CycleSortState(_expirySortState);
            RefreshGroupedMaterials();
        }

        private void OnQuantitySortClicked(object sender, EventArgs e)
        {
            _quantitySortState = CycleSortState(_quantitySortState);
            RefreshGroupedMaterials();
        }

        private void OnToggleGroupClicked(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not MaterialGroup group)
            {
                return;
            }

            group.IsExpanded = !group.IsExpanded;
        }

        private async void OnDeleteMaterial(object sender, EventArgs e)
        {
            if (sender is not SwipeItem swipe || swipe.CommandParameter is not Material material)
            {
                return;
            }

            var confirmed = await DisplayAlert("Sil", $"{material.Name} kaydını silmek istediğinize emin misiniz?", "Sil", "İptal");
            if (!confirmed)
            {
                return;
            }

            await _databaseService.DeleteMaterialAsync(material);
            _allMaterials.Remove(material);
            RefreshGroupedMaterials();
        }

        private async void OnWithdrawMaterial(object sender, EventArgs e)
        {
            if (sender is not SwipeItem swipe || swipe.CommandParameter is not Material material)
            {
                return;
            }

            var route = $"{nameof(StockManagementPage)}?MaterialId={material.Id}&Operation=Out";
            await Shell.Current.GoToAsync(route);
        }

        private async void OnEditMaterial(object sender, EventArgs e)
        {
            if (sender is not SwipeItem swipe || swipe.CommandParameter is not Material material)
            {
                return;
            }

            var newName = await DisplayPromptAsync("Malzeme Adı", "Yeni malzeme adını girin:", "Kaydet", "İptal", initialValue: material.Name);
            if (string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            var quantityText = await DisplayPromptAsync("Miktar", "Yeni miktarı girin:", "Devam", "İptal", initialValue: material.Quantity.ToString(), keyboard: Keyboard.Numeric);
            if (quantityText == null || !int.TryParse(quantityText, out var newQuantity) || newQuantity < 0)
            {
                await DisplayAlert("Uyarı", "Geçerli bir miktar girin.", "Tamam");
                return;
            }

            var owner = await DisplayPromptAsync("Sahip", "Malzeme sahibini güncelleyin:", "Devam", "Atla", initialValue: material.OwnerUser);
            owner ??= material.OwnerUser;

            var confirm = await DisplayAlert("Onay", "Değişiklikleri kaydetmek istiyor musunuz?", "Onayla", "İptal");
            if (!confirm)
            {
                return;
            }

            material.Name = newName.Trim();
            material.Quantity = newQuantity;
            material.OwnerUser = owner;
            material.UpdatedAt = DateTime.UtcNow;

            await _databaseService.SaveMaterialAsync(material);
            RefreshGroupedMaterials();
        }

        private async void OnOpenStockManagement(object sender, EventArgs e)
        {
            IsMenuOpen = false;
            await Shell.Current.GoToAsync(nameof(StockManagementPage));
        }

        private async void OnShowDeviceCounts(object sender, EventArgs e)
        {
            IsMenuOpen = false;
            await Shell.Current.GoToAsync(nameof(DeviceCountsPage));
        }

        private async void OnImportExcel(object sender, EventArgs e)
        {
            IsMenuOpen = false;
            try
            {
                var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "com.microsoft.excel.xls", "org.openxmlformats.spreadsheetml.sheet" } },
                    { DevicePlatform.MacCatalyst, new[] { "com.microsoft.excel.xls", "org.openxmlformats.spreadsheetml.sheet" } },
                    { DevicePlatform.WinUI, new[] { ".xls", ".xlsx" } },
                    { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel" } }
                });

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Excel dosyasını seçin",
                    FileTypes = fileTypes
                });

                if (result == null)
                {
                    return;
                }

                await using var stream = await result.OpenReadAsync();
                var imported = await _excelService.ImportMaterialsAsync(stream);

                var duplicates = new List<string>();
                foreach (var material in imported)
                {
                    material.OwnerUser = string.IsNullOrWhiteSpace(material.OwnerUser)
                        ? _authService.CurrentUser?.Username ?? "Depo"
                        : material.OwnerUser;

                    if (IsDuplicate(material))
                    {
                        duplicates.Add(material.Name);
                        continue;
                    }

                    await _databaseService.SaveMaterialAsync(material);
                    _allMaterials.Add(material);
                }

                RefreshGroupedMaterials();

                if (duplicates.Count > 0)
                {
                    var message = string.Join(Environment.NewLine, duplicates.Distinct());
                    await DisplayAlert("Yinelenen Kayıt", $"Aşağıdaki malzemeler zaten mevcut olduğu için eklenmedi:{Environment.NewLine}{message}", "Tamam");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Excel içe aktarımı başarısız oldu: {ex.Message}", "Tamam");
            }
        }

        private async void OnExportExcel(object sender, EventArgs e)
        {
            IsMenuOpen = false;
            try
            {
                var materials = _currentFiltered.Count > 0 ? _currentFiltered : _allMaterials;
                var stream = await _excelService.ExportMaterialsAsync(materials);
                var fileName = $"stok-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                await using (var fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fileStream);
                }

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Stok Dışa Aktarım",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Excel dışa aktarımı başarısız oldu: {ex.Message}", "Tamam");
            }
        }

        private bool IsDuplicate(Material material)
        {
            return _allMaterials.Any(existing =>
                string.Equals(existing.Name, material.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.Serial ?? string.Empty, material.Serial ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.Lot ?? string.Empty, material.Lot ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshGroupedMaterials()
        {
            IEnumerable<Material> query = _allMaterials;

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                query = query.Where(m => m.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                                         (m.Serial?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                         (m.Lot?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var activeFilter = FilterOptions.FirstOrDefault(f => f.IsActive);
            if (activeFilter != null)
            {
                query = query.Where(m => ContainsKeyword(m.Name, activeFilter.Keywords));
            }

            var materialList = query.ToList();

            materialList = ApplySorting(materialList);

            _currentFiltered.Clear();
            _currentFiltered.AddRange(materialList);
            _filteredTotal = materialList.Sum(m => m.Quantity);

            var existingStates = GroupedMaterials.ToDictionary(g => g.Key, g => g.IsExpanded);
            var grouped = materialList.GroupBy(m => GetGroupKey(m.Name)).ToList();

            for (var i = GroupedMaterials.Count - 1; i >= 0; i--)
            {
                if (grouped.All(g => g.Key != GroupedMaterials[i].Key))
                {
                    GroupedMaterials.RemoveAt(i);
                }
            }

            foreach (var group in grouped)
            {
                var existing = GroupedMaterials.FirstOrDefault(g => g.Key == group.Key);
                if (existing == null)
                {
                    existing = new MaterialGroup(group.Key);
                    if (existingStates.TryGetValue(group.Key, out var expanded))
                    {
                        existing.IsExpanded = expanded;
                    }

                    GroupedMaterials.Add(existing);
                }

                existing.ReplaceItems(group.ToList());
            }

            var orderedGroups = GroupedMaterials.OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase).ToList();
            for (var index = 0; index < orderedGroups.Count; index++)
            {
                var desired = orderedGroups[index];
                var currentIndex = GroupedMaterials.IndexOf(desired);
                if (currentIndex != index)
                {
                    GroupedMaterials.Move(currentIndex, index);
                }
            }

            OnPropertyChanged(nameof(TotalQuantityDisplay));
            OnPropertyChanged(nameof(ExpirySortColor));
            OnPropertyChanged(nameof(QuantitySortColor));
        }

        private List<Material> ApplySorting(List<Material> materials)
        {
            IOrderedEnumerable<Material>? ordered = null;

            if (_expirySortState != SortState.Default)
            {
                ordered = _expirySortState == SortState.Ascending
                    ? materials.OrderBy(m => m.ExpiryDate ?? DateTime.MaxValue)
                    : materials.OrderByDescending(m => m.ExpiryDate ?? DateTime.MinValue);
            }

            if (_quantitySortState != SortState.Default)
            {
                if (ordered == null)
                {
                    ordered = _quantitySortState == SortState.Ascending
                        ? materials.OrderBy(m => m.Quantity)
                        : materials.OrderByDescending(m => m.Quantity);
                }
                else
                {
                    ordered = _quantitySortState == SortState.Ascending
                        ? ordered.ThenBy(m => m.Quantity)
                        : ordered.ThenByDescending(m => m.Quantity);
                }
            }

            if (ordered != null)
            {
                return ordered.ThenBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            }

            return materials.OrderBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private static string GetGroupKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }

            var character = char.ToUpper(name[0]);
            return char.IsLetterOrDigit(character) ? character.ToString() : "?";
        }

        private static bool ContainsKeyword(string name, IEnumerable<string> keywords)
        {
            foreach (var keyword in keywords)
            {
                if (name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static SortState CycleSortState(SortState current)
        {
            return current switch
            {
                SortState.Default => SortState.Ascending,
                SortState.Ascending => SortState.Descending,
                _ => SortState.Default
            };
        }

        private static Color GetSortColor(SortState state)
        {
            return state switch
            {
                SortState.Ascending => Color.FromArgb("#C62828"),
                SortState.Descending => Color.FromArgb("#2E7D32"),
                _ => Color.FromArgb("#1F1F1F")
            };
        }

        private void SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
            {
                return;
            }

            backingStore = value;
            OnPropertyChanged(propertyName);
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private enum SortState
        {
            Default,
            Ascending,
            Descending
        }
    }
}
