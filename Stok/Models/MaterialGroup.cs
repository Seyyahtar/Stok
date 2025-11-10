using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Stok.Models
{
    public class MaterialGroup : INotifyPropertyChanged
    {
        private bool _isExpanded = true;

        public MaterialGroup(string key)
        {
            Key = key;
        }

        public string Key { get; }

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

        public IEnumerable<Material> VisibleItems => IsExpanded ? Items : Array.Empty<Material>();

        public string ToggleGlyph => IsExpanded ? "−" : "+";

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

    public class FilterOption : INotifyPropertyChanged
    {
        private bool _isActive;

        public FilterOption(string title, params string[] keywords)
        {
            Title = title;
            Keywords = keywords;
        }

        public string Title { get; }

        public string[] Keywords { get; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
