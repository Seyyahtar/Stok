using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Stok.Helpers;
using Stok.Models;
using Stok.Services;

namespace Stok.Pages
{
    public partial class DeviceCountsPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private readonly Dictionary<string, string[]> _categoryKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Pacemaker"] = new[] { "amvia sky", "endicos", "enitra", "edora" },
            ["ICD"] = new[] { "vr-t", "dr-t", "rivacor" },
            ["CRT"] = new[] { "hf-t" }
        };

        public DeviceCountsPage()
        {
            InitializeComponent();
            BindingContext = this;

            _databaseService = ServiceHelper.GetRequiredService<DatabaseService>();
            Categories = new ObservableCollection<DeviceCategoryGroup>();
        }

        public ObservableCollection<DeviceCategoryGroup> Categories { get; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            await _databaseService.InitializeAsync();
            var materials = await _databaseService.GetMaterialsAsync();
            BuildCategories(materials);
        }

        private void BuildCategories(IList<Material> materials)
        {
            var categoryStates = Categories.ToDictionary(c => c.Title, c => c.IsExpanded, StringComparer.OrdinalIgnoreCase);
            var modelStates = Categories
                .SelectMany(c => c.Models.Select(m => (Category: c.Title, Model: m.ModelName, m.IsExpanded)))
                .ToDictionary(x => (x.Category, x.Model), x => x.IsExpanded, new CategoryModelComparer());

            foreach (var category in _categoryKeywords.Keys)
            {
                if (!Categories.Any(c => c.Title.Equals(category, StringComparison.OrdinalIgnoreCase)))
                {
                    var categoryGroup = new DeviceCategoryGroup(category) { IsExpanded = true };
                    Categories.Add(categoryGroup);
                }
            }

            foreach (var categoryGroup in Categories)
            {
                if (!categoryStates.TryGetValue(categoryGroup.Title, out var expanded))
                {
                    expanded = true;
                }

                categoryGroup.IsExpanded = expanded;

                if (!_categoryKeywords.TryGetValue(categoryGroup.Title, out var keywords))
                {
                    keywords = Array.Empty<string>();
                }

                var matches = materials
                    .Where(m => keywords.Any(k => m.Name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var grouped = matches
                    .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var desiredModels = new List<DeviceModelGroup>();
                foreach (var group in grouped)
                {
                    var model = categoryGroup.Models.FirstOrDefault(m => m.ModelName.Equals(group.Key, StringComparison.OrdinalIgnoreCase));
                    if (model == null)
                    {
                        model = new DeviceModelGroup(group.Key);
                        if (modelStates.TryGetValue((categoryGroup.Title, group.Key), out var isExpanded))
                        {
                            model.IsExpanded = isExpanded;
                        }
                        else
                        {
                            model.IsExpanded = true;
                        }
                    }

                    model.ReplaceItems(group.ToList());
                    desiredModels.Add(model);
                }

                categoryGroup.ReplaceModels(desiredModels);
            }
        }

        private void OnToggleCategory(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not DeviceCategoryGroup category)
            {
                return;
            }

            category.IsExpanded = !category.IsExpanded;
        }

        private void OnToggleModel(object sender, EventArgs e)
        {
            if (sender is not Button button || button.CommandParameter is not DeviceModelGroup model)
            {
                return;
            }

            model.IsExpanded = !model.IsExpanded;
        }

        private class CategoryModelComparer : IEqualityComparer<(string Category, string Model)>
        {
            public bool Equals((string Category, string Model) x, (string Category, string Model) y)
            {
                return string.Equals(x.Category, y.Category, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(x.Model, y.Model, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode((string Category, string Model) obj)
            {
                return HashCode.Combine(obj.Category.ToLowerInvariant(), obj.Model.ToLowerInvariant());
            }
        }
    }
}
