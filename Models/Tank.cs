using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace CargoPlanApp.Models
{
    public class Tank : INotifyPropertyChanged
    {
        private string _tankNo;
        public string TankNo
        {
            get => _tankNo;
            set { _tankNo = value; OnPropertyChanged(); }
        }

        private string _selectedProductName;
        public string SelectedProductName
        {
            get => _selectedProductName;
            set
            {
                _selectedProductName = value;
                OnPropertyChanged();
                if (value == "Empty")
                {
                    Density15 = null;
                    ProductColor = null;
                }
            }
        }

        private double? _density15;
        public double? Density15
        {
            get => _density15;
            set { _density15 = value; OnPropertyChanged(); RefreshCalculated(); }
        }

        private double? _weight;
        public double? Weight
        {
            get => _weight;
            set { _weight = value; OnPropertyChanged(); RefreshCalculated(); }
        }

        private double? _temperature;
        public double? Temperature
        {
            get => _temperature;
            set { _temperature = value; OnPropertyChanged(); RefreshCalculated(); }
        }

        public double MaxVolume { get; set; }

        private Color? _productColor;
        public Color? ProductColor
        {
            get => _productColor;
            set { _productColor = value; OnPropertyChanged(); }
        }

        public bool IsEmpty => string.IsNullOrEmpty(SelectedProductName) || SelectedProductName == "Empty" || !Weight.HasValue || Weight.Value == 0;

        public double? Volume
        {
            get
            {
                if (IsEmpty) return null;
                if (Weight.HasValue && Density15.HasValue && Density15.Value != 0)
                {
                    double vcf = CalculateVCF(Temperature ?? 15, Density15.Value);
                    return Weight.Value / (Density15.Value * vcf);
                }
                return null;
            }
        }

        public double? FillPercent
        {
            get
            {
                if (IsEmpty) return 0;
                if (Volume.HasValue && MaxVolume > 0)
                    return (Volume.Value / MaxVolume) * 100.0;
                return null;
            }
        }

        public double? DensityAtTemp
        {
            get
            {
                if (IsEmpty) return null;
                if (Density15.HasValue)
                {
                    double vcf = CalculateVCF(Temperature ?? 15, Density15.Value);
                    return Density15.Value * vcf;
                }
                return null;
            }
        }

        public static double CalculateVCF(double tempC, double density15)
        {
            double deltaT = tempC - 15.0;
            double alpha = 0.0008 + 0.0000012 * density15;
            return Math.Exp(-alpha * deltaT * (1 + 0.8 * alpha * deltaT));
        }

        public void RefreshCalculated()
        {
            OnPropertyChanged(nameof(Volume));
            OnPropertyChanged(nameof(FillPercent));
            OnPropertyChanged(nameof(DensityAtTemp));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}