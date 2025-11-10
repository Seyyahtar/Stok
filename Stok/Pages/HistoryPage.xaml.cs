using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Stok.Helpers;
using Stok.Models;
using Stok.Services;

namespace Stok.Pages
{
    public partial class HistoryPage : ContentPage, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly ExcelService _excelService;
        private readonly ObservableCollection<HistoryDisplayItem> _historyItems = new();
        private readonly List<HistoryDisplayItem> _allItems = new();
        private HistoryFilter _activeFilter = HistoryFilter.All;
        private string _searchText = string.Empty;

        public HistoryPage()
        {
            InitializeComponent();
            BindingContext = this;

            _databaseService = ServiceHelper.GetRequiredService<DatabaseService>();
            _excelService = ServiceHelper.GetRequiredService<ExcelService>();
        }

        public ObservableCollection<HistoryDisplayItem> HistoryItems => _historyItems;

        public Color AllFilterColor => GetFilterColor(HistoryFilter.All);

        public Color StockFilterColor => GetFilterColor(HistoryFilter.Stock);

        public Color CaseFilterColor => GetFilterColor(HistoryFilter.Case);

        public Color ChecklistFilterColor => GetFilterColor(HistoryFilter.Checklist);

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            await _databaseService.InitializeAsync();
            var items = await _databaseService.GetHistoryAsync();

            _allItems.Clear();
            foreach (var item in items.OrderByDescending(entry => entry.CreatedAt))
            {
                _allItems.Add(BuildDisplayItem(item));
            }

