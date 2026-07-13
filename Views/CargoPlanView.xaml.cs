using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CargoPlanApp.Models;
using CargoPlanApp.ViewModels;

namespace CargoPlanApp.Views
{
    public partial class CargoPlanView : UserControl
    {
        private MainViewModel _vm;

        private const double TotalLength = 8.5;
        private const double FpkStart = 8.0;
        private const double FpkEnd = 8.5;

        private static readonly (double start, double end, string name)[] CargoDefs =
        {
            (0.0, 1.0, "SLOP "),
            (1.0, 2.0, "COT 7"),
            (2.0, 3.0, "COT 6"),
            (3.0, 4.0, "COT 5"),
            (4.0, 5.0, "COT 4"),
            (5.0, 6.0, "COT 3"),
            (6.0, 7.0, "COT 2"),
            (7.0, 8.0, "COT 1")
        };

        private static readonly (double start, double end, string name)[] BallastDefs =
        {
            (1.0, 2.333, "WBT 5"),
            (2.333, 2.667, "WWT"),
            (2.667, 4.0, "WBT 4"),
            (4.0, 5.0, "WBT 3"),
            (5.0, 6.667, "WBT 2"),
            (6.667, 8.0, "WBT 1")
        };

        public CargoPlanView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            SizeChanged += OnSizeChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainViewModel oldVm) Unsubscribe(oldVm);
            if (e.NewValue is MainViewModel vm)
            {
                _vm = vm;
                Subscribe(vm);
                Draw();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Draw();

        private void Subscribe(MainViewModel vm)
        {
            vm.CargoTanks.CollectionChanged += OnCollectionChanged;
            vm.BallastTanks.CollectionChanged += OnCollectionChanged;
            vm.Products.CollectionChanged += OnCollectionChanged;
            foreach (var tank in vm.CargoTanks) tank.PropertyChanged += OnTankChanged;
            foreach (var bt in vm.BallastTanks) bt.PropertyChanged += OnTankChanged;
        }

        private void Unsubscribe(MainViewModel vm)
        {
            vm.CargoTanks.CollectionChanged -= OnCollectionChanged;
            vm.BallastTanks.CollectionChanged -= OnCollectionChanged;
            vm.Products.CollectionChanged -= OnCollectionChanged;
            foreach (var tank in vm.CargoTanks) tank.PropertyChanged -= OnTankChanged;
            foreach (var bt in vm.BallastTanks) bt.PropertyChanged -= OnTankChanged;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => Draw();
        private void OnTankChanged(object sender, PropertyChangedEventArgs e) => Draw();

        private void Draw()
        {
            MainCanvas.Children.Clear();
            if (_vm == null) return;

            double canvasWidth = MainCanvas.ActualWidth;
            double canvasHeight = MainCanvas.ActualHeight;
            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            double scale = canvasWidth / TotalLength;
            double sideHeight = canvasHeight / 2;
            double ballastHeight = sideHeight / 3;
            double cargoHeight = sideHeight * 2 / 3;

            DrawFpk(canvasWidth, canvasHeight, scale);
            DrawSide("P", true, scale, ballastHeight, cargoHeight, sideHeight);
            DrawSide("S", false, scale, ballastHeight, cargoHeight, sideHeight);
        }

        private void DrawFpk(double canvasWidth, double canvasHeight, double scale)
        {
            double leftX = FpkStart * scale;
            double rightX = FpkEnd * scale;
            double topY = 0;
            double bottomY = canvasHeight;

            PointCollection points = new PointCollection
            {
                new Point(leftX, topY),
                new Point(leftX, bottomY),
                new Point(rightX, (topY + bottomY) / 2)
            };

            BallastTank fpk = _vm.BallastTanks.FirstOrDefault(b => b.TankNo == "FPK");
            Color fill = Colors.LightGray;
            string text = "FPK";
            if (fpk != null && fpk.WaterVolume.HasValue && fpk.WaterVolume > 0)
            {
                fill = Colors.LightBlue;
                text = $"FPK\n{fpk.WaterVolume.Value:F0} m³";
            }

            Polygon poly = new Polygon
            {
                Points = points,
                Fill = new SolidColorBrush(fill),
                Stroke = new SolidColorBrush(Colors.DarkSlateGray),
                StrokeThickness = 1
            };
            MainCanvas.Children.Add(poly);

            double textWidth = (rightX - leftX) * 0.7;
            double textHeight = canvasHeight * 0.4;
            Color textColor = PerceivedBrightness(fill) > 130 ? Colors.Black : Colors.White;
            Border textBorder = new Border
            {
                Width = textWidth,
                Height = textHeight,
                Child = new TextBlock
                {
                    Text = text,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(textColor),
                    TextWrapping = TextWrapping.Wrap
                }
            };
            Canvas.SetLeft(textBorder, leftX + (rightX - leftX) * 0.15);
            Canvas.SetTop(textBorder, canvasHeight * 0.3);
            MainCanvas.Children.Add(textBorder);
        }

        private void DrawSide(string side, bool isPort, double scale, double ballastH, double cargoH, double sideH)
        {
            double baseY = isPort ? 0 : sideH;
            double yBallast, yCargo;
            if (isPort)
            {
                yBallast = baseY;
                yCargo = baseY + ballastH;
            }
            else
            {
                yCargo = baseY;
                yBallast = baseY + cargoH;
            }

            // Грузовые танки
            foreach (var (start, end, name) in CargoDefs)
            {
                string tankName = name + side;
                double left = start * scale;
                double right = end * scale;
                double width = right - left;

                Tank tank = _vm.CargoTanks.FirstOrDefault(t => t.TankNo == tankName);
                Color fill = Colors.LightGray;
                string tankNo = tankName;
                string grade = "";
                string weight = "";
                string volume = "";
                string percent = "";

                if (tank != null)
                {
                    bool isEmpty = tank.IsEmpty;
                    if (!isEmpty)
                    {
                        var product = _vm.Products.FirstOrDefault(p => p.Name == tank.SelectedProductName);
                        if (product != null) fill = product.Color;
                        grade = tank.SelectedProductName ?? "";
                        weight = tank.Weight.HasValue ? tank.Weight.Value.ToString("F3") + " mt" : "";
                        volume = tank.Volume.HasValue ? tank.Volume.Value.ToString("F3") + " m³" : "";
                        percent = tank.FillPercent.HasValue ? tank.FillPercent.Value.ToString("F1") + "%" : "0%";
                    }
                    else
                    {
                        grade = "EMPTY";
                    }
                }
                else
                {
                    grade = "EMPTY";
                }

                DrawCargoRectangle(left, yCargo, width, cargoH, fill, tankNo, grade, weight, percent, volume);
            }

            // Балластные танки
            foreach (var (start, end, name) in BallastDefs)
            {
                string tankName = name + " " + side;
                double left = start * scale;
                double right = end * scale;
                double width = right - left;

                BallastTank bt = _vm.BallastTanks.FirstOrDefault(b => b.TankNo == tankName);
                Color fill = Colors.LightGray;
                string info = tankName;
                if (bt != null && bt.WaterVolume.HasValue && bt.WaterVolume > 0)
                {
                    fill = Colors.LightBlue;
                    info = $"{tankName}\n{bt.WaterVolume.Value:F0} m³";
                }

                DrawSimpleCell(left, yBallast, width, ballastH, fill, info);
            }
        }

        private void DrawCargoRectangle(double x, double y, double w, double h, Color fill,
            string tankNo, string grade, string weight, string percent, string volume)
        {
            Rectangle rect = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = new SolidColorBrush(fill),
                Stroke = new SolidColorBrush(Colors.DarkSlateGray),
                StrokeThickness = 1,
                RadiusX = 2,
                RadiusY = 2
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            MainCanvas.Children.Add(rect);

            Color textColor = PerceivedBrightness(fill) > 130 ? Colors.Black : Colors.White;

            Grid container = new Grid
            {
                Width = w - 4,
                Height = h - 4
            };
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock tbTank = new TextBlock
            {
                Text = tankNo,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(textColor)
            };
            Grid.SetRow(tbTank, 0);
            container.Children.Add(tbTank);

            string gradeText = string.IsNullOrEmpty(grade) ? "EMPTY" : grade;
            TextBlock tbGrade = new TextBlock
            {
                Text = gradeText,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(textColor)
            };
            Grid.SetRow(tbGrade, 1);
            container.Children.Add(tbGrade);

            TextBlock tbWeight = new TextBlock
            {
                Text = weight,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(textColor)
            };
            Grid.SetRow(tbWeight, 2);
            container.Children.Add(tbWeight);

            Grid bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition());
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition());

            TextBlock tbPercent = new TextBlock
            {
                Text = percent,
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(textColor)
            };
            Grid.SetColumn(tbPercent, 0);
            bottomGrid.Children.Add(tbPercent);

            TextBlock tbVolume = new TextBlock
            {
                Text = volume,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(textColor)
            };
            Grid.SetColumn(tbVolume, 1);
            bottomGrid.Children.Add(tbVolume);

            Grid.SetRow(bottomGrid, 3);
            container.Children.Add(bottomGrid);

            Canvas.SetLeft(container, x + 2);
            Canvas.SetTop(container, y + 2);
            MainCanvas.Children.Add(container);
        }

        private void DrawSimpleCell(double x, double y, double w, double h, Color fill, string text)
        {
            Rectangle rect = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = new SolidColorBrush(fill),
                Stroke = new SolidColorBrush(Colors.DarkSlateGray),
                StrokeThickness = 1,
                RadiusX = 2,
                RadiusY = 2
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            MainCanvas.Children.Add(rect);

            Color textColor = PerceivedBrightness(fill) > 130 ? Colors.Black : Colors.White;
            Border textBorder = new Border
            {
                Width = w - 4,
                Height = h - 4,
                Child = new TextBlock
                {
                    Text = text,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(textColor),
                    TextWrapping = TextWrapping.Wrap
                }
            };
            Canvas.SetLeft(textBorder, x + 2);
            Canvas.SetTop(textBorder, y + 2);
            MainCanvas.Children.Add(textBorder);
        }

        private int PerceivedBrightness(Color c)
        {
            return (int)(c.R * 0.299 + c.G * 0.587 + c.B * 0.114);
        }
    }
}