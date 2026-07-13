using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace CargoPlanApp.Models
{
    public class Product : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private double _density15;
        public double Density15
        {
            get => _density15;
            set { _density15 = value; OnPropertyChanged(); }
        }

        private Color _color;
        public Color Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColorBrush)); }
        }

        public Brush ColorBrush => new SolidColorBrush(Color);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}