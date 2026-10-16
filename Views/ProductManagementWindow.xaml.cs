using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CargoPlanApp.Models;

namespace CargoPlanApp.Views
{
    public partial class ProductManagementWindow : Wpf.Ui.Controls.FluentWindow
    {
        public ObservableCollection<Product> Products { get; set; }

        private const double MinDensity = 0.6100;
        private const double MaxDensity = 1.2000;

        public ProductManagementWindow(ObservableCollection<Product> original)
        {
            InitializeComponent();
            Products = new ObservableCollection<Product>(
                original.Select(p => new Product { Name = p.Name, Density15 = p.Density15, Color = p.Color }));
            ProductsGrid.ItemsSource = Products;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Products.Add(new Product { Name = "New Product", Density15 = 0.8, Color = Colors.Gray });
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is Product selected)
                Products.Remove(selected);
        }

        private void DensityTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (double.TryParse(textBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    if (val < MinDensity || val > MaxDensity)
                    {
                        MessageBox.Show($"Density has to be from {MinDensity:F4} to {MaxDensity:F4}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        textBox.Text = "";
                        textBox.Focus();
                    }
                }
                else if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    MessageBox.Show("Please enter Density", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Text = "";
                    textBox.Focus();
                }
            }
        }

        private void ColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Product product)
            {
                // Открываем диалог выбора цвета
                var dialog = new ColorPickerDialog
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Color = product.Color
                };

                if (dialog.ShowDialog() == true)
                {
                    product.Color = dialog.Color;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (var product in Products)
            {
                if (product.Density15 < MinDensity || product.Density15 > MaxDensity)
                {
                    MessageBox.Show($"Density of '{product.Name}' out of range ({MinDensity:F4} - {MaxDensity:F4}). Correct the value.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var duplicateName = Products
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .GroupBy(p => p.Name)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateName != null)
            {
                MessageBox.Show($"Product name '{duplicateName.Key}' is used more than once. Names must be unique.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mainVm = ((MainWindow)Owner).DataContext as ViewModels.MainViewModel;
            if (mainVm == null) return;

            var originalDict = mainVm.Products
                .GroupBy(p => p.Name)
                .ToDictionary(g => g.Key, g => g.First());
            var editedDict = Products.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToDictionary(p => p.Name);

            for (int i = mainVm.Products.Count - 1; i >= 0; i--)
            {
                if (!editedDict.ContainsKey(mainVm.Products[i].Name))
                    mainVm.Products.RemoveAt(i);
            }

            foreach (var kvp in editedDict)
            {
                if (originalDict.TryGetValue(kvp.Key, out var existing))
                {
                    existing.Density15 = kvp.Value.Density15;
                    existing.Color = kvp.Value.Color;
                }
                else
                {
                    mainVm.Products.Add(new Product { Name = kvp.Key, Density15 = kvp.Value.Density15, Color = kvp.Value.Color });
                }
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}