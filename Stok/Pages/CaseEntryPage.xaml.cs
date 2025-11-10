using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stok.Helpers;
using Stok.Models;
using Stok.Services;

namespace Stok.Pages
{
    public partial class CaseEntryPage : ContentPage, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly AuthService _authService;
        private readonly ObservableCollection<string> _hospitalSuggestions = new();
        private readonly ObservableCollection<string> _doctorSuggestions = new();
        private readonly ObservableCollection<CaseMaterialEntry> _materials = new();
        private readonly List<Material> _inventory = new();
        private List<LookupValue> _hospitalLookups = new();
        private List<LookupValue> _doctorLookups = new();
        private bool _showHospitalSuggestions;
        private bool _showDoctorSuggestions;
        private DateTime _caseDate = DateTime.Today;
        private string _hospital = string.Empty;
        private string _doctor = string.Empty;
        private string _patient = string.Empty;
        private string? _note;
        private string _createdByDisplay = string.Empty;
        private string? _validationMessage;
        private bool _isInitialized;

        public CaseEntryPage()
        {
            InitializeComponent();
            BindingContext = this;

            _databaseService = ServiceHelper.GetRequiredService<DatabaseService>();
            _authService = ServiceHelper.GetRequiredService<AuthService>();
        }

        public ObservableCollection<string> HospitalSuggestions => _hospitalSuggestions;

        public ObservableCollection<string> DoctorSuggestions => _doctorSuggestions;

        public ObservableCollection<CaseMaterialEntry> Materials => _materials;

        public bool ShowHospitalSuggestions
        {
            get => _showHospitalSuggestions;
            set => SetProperty(ref _showHospitalSuggestions, value);
        }

        public bool ShowDoctorSuggestions
        {
            get => _showDoctorSuggestions;
            set => SetProperty(ref _showDoctorSuggestions, value);
        }

        public DateTime CaseDate
        {
            get => _caseDate;
            set => SetProperty(ref _caseDate, value);
        }

        public string Hospital
        {
            get => _hospital;
            set => SetProperty(ref _hospital, value);
        }

        public string Doctor
        {
            get => _doctor;
            set => SetProperty(ref _doctor, value);
        }

        public string Patient
        {
            get => _patient;
            set => SetProperty(ref _patient, value);
        }

        public string? Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        public string CreatedByDisplay
        {
            get => _createdByDisplay;
            private set => SetProperty(ref _createdByDisplay, value);
        }

        public string? ValidationMessage
        {
            get => _validationMessage;
            private set
            {
                if (SetProperty(ref _validationMessage, value))
                {
                    OnPropertyChanged(nameof(HasValidationMessage));
                }
            }
        }

