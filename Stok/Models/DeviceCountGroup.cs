using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Stok.Models
{
    public class DeviceCategoryGroup : INotifyPropertyChanged
    {
        private bool _isExpanded;

        public DeviceCategoryGroup(string title)
        {
            Title = title;
        }

        public string Title { get; }

        public ObservableCollection<DeviceModelGroup> Models { get; } = new();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ToggleGlyph));
                    OnPropertyChanged(nameof(VisibleModels));
                }
            }
        }

        public string ToggleGlyph => IsExpanded ? "−" : "+";

        public IEnumerable<DeviceModelGroup> VisibleModels => IsExpanded ? Models : Array.Empty<DeviceModelGroup>();

        public int TotalQuantity => Models.Sum(m => m.TotalQuantity);

        public void ReplaceModels(IEnumerable<DeviceModelGroup> models)
        {
            var desired = models.ToList();
            for (var i = Models.Count - 1; i >= 0; i--)
            {
                if (!desired.Contains(Models[i]))
                {
                    Models.RemoveAt(i);
                }
            }

            foreach (var model in desired)
            {
                if (!Models.Contains(model))
                {
                    Models.Add(model);
                }
            }

            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(VisibleModels));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DeviceModelGroup : INotifyPropertyChanged
    {
        private bool _isExpanded;

        public DeviceModelGroup(string modelName)
        {
            ModelName = modelName;
        }

        public string ModelName { get; }

        public ObservableCollection<Material> Items { get; } = new();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ToggleGlyph));
                    OnPropertyChanged(nameof(VisibleItems));
                }
            }
        }

        public string ToggleGlyph => IsExpanded ? "−" : "+";

        public IEnumerable<Material> VisibleItems => IsExpanded ? Items : Array.Empty<Material>();

        public int TotalQuantity => Items.Sum(m => m.Quantity);

        public void ReplaceItems(IEnumerable<Material> materials)
        {
            var desired = materials.ToList();
            for (var i = Items.Count - 1; i >= 0; i--)
            {
                if (!desired.Contains(Items[i]))
                {
                    Items.RemoveAt(i);
                }
            }

            foreach (var material in desired)
            {
                if (!Items.Contains(material))
                {
                    Items.Add(material);
                }
            }

            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(VisibleItems));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
