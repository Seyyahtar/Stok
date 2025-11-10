using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Stok.Helpers;
using Stok.Models;
using Stok.Services;

namespace Stok.Pages
{
    public partial class ChecklistPage : ContentPage, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly ExcelService _excelService;
        private readonly AuthService _authService;
        private readonly ObservableCollection<ChecklistDisplayItem> _items = new();
        private bool _isLoaded;

        public ChecklistPage()
        {
            InitializeComponent();
            BindingContext = this;

            _databaseService = ServiceHelper.GetRequiredService<DatabaseService>();
            _excelService = ServiceHelper.GetRequiredService<ExcelService>();
            _authService = ServiceHelper.GetRequiredService<AuthService>();
        }

        public ObservableCollection<ChecklistDisplayItem> ChecklistItems => _items;

        public string SummaryText => _items.Count == 0
            ? "Henüz kontrol listesi yok"
            : $"Tamamlanan: {_items.Count(item => item.IsDone)}/{_items.Count}";

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_isLoaded)
            {
                return;
            }

            await LoadAsync();
            _isLoaded = true;
        }

        private async Task LoadAsync()
        {
            await _databaseService.InitializeAsync();
            await _authService.InitializeAsync();

            var items = await _databaseService.GetChecklistItemsAsync();
            _items.Clear();
            foreach (var item in items.OrderBy(entry => entry.OrderNo))
            {
                AddDisplayItem(ChecklistDisplayItem.FromModel(item));
            }

            OnPropertyChanged(nameof(SummaryText));
        }

        private void AddDisplayItem(ChecklistDisplayItem displayItem)
        {
            displayItem.PropertyChanged += OnItemPropertyChanged;
            _items.Add(displayItem);
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChecklistDisplayItem.IsDone))
            {
                OnPropertyChanged(nameof(SummaryText));
            }
        }

        private async void OnImportExcel(object sender, EventArgs e)
        {
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
                    PickerTitle = "Kontrol listesi Excel dosyasını seçin",
                    FileTypes = fileTypes
                });

                if (result == null)
                {
                    return;
                }

                await using var stream = await result.OpenReadAsync();
                var imported = await _excelService.ImportChecklistAsync(stream);

                _items.Clear();
                foreach (var item in imported.OrderBy(entry => entry.OrderNo))
                {
                    item.Status = ChecklistStatus.NotYet;
                    AddDisplayItem(ChecklistDisplayItem.FromModel(item));
                }

                OnPropertyChanged(nameof(SummaryText));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Excel içe aktarımı başarısız oldu: {ex.Message}", "Tamam");
            }
        }

        private void OnSelectAll(object sender, EventArgs e)
        {
            foreach (var item in _items)
            {
                item.IsDone = true;
            }
        }

        private void OnMarkCompleted(object sender, EventArgs e)
        {
            foreach (var item in _items.Where(entry => !entry.IsDone))
            {
                item.IsDone = true;
            }
        }

        private void OnItemChecked(object sender, CheckedChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SummaryText));
        }

        private async void OnPhoneTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is not string phone || string.IsNullOrWhiteSpace(phone))
            {
                return;
            }

            await Clipboard.Default.SetTextAsync(phone);
            await Toast.Make($"{phone} numarası panoya kopyalandı.", ToastDuration.Short).Show();
        }

        private async void OnSaveChecklist(object sender, EventArgs e)
        {
            if (_items.Count == 0)
            {
                await DisplayAlert("Bilgi", "Kaydedilecek kayıt bulunmuyor.", "Tamam");
                return;
            }

            var incomplete = _items.Count(item => !item.IsDone);
            if (incomplete > 0)
            {
                var confirm = await DisplayAlert("Uyarı", $"{incomplete} hasta henüz tamamlanmadı. Yine de kaydetmek ister misiniz?", "Kaydet", "İptal");
                if (!confirm)
                {
                    return;
                }
            }

            var existing = await _databaseService.GetChecklistItemsAsync();
            foreach (var item in existing)
            {
                await _databaseService.DeleteChecklistItemAsync(item);
            }

            foreach (var display in _items)
            {
                await _databaseService.SaveChecklistItemAsync(display.ToModel());
            }

            var createdBy = _authService.CurrentUser?.Username ?? "Depo";
            var history = new HistoryItem
            {
                Type = HistoryType.Checklist,
                Summary = $"Kontrol listesi tamamlandı - {_items.Count(item => item.IsDone)}/{_items.Count}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                Reversible = true,
                Details = _items.Select(item => new UsedMaterialRecord
                {
                    Name = item.Patient,
                    SerialOrLot = $"{item.Hospital} • Tel: {item.Phone}",
                    Quantity = item.IsDone ? 1 : 0
                }).ToList()
            };

            await _databaseService.SaveHistoryItemAsync(history);

            await Toast.Make("Kontrol listesi kaydedildi.", ToastDuration.Short).Show();
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ChecklistDisplayItem : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private int _orderNo;
        private string _patient = string.Empty;
        private string _hospital = string.Empty;
        private string _phone = string.Empty;
        private TimeSpan _time;
        private bool _isDone;

        public Guid Id
        {
            get => _id;
            set => _id = value;
        }

        public int OrderNo
        {
            get => _orderNo;
            set => SetProperty(ref _orderNo, value);
        }

        public string Patient
        {
            get => _patient;
            set => SetProperty(ref _patient, value);
        }

        public string Hospital
        {
            get => _hospital;
            set => SetProperty(ref _hospital, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public TimeSpan Time
        {
            get => _time;
            set
            {
                if (SetProperty(ref _time, value))
                {
                    OnPropertyChanged(nameof(TimeDisplay));
                }
            }
        }

        public bool IsDone
        {
            get => _isDone;
            set => SetProperty(ref _isDone, value);
        }

        public string OrderDisplay => $"#{OrderNo}";

        public string TimeDisplay => Time.ToString("HH\\:mm");

        public event PropertyChangedEventHandler? PropertyChanged;

        public static ChecklistDisplayItem FromModel(ChecklistItem item)
        {
            return new ChecklistDisplayItem
            {
                Id = item.Id,
                OrderNo = item.OrderNo,
                Patient = item.Patient,
                Hospital = item.Hospital,
                Phone = item.Phone,
                Time = item.Time,
                IsDone = item.Status == ChecklistStatus.Done
            };
        }

        public ChecklistItem ToModel()
        {
            return new ChecklistItem
            {
                Id = Id,
                OrderNo = OrderNo,
                Patient = Patient,
                Hospital = Hospital,
                Phone = Phone,
                Time = Time,
                Status = IsDone ? ChecklistStatus.Done : ChecklistStatus.NotYet
            };
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
