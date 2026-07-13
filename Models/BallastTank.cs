using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CargoPlanApp.Models
{
    public class BallastTank : INotifyPropertyChanged
    {
        private string _tankNo;
        public string TankNo
        {
            get => _tankNo;
            set { _tankNo = value; OnPropertyChanged(); }
        }

        private double? _waterVolume;
        public double? WaterVolume
        {
            get => _waterVolume;
            set { _waterVolume = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}