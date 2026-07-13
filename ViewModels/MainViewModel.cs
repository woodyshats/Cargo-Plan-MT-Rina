using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CargoPlanApp.Models;
using CargoPlanApp.Services;
using CargoPlanApp.Views;

namespace CargoPlanApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly JsonDataService _settingsService = new JsonDataService("settings.json");
        private readonly JsonDataService _dataService = new JsonDataService("data.json"); // временно для совместимости

        public ObservableCollection<Tank> CargoTanks { get; set; } = new ObservableCollection<Tank>();
        public ObservableCollection<BallastTank> BallastTanks { get; set; } = new ObservableCollection<BallastTank>();
        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
        public ObservableCollection<GradeSummary> GradeSummaries { get; set; } = new ObservableCollection<GradeSummary>();

        public ObservableCollection<string> AllProductNames { get; } = new ObservableCollection<string>();

        private Dictionary<string, double> _tankMaxVolumes = new Dictionary<string, double>();
        public Dictionary<string, double> TankMaxVolumes
        {
            get => _tankMaxVolumes;
            set { _tankMaxVolumes = value; OnPropertyChanged(); UpdateMaxVolumes(); }
        }

        private string _voyage;
        public string Voyage { get => _voyage; set { _voyage = value; OnPropertyChanged(); MarkDirty(); } }
        private string _portOfLoading;
        public string PortOfLoading { get => _portOfLoading; set { _portOfLoading = value; OnPropertyChanged(); MarkDirty(); } }
        private string _portOfDischarge;
        public string PortOfDischarge { get => _portOfDischarge; set { _portOfDischarge = value; OnPropertyChanged(); MarkDirty(); } }
        private string _terminal;
        public string Terminal { get => _terminal; set { _terminal = value; OnPropertyChanged(); MarkDirty(); } }
        private string _planType;
        public string PlanType { get => _planType; set { _planType = value; OnPropertyChanged(); MarkDirty(); } }
        private DateTime _date = DateTime.Now;
        public DateTime Date { get => _date; set { _date = value; OnPropertyChanged(); MarkDirty(); } }
        private double? _draft;
        public double? Draft { get => _draft; set { _draft = value; OnPropertyChanged(); MarkDirty(); } }
        private double? _waterDensity;
        public double? WaterDensity { get => _waterDensity; set { _waterDensity = value; OnPropertyChanged(); MarkDirty(); } }

        private string _currentFilePath;
        private bool _isDirty;

        public bool HasUnsavedChanges => _isDirty;

        public ICommand ManageProductsCommand { get; }
        public ICommand SetMaxVolumesCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveAsCommand { get; }

        public event Action OnPrintRequested;
        public event Action OnCloseRequested;

        public MainViewModel()
        {
            ManageProductsCommand = new RelayCommand(OpenProductManagement);
            SetMaxVolumesCommand = new RelayCommand(OpenMaxVolumesWindow);
            SaveCommand = new RelayCommand(Save);
            PrintCommand = new RelayCommand(() => OnPrintRequested?.Invoke());
            NewCommand = new RelayCommand(NewPlan);
            OpenCommand = new RelayCommand(OpenPlan);
            SaveAsCommand = new RelayCommand(SaveAs);

            LoadSettings();
            InitializeTanks();

            Products.CollectionChanged += (s, e) => { SaveSettings(); UpdateProductNames(); };
            TankMaxVolumes = _tankMaxVolumes; // сохраним ссылку после загрузки

            foreach (var tank in CargoTanks)
                tank.PropertyChanged += OnTankPropertyChanged;
            CargoTanks.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (Tank t in e.NewItems) t.PropertyChanged += OnTankPropertyChanged;
                if (e.OldItems != null)
                    foreach (Tank t in e.OldItems) t.PropertyChanged -= OnTankPropertyChanged;
            };
        }

        private void LoadSettings()
        {
            var settings = _settingsService.Load();
            if (settings != null)
            {
                Products = new ObservableCollection<Product>(settings.Products ?? new List<Product>());
                _tankMaxVolumes = settings.TankMaxVolumes ?? new Dictionary<string, double>();
            }
            else
            {
                Products = new ObservableCollection<Product>();
                _tankMaxVolumes = new Dictionary<string, double>();
            }
            UpdateProductNames();
            UpdateMaxVolumes();
        }

        public void SaveSettings()
        {
            var settings = new CargoPlanData
            {
                Products = Products.ToList(),
                TankMaxVolumes = new Dictionary<string, double>(_tankMaxVolumes)
            };
            _settingsService.Save(settings);
        }

        private void InitializeTanks()
        {
            CargoTanks.Clear();
            string[] cargoNames = { "COT 1P", "COT 1S", "COT 2P", "COT 2S", "COT 3P", "COT 3S", "COT 4P", "COT 4S", "COT 5P", "COT 5S", "COT 6P", "COT 6S", "COT 7P", "COT 7S", "SLOP P", "SLOP S" };
            foreach (var n in cargoNames) CargoTanks.Add(new Tank { TankNo = n });

            BallastTanks.Clear();
            string[] ballastNames = { "FPK",
                                      "WBT 1 P","WBT 1 S",
                                      "WBT 2 P","WBT 2 S",
                                      "WBT 3 P","WBT 3 S",
                                      "WBT 4 P","WBT 4 S",
                                      "WWT P","WWT S",
                                      "WBT 5 P","WBT 5 S" };
            foreach (var n in ballastNames) BallastTanks.Add(new BallastTank { TankNo = n });
        }

        private void OnTankPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tank.SelectedProductName))
            {
                var tank = (Tank)sender;
                if (tank.SelectedProductName == "Empty")
                {
                    tank.Density15 = null;
                    tank.ProductColor = null;
                }
                else
                {
                    var product = Products.FirstOrDefault(p => p.Name == tank.SelectedProductName);
                    tank.Density15 = product?.Density15 ?? null;
                    tank.ProductColor = product?.Color;
                }
            }
            MarkDirty();
            UpdateSummaries();
        }

        private void MarkDirty() => _isDirty = true;
        public void ClearDirty() => _isDirty = false;

        private void UpdateProductNames()
        {
            AllProductNames.Clear();
            AllProductNames.Add("Empty");
            foreach (var p in Products)
                AllProductNames.Add(p.Name);
        }

        public void UpdateSummaries()
        {
            var summaries = new List<GradeSummary>();
            var active = CargoTanks.Where(t => !string.IsNullOrEmpty(t.SelectedProductName) && t.Weight.HasValue && t.Weight > 0 && t.SelectedProductName != "Empty").ToList();
            var groups = active.GroupBy(t => t.SelectedProductName);
            foreach (var g in groups)
            {
                var prod = Products.FirstOrDefault(p => p.Name == g.Key);
                double tw = g.Sum(t => t.Weight ?? 0);
                double tv = g.Sum(t => t.Volume ?? 0);
                double avgT = tv > 0 ? g.Sum(t => (t.Temperature ?? 15) * (t.Volume ?? 0)) / tv
                                     : g.Average(t => t.Temperature ?? 15);
                summaries.Add(new GradeSummary
                {
                    Grade = g.Key,
                    Density15 = prod?.Density15 ?? 0,
                    TotalWeight = tw,
                    TotalVolume = tv,
                    AvgTemperature = avgT
                });
            }
            var totalWeightAll = summaries.Sum(s => s.TotalWeight);
            var totalVolumeAll = summaries.Sum(s => s.TotalVolume);
            double avgTempAll = 0;
            if (totalVolumeAll > 0)
                avgTempAll = active.Sum(t => (t.Temperature ?? 15) * (t.Volume ?? 0)) / totalVolumeAll;
            summaries.Add(new GradeSummary
            {
                Grade = "TOTAL",
                Density15 = double.NaN,
                TotalWeight = totalWeightAll,
                TotalVolume = totalVolumeAll,
                AvgTemperature = avgTempAll
            });
            GradeSummaries = new ObservableCollection<GradeSummary>(summaries);
            OnPropertyChanged(nameof(GradeSummaries));
        }

        private void UpdateMaxVolumes()
        {
            foreach (var tank in CargoTanks)
            {
                tank.MaxVolume = _tankMaxVolumes.TryGetValue(tank.TankNo, out double vol) ? vol : 0;
                tank.RefreshCalculated();
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAs();
                return;
            }
            var data = BuildDataObject();
            new JsonDataService(_currentFilePath).Save(data);
            ClearDirty();
        }

        public void SaveAs()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            dlg.DefaultExt = ".json";
            dlg.FileName = "CargoPlan.json";
            if (dlg.ShowDialog() == true)
            {
                _currentFilePath = dlg.FileName;
                Save();
            }
        }

        public void NewPlan()
        {
            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show("Do you want to save current cargo plan?", "New Cargo Plan", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                    Save();
                else if (result == MessageBoxResult.Cancel)
                    return;
            }
            // Очищаем данные танков
            foreach (var tank in CargoTanks)
            {
                tank.SelectedProductName = "Empty";
                tank.Weight = null;
                tank.Temperature = null;
            }
            foreach (var bt in BallastTanks)
                bt.WaterVolume = null;

            Voyage = PortOfLoading = PortOfDischarge = Terminal = PlanType = null;
            Draft = WaterDensity = null;
            Date = DateTime.Now;
            _currentFilePath = null;
            ClearDirty();
            UpdateSummaries();
        }

        public void OpenPlan()
        {
            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show("Do you want to save current cargo plan?", "Open Cargo Plan", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                    Save();
                else if (result == MessageBoxResult.Cancel)
                    return;
            }

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var data = new JsonDataService(dlg.FileName).Load();
                    if (data == null) return;
                    ApplyData(data);
                    _currentFilePath = dlg.FileName;
                    ClearDirty();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ApplyData(CargoPlanData data)
        {
            // Продукты и объёмы НЕ трогаем, используем текущие
            // Применяем только состояния танков и информацию о рейсе
            foreach (var s in data.TankStates)
            {
                var tank = CargoTanks.FirstOrDefault(t => t.TankNo == s.TankNo);
                if (tank != null)
                {
                    tank.SelectedProductName = s.Grade;
                    tank.Weight = s.Weight;
                    tank.Temperature = s.Temperature;
                }
            }
            foreach (var s in data.BallastStates)
            {
                var bt = BallastTanks.FirstOrDefault(b => b.TankNo == s.TankNo);
                if (bt != null) bt.WaterVolume = s.WaterVolume;
            }
            if (data.VesselInfo != null)
            {
                Voyage = data.VesselInfo.Voyage;
                PortOfLoading = data.VesselInfo.PortOfLoading;
                PortOfDischarge = data.VesselInfo.PortOfDischarge;
                Terminal = data.VesselInfo.Terminal;
                PlanType = data.VesselInfo.PlanType;
                Draft = data.VesselInfo.Draft;
                WaterDensity = data.VesselInfo.WaterDensity;
                if (DateTime.TryParse(data.VesselInfo.Date, out DateTime dt)) Date = dt;
            }
            UpdateSummaries();
        }

        private CargoPlanData BuildDataObject()
        {
            return new CargoPlanData
            {
                Products = Products.ToList(),
                TankMaxVolumes = new Dictionary<string, double>(_tankMaxVolumes),
                TankStates = CargoTanks.Select(t => new TankState { TankNo = t.TankNo, Grade = t.SelectedProductName, Weight = t.Weight, Temperature = t.Temperature }).ToList(),
                BallastStates = BallastTanks.Select(b => new BallastTankState { TankNo = b.TankNo, WaterVolume = b.WaterVolume }).ToList(),
                VesselInfo = new VesselInfo
                {
                    Date = Date.ToString("yyyy-MM-dd"),
                    Voyage = Voyage,
                    PortOfLoading = PortOfLoading,
                    PortOfDischarge = PortOfDischarge,
                    Terminal = Terminal,
                    PlanType = PlanType,
                    Draft = Draft,
                    WaterDensity = WaterDensity
                }
            };
        }

        private void OpenProductManagement()
        {
            var win = new ProductManagementWindow(Products);
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
            SaveSettings();
            foreach (var tank in CargoTanks)
            {
                if (!string.IsNullOrEmpty(tank.SelectedProductName) && tank.SelectedProductName != "Empty")
                {
                    var prod = Products.FirstOrDefault(p => p.Name == tank.SelectedProductName);
                    if (prod == null) tank.SelectedProductName = "Empty";
                    else { tank.Density15 = prod.Density15; tank.ProductColor = prod.Color; }
                }
            }
            UpdateSummaries();
        }

        private void OpenMaxVolumesWindow()
        {
            var win = new TankVolumesWindow(_tankMaxVolumes);
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
            SaveSettings();
            UpdateMaxVolumes();
            UpdateSummaries();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute();
    }
}