        public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_isInitialized)
            {
                return;
            }

            await LoadAsync();
            _isInitialized = true;
        }

        private async Task LoadAsync()
        {
            await _databaseService.InitializeAsync();
            await _authService.InitializeAsync();

            CreatedByDisplay = _authService.CurrentUser?.Username ?? "Misafir";

            var materials = await _databaseService.GetMaterialsAsync();
            _inventory.Clear();
            _inventory.AddRange(materials);

            _hospitalLookups = (await _databaseService.GetLookupValuesAsync(LookupType.Hospital)).ToList();
            _doctorLookups = (await _databaseService.GetLookupValuesAsync(LookupType.Doctor)).ToList();

            RefreshHospitalSuggestions();
            RefreshDoctorSuggestions();

            if (_materials.Count == 0)
            {
                _materials.Add(new CaseMaterialEntry());
            }
        }

        private void RefreshHospitalSuggestions(string? filter = null)
        {
            _hospitalSuggestions.Clear();
            var query = _hospitalLookups.Select(item => item.Value);
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(value => value.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var value in query.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                _hospitalSuggestions.Add(value);
            }

            ShowHospitalSuggestions = _hospitalSuggestions.Count > 0 && !string.IsNullOrWhiteSpace(filter);
        }

        private void RefreshDoctorSuggestions(string? filter = null)
        {
            _doctorSuggestions.Clear();
            var query = _doctorLookups.Select(item => item.Value);
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(value => value.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var value in query.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                _doctorSuggestions.Add(value);
            }

            ShowDoctorSuggestions = _doctorSuggestions.Count > 0 && !string.IsNullOrWhiteSpace(filter);
        }

        private void OnHospitalTextChanged(object sender, TextChangedEventArgs e)
        {
            Hospital = e.NewTextValue?.Trim() ?? string.Empty;
            RefreshHospitalSuggestions(Hospital);
        }

        private void OnDoctorTextChanged(object sender, TextChangedEventArgs e)
        {
            Doctor = e.NewTextValue?.Trim() ?? string.Empty;
            RefreshDoctorSuggestions(Doctor);
        }

        private void OnHospitalSuggestionTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is string value)
            {
                Hospital = value;
            }

            ShowHospitalSuggestions = false;
        }

        private async void OnRemoveHospitalSuggestion(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not string value)
            {
                return;
            }

            var lookup = _hospitalLookups.FirstOrDefault(item => item.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (lookup == null)
            {
                return;
            }

            _hospitalLookups.Remove(lookup);
            await _databaseService.DeleteLookupValueAsync(lookup);
            RefreshHospitalSuggestions(Hospital);
        }

        private void OnDoctorSuggestionTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is string value)
            {
                Doctor = value;
            }

            ShowDoctorSuggestions = false;
        }

        private async void OnRemoveDoctorSuggestion(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not string value)
            {
                return;
            }

            var lookup = _doctorLookups.FirstOrDefault(item => item.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (lookup == null)
            {
                return;
            }

            _doctorLookups.Remove(lookup);
            await _databaseService.DeleteLookupValueAsync(lookup);
            RefreshDoctorSuggestions(Doctor);
        }

        private void OnAddMaterial(object sender, EventArgs e)
        {
            _materials.Add(new CaseMaterialEntry());
        }

        private void OnRemoveMaterial(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not CaseMaterialEntry entry)
            {
                return;
            }

            _materials.Remove(entry);
        }

        private void OnMaterialSerialChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entryControl || entryControl.BindingContext is not CaseMaterialEntry materialEntry)
            {
                return;
            }

            materialEntry.SerialOrLot = e.NewTextValue?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(materialEntry.SerialOrLot))
            {
                materialEntry.ClearResolved();
                return;
            }

            var matches = _inventory.Where(item =>
                string.Equals(item.Serial ?? string.Empty, materialEntry.SerialOrLot, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Lot ?? string.Empty, materialEntry.SerialOrLot, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1)
            {
                materialEntry.ApplyFrom(matches[0]);
            }
            else if (matches.Count == 0)
            {
                materialEntry.ClearResolved();
            }
        }

        private void OnMaterialQuantityChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entryControl || entryControl.BindingContext is not CaseMaterialEntry materialEntry)
            {
                return;
            }

            if (int.TryParse(e.NewTextValue, out var quantity) && quantity >= 0)
            {
                materialEntry.Quantity = quantity;
            }
            else if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                materialEntry.Quantity = 0;
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            ValidationMessage = null;

            if (string.IsNullOrWhiteSpace(Hospital) || string.IsNullOrWhiteSpace(Doctor) || string.IsNullOrWhiteSpace(Patient))
            {
                ValidationMessage = "Hastane, doktor ve hasta alanları boş bırakılamaz.";
                return;
            }

            var usedMaterials = new List<(CaseMaterialEntry Entry, Material Stock)>();
            foreach (var entry in _materials)
            {
                if (string.IsNullOrWhiteSpace(entry.SerialOrLot))
                {
                    ValidationMessage = "Seri/Lot bilgisi boş olamaz.";
                    return;
                }

                var match = _inventory.FirstOrDefault(item =>
                    string.Equals(item.Serial ?? string.Empty, entry.SerialOrLot, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Lot ?? string.Empty, entry.SerialOrLot, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    ValidationMessage = $"{entry.SerialOrLot} numaralı malzeme stokta bulunamadı.";
                    return;
                }

                if (entry.Quantity <= 0)
                {
                    ValidationMessage = "Miktar 0'dan büyük olmalıdır.";
                    return;
                }

                if (entry.Quantity > match.Quantity)
                {
                    ValidationMessage = $"{match.Name} için yeterli stok yok. Kalan: {match.Quantity}.";
                    return;
                }

                entry.ApplyFrom(match);
                usedMaterials.Add((entry, match));
            }

            if (usedMaterials.Count == 0)
            {
                ValidationMessage = "En az bir malzeme eklenmelidir.";
                return;
            }

            var createdBy = _authService.CurrentUser?.Username ?? "Depo";
            var caseRecord = new CaseRecord
            {
                Hospital = Hospital,
                Doctor = Doctor,
                Patient = Patient,
                Note = Note,
                CreatedAt = CaseDate.Date.Add(DateTime.Now.TimeOfDay),
                CreatedBy = createdBy
            };

            caseRecord.UsedMaterials = usedMaterials.Select(item => new UsedMaterialRecord
            {
                MaterialId = item.Stock.Id,
                Name = item.Stock.Name,
                SerialOrLot = item.Entry.SerialOrLot,
                Serial = item.Stock.Serial,
                Lot = item.Stock.Lot,
                ExpiryDate = item.Stock.ExpiryDate,
                Quantity = item.Entry.Quantity
            }).ToList();

            await _databaseService.SaveCaseRecordAsync(caseRecord);

            foreach (var (entry, stock) in usedMaterials)
            {
                stock.Quantity -= entry.Quantity;
                await _databaseService.SaveMaterialAsync(stock);
            }

            await SaveLookupIfMissingAsync(LookupType.Hospital, Hospital, _hospitalLookups);
            await SaveLookupIfMissingAsync(LookupType.Doctor, Doctor, _doctorLookups);

            var history = new HistoryItem
            {
                Type = HistoryType.Case,
                Summary = $"Vaka: {Patient} - {Hospital}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                Reversible = true,
                ReferenceId = caseRecord.Id,
                Details = caseRecord.UsedMaterials
            };

            await _databaseService.SaveHistoryItemAsync(history);

            await DisplayAlert("Başarılı", "Vaka kaydı oluşturuldu ve stok güncellendi.", "Tamam");

            ResetForm();
            await ReloadInventoryAsync();
        }

        private async Task ReloadInventoryAsync()
        {
            var materials = await _databaseService.GetMaterialsAsync();
            _inventory.Clear();
            _inventory.AddRange(materials);
        }

        private void ResetForm()
        {
            CaseDate = DateTime.Today;
            Patient = string.Empty;
            Note = string.Empty;
            Materials.Clear();
            Materials.Add(new CaseMaterialEntry());
            ValidationMessage = null;
        }

        private async Task SaveLookupIfMissingAsync(LookupType type, string value, List<LookupValue> cache)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (cache.Any(item => item.Value.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var lookup = new LookupValue
            {
                Type = type,
                Value = value.Trim()
            };

            cache.Add(lookup);
            await _databaseService.SaveLookupValueAsync(lookup);

            if (type == LookupType.Hospital)
            {
                RefreshHospitalSuggestions(Hospital);
            }
            else
            {
                RefreshDoctorSuggestions(Doctor);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    }

    public class CaseMaterialEntry : INotifyPropertyChanged
    {
        private Guid? _materialId;
        private string _name = string.Empty;
        private string _serialOrLot = string.Empty;
        private DateTime? _expiryDate;
        private string? _ubb;
        private string? _serial;
        private string? _lot;
        private int _quantity = 1;
        private int _availableQuantity;

        public Guid? MaterialId
        {
            get => _materialId;
            set => SetProperty(ref _materialId, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string SerialOrLot
        {
            get => _serialOrLot;
            set => SetProperty(ref _serialOrLot, value);
        }

        public DateTime? ExpiryDate
        {
            get => _expiryDate;
            set
            {
                if (SetProperty(ref _expiryDate, value))
                {
                    OnPropertyChanged(nameof(ExpiryDisplay));
                }
            }
        }

        public string? Ubb
        {
            get => _ubb;
            set
            {
                if (SetProperty(ref _ubb, value))
                {
                    OnPropertyChanged(nameof(UbbDisplay));
                }
            }
        }

        public string? Serial
        {
            get => _serial;
            set => SetProperty(ref _serial, value);
        }

        public string? Lot
        {
            get => _lot;
            set => SetProperty(ref _lot, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public int AvailableQuantity
        {
            get => _availableQuantity;
            set
            {
                if (SetProperty(ref _availableQuantity, value))
                {
                    OnPropertyChanged(nameof(AvailabilityDisplay));
                }
            }
        }

        public string ExpiryDisplay => ExpiryDate.HasValue ? $"SKT: {ExpiryDate:dd.MM.yyyy}" : "SKT bilgisi bulunmuyor";

        public string UbbDisplay => string.IsNullOrWhiteSpace(Ubb) ? "UBB: -" : $"UBB: {Ubb}";

        public string AvailabilityDisplay => MaterialId.HasValue
            ? $"Stokta {AvailableQuantity} adet mevcut"
            : "Stok eşleşmesi bekleniyor";

        public event PropertyChangedEventHandler? PropertyChanged;

        public void ApplyFrom(Material material)
        {
            MaterialId = material.Id;
            Name = material.Name;
            ExpiryDate = material.ExpiryDate;
            Ubb = material.Ubb;
            Serial = material.Serial;
            Lot = material.Lot;
            AvailableQuantity = material.Quantity;
            if (string.IsNullOrWhiteSpace(SerialOrLot))
            {
                SerialOrLot = material.Serial ?? material.Lot ?? string.Empty;
            }
        }

        public void ClearResolved()
        {
            MaterialId = null;
            Name = string.Empty;
            ExpiryDate = null;
            Ubb = null;
            Serial = null;
            Lot = null;
            AvailableQuantity = 0;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    }
}