            ApplyFilters();
        }

        private HistoryDisplayItem BuildDisplayItem(HistoryItem item)
        {
            var details = new List<string>();
            foreach (var record in item.Details)
            {
                if (item.Type == HistoryType.Checklist)
                {
                    var status = record.Quantity > 0 ? "Tamamlandı" : "Beklemede";
                    var parts = new List<string> { record.Name };
                    if (!string.IsNullOrWhiteSpace(record.SerialOrLot))
                    {
                        parts.Add(record.SerialOrLot);
                    }

                    parts.Add(status);
                    details.Add(string.Join(" • ", parts));
                    continue;
                }

                var segments = new List<string> { record.Name };
                if (!string.IsNullOrWhiteSpace(record.Serial))
                {
                    segments.Add($"Seri: {record.Serial}");
                }
                else if (!string.IsNullOrWhiteSpace(record.Lot))
                {
                    segments.Add($"Lot: {record.Lot}");
                }
                else if (!string.IsNullOrWhiteSpace(record.SerialOrLot))
                {
                    segments.Add(record.SerialOrLot);
                }

                if (record.ExpiryDate.HasValue)
                {
                    segments.Add($"SKT: {record.ExpiryDate:dd.MM.yyyy}");
                }

                segments.Add($"Adet: {record.Quantity}");
                details.Add(string.Join(" • ", segments));
            }

            var display = new HistoryDisplayItem(item)
            {
                Details = details,
                CreatedByDisplay = string.IsNullOrWhiteSpace(item.CreatedBy)
                    ? "Sistem"
                    : $"İşlemi yapan: {item.CreatedBy}"
            };

            return display;
        }

        private void ApplyFilters()
        {
            var filtered = _allItems.Where(item => FilterMatches(item) && SearchMatches(item)).ToList();

            _historyItems.Clear();
            foreach (var item in filtered)
            {
                _historyItems.Add(item);
            }

            OnPropertyChanged(nameof(AllFilterColor));
            OnPropertyChanged(nameof(StockFilterColor));
            OnPropertyChanged(nameof(CaseFilterColor));
            OnPropertyChanged(nameof(ChecklistFilterColor));
        }

        private bool FilterMatches(HistoryDisplayItem item)
        {
            return _activeFilter switch
            {
                HistoryFilter.Stock => item.Type is HistoryType.StockIn or HistoryType.StockOut or HistoryType.Delete,
                HistoryFilter.Case => item.Type == HistoryType.Case,
                HistoryFilter.Checklist => item.Type == HistoryType.Checklist,
                _ => true
            };
        }

        private bool SearchMatches(HistoryDisplayItem item)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                return true;
            }

            if (item.Summary.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return item.Details.Any(detail => detail.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        private Color GetFilterColor(HistoryFilter filter)
        {
            return _activeFilter == filter ? Color.FromArgb("#512BD4") : Color.FromArgb("#E0E0E0");
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue ?? string.Empty;
            ApplyFilters();
        }

        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not string parameter)
            {
                return;
            }

            _activeFilter = parameter switch
            {
                "Stock" => HistoryFilter.Stock,
                "Case" => HistoryFilter.Case,
                "Checklist" => HistoryFilter.Checklist,
                _ => HistoryFilter.All
            };

            ApplyFilters();
        }

        private async void OnExportFiltered(object sender, EventArgs e)
        {
            if (_historyItems.Count == 0)
            {
                await DisplayAlert("Bilgi", "Dışa aktarılacak kayıt bulunamadı.", "Tamam");
                return;
            }

            await ExportAsync(_historyItems.Select(item => item.Source));
        }

        private async void OnExportHistoryItem(object sender, EventArgs e)
        {
            if (sender is not SwipeItem swipe || swipe.CommandParameter is not HistoryDisplayItem item)
            {
                return;
            }

            await ExportAsync(new[] { item.Source });
        }

        private async Task ExportAsync(IEnumerable<HistoryItem> items)
        {
            try
            {
                var stream = await _excelService.ExportHistoryAsync(items);
                var fileName = $"gecmis-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                await using (var fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write))
                {
                    await stream.CopyToAsync(fileStream);
                }

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Geçmiş Dışa Aktarım",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Excel dışa aktarımı başarısız oldu: {ex.Message}", "Tamam");
            }
        }

        private async void OnUndoHistory(object sender, EventArgs e)
        {
            if (sender is not SwipeItem swipe || swipe.CommandParameter is not HistoryDisplayItem item)
            {
                return;
            }

            if (!item.Reversible)
            {
                await DisplayAlert("Bilgi", "Bu kayıt geri alınamaz.", "Tamam");
                return;
            }

            var confirm = await DisplayAlert("Geri Al", "İşlemi geri almak istediğinizden emin misiniz?", "Evet", "Hayır");
            if (!confirm)
            {
                return;
            }

            try
            {
                switch (item.Type)
                {
                    case HistoryType.Case:
                        await UndoCaseAsync(item);
                        break;
                    case HistoryType.StockIn:
                    case HistoryType.StockOut:
                        await UndoStockAsync(item);
                        break;
                    case HistoryType.Delete:
                        await UndoDeleteAsync(item);
                        break;
                    case HistoryType.Checklist:
                        await UndoChecklistAsync(item);
                        break;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Geri alma sırasında hata oluştu: {ex.Message}", "Tamam");
            }
        }

        private async Task UndoCaseAsync(HistoryDisplayItem item)
        {
            if (item.Source.ReferenceId.HasValue)
            {
                var record = await _databaseService.GetCaseRecordAsync(item.Source.ReferenceId.Value);
                if (record != null)
                {
                    await _databaseService.DeleteCaseRecordAsync(record);
                }
            }

            foreach (var detail in item.Source.Details)
            {
                Material? material = null;
                if (detail.MaterialId.HasValue)
                {
                    material = await _databaseService.GetMaterialAsync(detail.MaterialId.Value);
                }

                if (material == null && !string.IsNullOrWhiteSpace(detail.Serial))
                {
                    material = await FindMaterialBySerialAsync(detail.Serial);
                }

                if (material == null && !string.IsNullOrWhiteSpace(detail.Lot))
                {
                    material = await FindMaterialByLotAsync(detail.Lot);
                }

                if (material == null)
                {
                    material = new Material
                    {
                        Name = detail.Name,
                        Serial = detail.Serial,
                        Lot = detail.Lot,
                        ExpiryDate = detail.ExpiryDate,
                        Quantity = 0,
                        OwnerUser = item.Source.CreatedBy
                    };
                }

                material.Quantity += detail.Quantity;
                await _databaseService.SaveMaterialAsync(material);
            }

            await _databaseService.DeleteHistoryItemAsync(item.Source);
            _allItems.Remove(item);
            _historyItems.Remove(item);
            ApplyFilters();
        }

        private async Task UndoStockAsync(HistoryDisplayItem item)
        {
            var isStockIn = item.Type == HistoryType.StockIn;
            foreach (var detail in item.Source.Details)
            {
                Material? material = null;
                if (detail.MaterialId.HasValue)
                {
                    material = await _databaseService.GetMaterialAsync(detail.MaterialId.Value);
                }

                if (material == null && !string.IsNullOrWhiteSpace(detail.Serial))
                {
                    material = await FindMaterialBySerialAsync(detail.Serial);
                }

                if (material == null && !string.IsNullOrWhiteSpace(detail.Lot))
                {
                    material = await FindMaterialByLotAsync(detail.Lot);
                }

                if (material == null)
                {
                    if (isStockIn)
                    {
                        // Orijinal stok girişi bulunamadı, geri alma işlemi atlanıyor.
                        continue;
                    }

                    material = new Material
                    {
                        Name = detail.Name,
                        Serial = detail.Serial,
                        Lot = detail.Lot,
                        ExpiryDate = detail.ExpiryDate,
                        OwnerUser = item.Source.CreatedBy
                    };
                }

                material.Quantity += isStockIn ? -detail.Quantity : detail.Quantity;
                if (material.Quantity < 0)
                {
                    material.Quantity = 0;
                }

                await _databaseService.SaveMaterialAsync(material);
            }

            await _databaseService.DeleteHistoryItemAsync(item.Source);
            _allItems.Remove(item);
            _historyItems.Remove(item);
            ApplyFilters();
        }

        private async Task UndoDeleteAsync(HistoryDisplayItem item)
        {
            foreach (var detail in item.Source.Details)
            {
                var material = new Material
                {
                    Name = detail.Name,
                    Serial = detail.Serial,
                    Lot = detail.Lot,
                    ExpiryDate = detail.ExpiryDate,
                    Quantity = detail.Quantity,
                    OwnerUser = item.Source.CreatedBy
                };

                await _databaseService.SaveMaterialAsync(material);
            }

            await _databaseService.DeleteHistoryItemAsync(item.Source);
            _allItems.Remove(item);
            _historyItems.Remove(item);
            ApplyFilters();
        }

        private async Task UndoChecklistAsync(HistoryDisplayItem item)
        {
            // For checklist reversals we simply remove the history record.
            await _databaseService.DeleteHistoryItemAsync(item.Source);
            _allItems.Remove(item);
            _historyItems.Remove(item);
            ApplyFilters();
        }

        private async Task<Material?> FindMaterialBySerialAsync(string serial)
        {
            var materials = await _databaseService.GetMaterialsAsync();
            return materials.FirstOrDefault(m => string.Equals(m.Serial, serial, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<Material?> FindMaterialByLotAsync(string lot)
        {
            var materials = await _databaseService.GetMaterialsAsync();
            return materials.FirstOrDefault(m => string.Equals(m.Lot, lot, StringComparison.OrdinalIgnoreCase));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class HistoryDisplayItem
    {
        public HistoryDisplayItem(HistoryItem source)
        {
            Source = source;
        }

        public HistoryItem Source { get; }

        public HistoryType Type => Source.Type;

        public string Summary => Source.Summary;

        public DateTime CreatedAt => Source.CreatedAt;

        public string CreatedByDisplay { get; set; } = string.Empty;

        public IReadOnlyList<string> Details { get; set; } = Array.Empty<string>();

        public bool Reversible => Source.Reversible;
    }

    public enum HistoryFilter
    {
        All,
        Stock,
        Case,
        Checklist
    }
}
