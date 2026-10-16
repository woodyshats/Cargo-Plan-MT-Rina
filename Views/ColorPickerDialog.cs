using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace CargoPlanApp.Views
{
    public partial class ColorPickerDialog : FluentWindow
    {
        private Color _color = Colors.White;
        private Border? _selectedSwatch;

        /// <summary>Палитра готовых цветов для быстрого выбора.</summary>
        private static readonly Color[] Palette =
        {
            // нейтральные
            Colors.Black, Color.FromRgb(0x40,0x40,0x40), Color.FromRgb(0x80,0x80,0x80),
            Color.FromRgb(0xC0,0xC0,0xC0), Colors.White, Color.FromRgb(0x7F,0x7F,0x7F),
            Color.FromRgb(0x59,0x59,0x59), Color.FromRgb(0xA6,0xA6,0xA6),
            // тёплые
            Colors.Red, Color.FromRgb(0xC0,0x00,0x00), Color.FromRgb(0xFF,0x66,0x00),
            Color.FromRgb(0xFF,0x99,0x00), Color.FromRgb(0xFF,0xCC,0x00), Colors.Yellow,
            Color.FromRgb(0x99,0xCC,0x00), Color.FromRgb(0x00,0xCC,0x00),
            // зелёные / синие
            Colors.Green, Color.FromRgb(0x00,0xB0,0x50), Color.FromRgb(0x00,0x99,0x99),
            Color.FromRgb(0x00,0xB0,0xF0), Color.FromRgb(0x00,0x70,0xC0), Colors.Blue,
            Color.FromRgb(0x33,0x33,0xCC), Color.FromRgb(0x70,0x30,0xA0),
            // фиолетовые / розовые
            Color.FromRgb(0x99,0x33,0xFF), Color.FromRgb(0xCC,0x00,0xFF), Colors.Magenta,
            Color.FromRgb(0xFF,0x33,0x99), Color.FromRgb(0xFF,0x66,0x99), Color.FromRgb(0xA6,0x4D,0x79),
            Color.FromRgb(0x80,0x00,0x00), Color.FromRgb(0x8C,0x4A,0x00),
            // пастельные / прочие
            Colors.Cyan, Color.FromRgb(0x66,0xCC,0xFF), Color.FromRgb(0x99,0xFF,0xFF),
            Color.FromRgb(0xCC,0xFF,0xCC), Color.FromRgb(0xFF,0xFF,0x99), Color.FromRgb(0xFF,0xCC,0x99),
            Color.FromRgb(0xD9,0xD9,0xD9), Color.FromRgb(0xE6,0xB0,0xAA),
        };

        /// <summary>
        /// Выбранный цвет. Читается вызывающим кодом после ShowDialog()==true.
        /// Установка значения (в т.ч. через инициализатор объекта) сразу обновляет поля и предпросмотр.
        /// </summary>
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                ApplyToUi();
            }
        }

        public ColorPickerDialog()
        {
            InitializeComponent();

            PaletteList.ItemsSource = BuildPaletteBrushes();

            // Подписка выполняется здесь, а не в XAML: иначе установка Value
            // во время InitializeComponent вызывает обработчик до создания GBox/BBox.
            RBox.ValueChanged += Rgb_ValueChanged;
            GBox.ValueChanged += Rgb_ValueChanged;
            BBox.ValueChanged += Rgb_ValueChanged;

            // Контейнеры палитры генерируются при первой раскладке — подсветку
            // выбранного образца обновляем после загрузки окна.
            Loaded += (s, e) => RefreshSelection();

            ApplyToUi();
        }

        private static List<SolidColorBrush> BuildPaletteBrushes()
        {
            var brushes = new List<SolidColorBrush>(Palette.Length);
            foreach (var c in Palette)
            {
                var brush = new SolidColorBrush(c);
                brush.Freeze();
                brushes.Add(brush);
            }
            return brushes;
        }

        private void ApplyToUi()
        {
            if (RBox == null) return;
            RBox.Value = _color.R;
            GBox.Value = _color.G;
            BBox.Value = _color.B;
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (RBox == null || GBox == null || BBox == null || Preview == null) return;
            byte r = ToByte(RBox.Value);
            byte g = ToByte(GBox.Value);
            byte b = ToByte(BBox.Value);
            _color = Color.FromRgb(r, g, b);

            Preview.Background = new SolidColorBrush(_color);
            if (HexBox != null)
                HexBox.Text = $"#{r:X2}{g:X2}{b:X2}";
            RefreshSelection();
        }

        private void Swatch_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Background is SolidColorBrush brush)
                Color = brush.Color;
        }

        private void RefreshSelection()
        {
            if (_selectedSwatch != null)
            {
                _selectedSwatch.BorderBrush = Brushes.Transparent;
                _selectedSwatch = null;
            }
            if (PaletteList == null) return;

            foreach (var border in EnumerateSwatches(PaletteList))
            {
                if (border.Background is SolidColorBrush brush && brush.Color == _color)
                {
                    border.SetResourceReference(Border.BorderBrushProperty, "App.SelectionBackground");
                    _selectedSwatch = border;
                    break;
                }
            }
        }

        private static IEnumerable<Border> EnumerateSwatches(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is Border border)
                    yield return border;
                foreach (var nested in EnumerateSwatches(child))
                    yield return nested;
            }
        }

        private static byte ToByte(double? value)
        {
            if (value == null || double.IsNaN(value.Value)) return 0;
            if (value.Value < 0) return 0;
            if (value.Value > 255) return 255;
            return (byte)System.Math.Round(value.Value);
        }

        private void Rgb_ValueChanged(object sender, NumberBoxValueChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
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
