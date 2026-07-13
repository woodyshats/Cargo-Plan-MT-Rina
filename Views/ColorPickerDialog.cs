using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace CargoPlanApp.Views
{
    public class ColorPickerDialog : Window
    {
        public Color Color { get; set; }
        private ColorPicker _colorPicker;

        public ColorPickerDialog()
        {
            Title = "Выбор цвета";
            Width = 350;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;
            Background = System.Windows.Media.Brushes.White;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

            _colorPicker = new ColorPicker
            {
                Margin = new Thickness(10)
            };
            grid.Children.Add(_colorPicker);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(5),
                Style = Application.Current.Resources["MaterialDesignRaisedButton"] as Style
            };
            okButton.Click += (s, e) =>
            {
                Color = _colorPicker.Color;
                DialogResult = true;
                Close();
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 80,
                Margin = new Thickness(5),
                Style = Application.Current.Resources["MaterialDesignFlatButton"] as Style
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}