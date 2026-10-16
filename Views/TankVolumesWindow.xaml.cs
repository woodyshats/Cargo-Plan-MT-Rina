using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace CargoPlanApp.Views
{
    public partial class TankVolumesWindow : Wpf.Ui.Controls.FluentWindow
    {
        public ObservableCollection<TankVolumeItem> Items { get; set; }
        private Dictionary<string, double> _volumes;

        public TankVolumesWindow(Dictionary<string, double> volumes)
        {
            InitializeComponent();
            _volumes = volumes;
            Items = new ObservableCollection<TankVolumeItem>();
            string[] tankNames = { "COT 1P", "COT 1S", "COT 2P", "COT 2S", "COT 3P", "COT 3S", "COT 4P", "COT 4S", "COT 5P", "COT 5S", "COT 6P", "COT 6S", "COT 7P", "COT 7S", "SLOP P", "SLOP S" };
            foreach (var n in tankNames)
                Items.Add(new TankVolumeItem { TankNo = n, MaxVolume = volumes.ContainsKey(n) ? volumes[n] : (double?)null });
            VolumesGrid.ItemsSource = Items;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _volumes.Clear();
            foreach (var item in Items)
                if (item.MaxVolume.HasValue)
                    _volumes[item.TankNo] = item.MaxVolume.Value;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class TankVolumeItem
    {
        public string TankNo { get; set; }
        public double? MaxVolume { get; set; }
    }
}