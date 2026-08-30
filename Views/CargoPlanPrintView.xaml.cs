using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CargoPlanApp.Models;
using CargoPlanApp.ViewModels;

namespace CargoPlanApp.Views
{
    public partial class CargoPlanPrintView : UserControl
    {
        private MainViewModel _vm;

        private bool _isBuilding;

        public CargoPlanPrintView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += (s, e) => Build();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _vm = e.NewValue as MainViewModel;
            Build();
        }

        public void Rebuild() => Build();

        private void Build()
        {
            if (_isBuilding) return;
            if (_vm == null) return;
            if (ActualWidth <= 0 && Width <= 0 && double.IsNaN(Width)) return;
            double w = !double.IsNaN(Width) && Width > 0 ? Width : ActualWidth;
            if (w <= 0) w = 1080;
            _isBuilding = true;
            try
            {
                RootGrid.Children.Clear();
                RootGrid.RowDefinitions.Clear();
                RootGrid.ColumnDefinitions.Clear();

            // Root rows: header, ship, draftBar, legend
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = BuildHeader();
            Grid.SetRow(header, 0);
            RootGrid.Children.Add(header);

            var ship = BuildShipDiagram(w - 20); // minus padding
            Grid.SetRow(ship, 1);
            RootGrid.Children.Add(ship);

            var draftBar = BuildDraftBar();
            Grid.SetRow(draftBar, 2);
            RootGrid.Children.Add(draftBar);

            var legend = BuildLegend();
            Grid.SetRow(legend, 3);
            RootGrid.Children.Add(legend);
            }
            finally
            {
                _isBuilding = false;
            }
        }

        private UIElement BuildHeader()
        {
            var outer = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row0: mt Rina | CARGO PLAN
            var r0 = new Grid();
            r0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r0.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var tbRina = new TextBlock { Text = "mt Rina", FontSize = 9, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Bottom };
            Grid.SetColumn(tbRina, 0);
            r0.Children.Add(tbRina);
            string planTitle = string.IsNullOrWhiteSpace(_vm.PlanType) ? "CARGO PLAN" : _vm.PlanType.ToUpperInvariant();
            var tbTitle = new TextBlock { Text = planTitle, FontSize = 18, FontWeight = FontWeights.Bold, FontStyle = FontStyles.Italic, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(tbTitle, 1);
            r0.Children.Add(tbTitle);
            Grid.SetRow(r0, 0);
            outer.Children.Add(r0);

            // Row1: Date: 17.02.2025 | Voyage: 02/25
            var r1 = new Grid { Margin = new Thickness(0, 2, 0, 0) };
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            r1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var tbDateLabel = new TextBlock { Text = "Date:", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(tbDateLabel, 0);
            r1.Children.Add(tbDateLabel);
            var dateStr = _vm.Date.ToString("dd.MM.yyyy");
            var dateBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(4, 0, 20, 0), Padding = new Thickness(0, 0, 0, 1), MinWidth = 100 };
            dateBorder.Child = new TextBlock { Text = dateStr, FontSize = 9, TextAlignment = TextAlignment.Center, Foreground = Brushes.Black };
            Grid.SetColumn(dateBorder, 1);
            r1.Children.Add(dateBorder);

            var tbVoyageLabel = new TextBlock { Text = "Voyage:", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) };
            Grid.SetColumn(tbVoyageLabel, 3);
            r1.Children.Add(tbVoyageLabel);
            var voyageVal = string.IsNullOrWhiteSpace(_vm.Voyage) ? "" : _vm.Voyage;
            var voyBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(0, 0, 0, 1), MinWidth = 80 };
            voyBorder.Child = new TextBlock { Text = voyageVal, FontSize = 9, TextAlignment = TextAlignment.Center, Foreground = Brushes.Black };
            Grid.SetColumn(voyBorder, 4);
            r1.Children.Add(voyBorder);
            Grid.SetRow(r1, 1);
            outer.Children.Add(r1);

            // Row2: Port of loading | Terminal | Port of discharge
            var r2 = new Grid { Margin = new Thickness(0, 2, 0, 0) };
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.6, GridUnitType.Star) });

            var tbPol = new TextBlock { Text = "Port of loading:", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(tbPol, 0);
            r2.Children.Add(tbPol);
            var polVal = string.IsNullOrWhiteSpace(_vm.PortOfLoading) ? "" : _vm.PortOfLoading;
            var polBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(4, 0, 8, 0), Padding = new Thickness(0, 0, 0, 1) };
            polBorder.Child = new TextBlock { Text = polVal, FontSize = 9, TextAlignment = TextAlignment.Center, Foreground = Brushes.Black };
            Grid.SetColumn(polBorder, 1);
            r2.Children.Add(polBorder);

            var tbTerm = new TextBlock { Text = "Terminal:", FontSize = 9, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(tbTerm, 2);
            r2.Children.Add(tbTerm);
            var termVal = string.IsNullOrWhiteSpace(_vm.Terminal) ? "" : _vm.Terminal;
            var termBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(4, 0, 8, 0), Padding = new Thickness(0, 0, 0, 1), MinWidth = 70 };
            termBorder.Child = new TextBlock { Text = termVal, FontSize = 9, TextAlignment = TextAlignment.Center, Foreground = Brushes.Black };
            Grid.SetColumn(termBorder, 3);
            r2.Children.Add(termBorder);

            var tbPod = new TextBlock { Text = "Port of discharge:", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(tbPod, 4);
            r2.Children.Add(tbPod);
            var podVal = string.IsNullOrWhiteSpace(_vm.PortOfDischarge) ? "" : _vm.PortOfDischarge;
            var podBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(0, 0, 0, 1) };
            podBorder.Child = new TextBlock { Text = podVal, FontSize = 9, TextAlignment = TextAlignment.Center, Foreground = Brushes.Black };
            Grid.SetColumn(podBorder, 5);
            r2.Children.Add(podBorder);

            Grid.SetRow(r2, 2);
            outer.Children.Add(r2);

            return new Border { Child = outer, Padding = new Thickness(2, 0, 2, 0), Background = Brushes.White };
        }

        private UIElement BuildShipDiagram(double totalWidth)
        {
            // Ship grid
            double shipWidth = totalWidth;
            double shipHeight = 280;
            // use Grid with 9 cols, 4 rows
            var grid = new Grid { Width = shipWidth, Height = shipHeight, Background = Brushes.White, ClipToBounds = true };
            // columns: SLOP (0.75), COT7-1 each 1.0, FPK 0.45
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.75, GridUnitType.Star) });
            for (int i = 0; i < 7; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.45, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) }); // WBT top
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // COT P
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // COT S
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) }); // WBT bottom

            // Draw bow chamfer and stern triangle as overlay paths? We'll add left bow cut shape as white overlay? Simpler: draw hull polygon as background path.
            // We'll add outer hull polygon behind cells
            var hullPath = new Path
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1.2,
                Fill = Brushes.Transparent,
                IsHitTestVisible = false
            };
            // We'll create hull geometry after grid is measured? Instead create simple outer border via grid's outerBorder. For bow cut, add two small polygons to hide corners.
            // Top-left chamfer
            // Instead of complex hull, we just keep rectangular outer border and add triangular notches visually by covering?
            // Let's create bow cut: small white triangles at top-left and bottom-left corners with black diagonal border
            // This mimics the image where bow has cut corners.

            // Top WBT cells - 6 cells spanning columns 1-7
            var topBallasts = new[] { "WBT 5 P", "WWT P", "WBT 4 P", "WBT 3 P", "WBT 2 P", "WBT 1 P" };
            var topGrid = new Grid();
            for (int i = 0; i < 6; i++) topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 6; i++)
            {
                var bt = FindBallast(topBallasts[i]);
                var cell = BuildBallastCell(topBallasts[i], bt, true);
                Grid.SetColumn(cell, i);
                topGrid.Children.Add(cell);
            }
            Grid.SetRow(topGrid, 0);
            Grid.SetColumn(topGrid, 1);
            Grid.SetColumnSpan(topGrid, 7);
            grid.Children.Add(topGrid);

            // Bottom WBT cells
            var botBallasts = new[] { "WBT 5 S", "WWT S", "WBT 4 S", "WBT 3 S", "WBT 2 S", "WBT 1 S" };
            var botGrid = new Grid();
            for (int i = 0; i < 6; i++) botGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 6; i++)
            {
                var bt = FindBallast(botBallasts[i]);
                var cell = BuildBallastCell(botBallasts[i], bt, false);
                Grid.SetColumn(cell, i);
                botGrid.Children.Add(cell);
            }
            Grid.SetRow(botGrid, 3);
            Grid.SetColumn(botGrid, 1);
            Grid.SetColumnSpan(botGrid, 7);
            grid.Children.Add(botGrid);

            // FPK triangle spanning all 4 rows in last column
            var fpk = FindBallast("FPK");
            var fpkPolygon = new Polygon
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Stretch = Stretch.Fill,
                Points = new PointCollection { new Point(0, 0), new Point(0, 1), new Point(1, 0.5) }
            };
            // Fill based on FPK volume
            if (fpk != null && fpk.WaterVolume.HasValue && fpk.WaterVolume.Value > 0.001)
                fpkPolygon.Fill = new SolidColorBrush(Color.FromRgb(0x8E, 0xC8, 0xFF)); // light blue for ballast
            else
                fpkPolygon.Fill = new SolidColorBrush(Colors.White);
            var fpkGrid = new Grid();
            fpkGrid.Children.Add(fpkPolygon);
            var fpkLabel = new TextBlock
            {
                Text = BuildFpkText(fpk),
                FontSize = 7,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Black
            };
            fpkGrid.Children.Add(fpkLabel);
            Grid.SetRow(fpkGrid, 0);
            Grid.SetRowSpan(fpkGrid, 4);
            Grid.SetColumn(fpkGrid, 8);
            grid.Children.Add(fpkGrid);

            // Need to ensure fpk polygon stretches - set HorizontalAlignment Stretch etc. We'll handle via Loaded sizing.
            // For now make it fill available space via binding Width/Height to parent - use Grid approach: Polygon with Stretch Fill will fill Grid.

            // SLOP and COT cells
            var cotNamesP = new[] { "SLOP P", "COT 7P", "COT 6P", "COT 5P", "COT 4P", "COT 3P", "COT 2P", "COT 1P" };
            var cotNamesS = new[] { "SLOP S", "COT 7S", "COT 6S", "COT 5S", "COT 4S", "COT 3S", "COT 2S", "COT 1S" };
            for (int col = 0; col < 8; col++)
            {
                var tankP = FindCargo(cotNamesP[col]);
                var cellP = BuildCargoCell(tankP, cotNamesP[col]);
                Grid.SetRow(cellP, 1);
                Grid.SetColumn(cellP, col);
                grid.Children.Add(cellP);

                var tankS = FindCargo(cotNamesS[col]);
                var cellS = BuildCargoCell(tankS, cotNamesS[col]);
                Grid.SetRow(cellS, 2);
                Grid.SetColumn(cellS, col);
                grid.Children.Add(cellS);
            }

            // Bow chamfer overlay - two small triangles at left edge top and bottom to simulate cut bow
            // Top chamfer
            var topChamfer = new Polygon
            {
                Points = new PointCollection { new Point(0, 0), new Point(14, 0), new Point(0, 14) },
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            var topWrapper = new Canvas { Width = 14, Height = 14, Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            topWrapper.Children.Add(topChamfer);
            Grid.SetRow(topWrapper, 0);
            Grid.SetRowSpan(topWrapper, 1);
            Grid.SetColumn(topWrapper, 0);
            // Instead of spanning, we overlay directly on outerBorder? Simpler to just keep chamfer as part of outer border visual - add to grid
            // We'll add the chamfer canvases directly to grid with absolute positioning via Canvas inside Grid - but easier to just ignore for now.
            // To keep ship look, we already have rectangular shape - the chamfer is a minor aesthetic.

            // Wrap in container without outer frame (рамка вокруг схемы убрана)
            var container = new Grid();
            container.Children.Add(grid);
            return container;
        }

        private Border BuildCargoCell(Tank tank, string tankNo)
        {
            bool isSlop = tankNo.StartsWith("SLOP");
            string displayNo = tankNo.Replace("COT ", "COT №"); // like COT №7P
            // Determine fill and texts
            Color fillColor;
            string gradeLine = "";
            string volText = "";
            string mtText = "";
            string pctText = "";
            bool isEmpty = true;

            if (tank != null)
            {
                // Consider empty if weight null/0 or product Empty
                if (!tank.IsEmpty)
                {
                    isEmpty = false;
                    var prod = _vm.Products.FirstOrDefault(p => p.Name == tank.SelectedProductName);
                    fillColor = prod != null ? prod.Color : Colors.LightGray;
                    gradeLine = tank.SelectedProductName ?? "";
                    volText = tank.Volume.HasValue ? $"{tank.Volume.Value.ToString("F3", CultureInfo.InvariantCulture).Replace('.', ',')} m3" : "0,000 m3";
                    mtText = tank.Weight.HasValue ? $"{tank.Weight.Value.ToString("F3", CultureInfo.InvariantCulture).Replace('.', ',')} mt" : "0,000 mt";
                    pctText = tank.FillPercent.HasValue ? $"{tank.FillPercent.Value.ToString("F0", CultureInfo.InvariantCulture)}%" : "0%";
                }
                else
                {
                    // Empty or water? Check if product is Water with zero weight still empty?
                    if (tank.SelectedProductName != null && tank.SelectedProductName != "Empty" && tank.SelectedProductName.ToLower().Contains("water"))
                    {
                        // treat as water ballast? But still empty by IsEmpty if weight 0
                        isEmpty = true;
                    }
                    fillColor = Colors.White;
                    gradeLine = string.IsNullOrWhiteSpace(tank.SelectedProductName) || tank.SelectedProductName == "Empty" ? "Empty" : tank.SelectedProductName;
                    // For SLOP water case with weight?
                    if (tank.Weight.HasValue && tank.Weight.Value > 0)
                    {
                        volText = tank.Volume.HasValue ? $"{tank.Volume.Value.ToString("F3", CultureInfo.InvariantCulture).Replace('.', ',')} m3" : $"{tank.Weight.Value.ToString("F3", CultureInfo.InvariantCulture).Replace('.', ',')} m3";
                        mtText = $"{tank.Weight.Value.ToString("F3", CultureInfo.InvariantCulture).Replace('.', ',')} mt";
                        pctText = tank.FillPercent.HasValue ? $"{tank.FillPercent.Value.ToString("F0")}%" : "0%";
                        // if has weight, not empty visually - use product color if available
                        var prod = _vm.Products.FirstOrDefault(p => p.Name == tank.SelectedProductName);
                        if (prod != null) fillColor = prod.Color;
                        isEmpty = false;
                    }
                    else
                    {
                        volText = "0,000 m3";
                        mtText = "0,000 mt";
                        pctText = "0%";
                    }
                }
            }
            else
            {
                fillColor = Colors.White;
                gradeLine = "Empty";
                volText = "0,000 m3";
                mtText = "0,000 mt";
                pctText = "0%";
            }

            // Special handling for SLOP Water with volume: product Water density 1.0, fill blue? In image Water is light blue (#D9E9F5) with black text
            if (gradeLine != null && gradeLine.ToLower().Contains("water"))
            {
                fillColor = Color.FromRgb(0xD9, 0xE9, 0xF5);
            }

            var bg = new SolidColorBrush(fillColor);
            Color fg = PerceivedBrightness(fillColor) > 130 ? Colors.Black : Colors.White;
            var fgBrush = new SolidColorBrush(fg);

            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.7),
                Background = bg,
                Padding = new Thickness(1)
            };

            var inner = new Grid();
            inner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // tankNo
            inner.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // grade
            inner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // vol
            inner.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // mt + pct

            var tbNo = new TextBlock { Text = displayNo, FontSize = 6.5, FontWeight = FontWeights.Bold, Foreground = fgBrush, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetRow(tbNo, 0);
            inner.Children.Add(tbNo);

            var tbGrade = new TextBlock
            {
                Text = gradeLine,
                FontSize = isSlop ? 6.5 : 7,
                FontWeight = FontWeights.Bold,
                Foreground = fgBrush,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 1)
            };
            Grid.SetRow(tbGrade, 1);
            inner.Children.Add(tbGrade);

            var tbVol = new TextBlock { Text = volText, FontSize = 6.5, Foreground = fgBrush, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetRow(tbVol, 2);
            inner.Children.Add(tbVol);

            var bottom = new Grid { Margin = new Thickness(0, 1, 0, 0) };
            bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var tbPct = new TextBlock { Text = pctText, FontSize = 6, FontWeight = FontWeights.Normal, Foreground = fgBrush, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
            Grid.SetColumn(tbPct, 0);
            bottom.Children.Add(tbPct);
            var tbMt = new TextBlock { Text = mtText, FontSize = 6.5, FontWeight = FontWeights.Bold, Foreground = fgBrush, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            // underline mt? In image mt maybe bold
            Grid.SetColumn(tbMt, 1);
            bottom.Children.Add(tbMt);
            Grid.SetRow(bottom, 3);
            inner.Children.Add(bottom);

            border.Child = inner;
            return border;
        }

        private Border BuildBallastCell(string name, BallastTank bt, bool isTop)
        {
            bool hasWater = bt != null && bt.WaterVolume.HasValue && bt.WaterVolume.Value > 0.001;
            Color fill = hasWater ? Color.FromRgb(0xB8, 0xD9, 0xF5) : Colors.White;
            string displayName = name; // e.g. WBT 5P
            string vol = hasWater ? $"{bt.WaterVolume.Value.ToString("F0", CultureInfo.InvariantCulture)} m3" : "0 m3";

            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.7),
                Background = new SolidColorBrush(fill),
                Padding = new Thickness(1)
            };
            var stack = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var tbName = new TextBlock { Text = displayName, FontSize = 6, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.Black };
            var tbVol = new TextBlock { Text = vol, FontSize = 6, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.Black };
            stack.Children.Add(tbName);
            stack.Children.Add(tbVol);
            border.Child = stack;
            return border;
        }

        private UIElement BuildDraftBar()
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: Draft + Water Density
            var leftStack = new StackPanel { Orientation = Orientation.Vertical };
            var draftLine = new StackPanel { Orientation = Orientation.Horizontal };
            draftLine.Children.Add(new TextBlock { Text = "Draft:", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center });
            var tbDraft = new TextBlock { FontSize = 8, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center };
            if (_vm.Draft.HasValue)
            {
                double draft = _vm.Draft.Value;
                tbDraft.Inlines.Add(new System.Windows.Documents.Run($"Tmax=") { FontWeight = FontWeights.Bold });
                tbDraft.Inlines.Add(new System.Windows.Documents.Run($"  {draft.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',')} m"));
            }
            else
            {
                tbDraft.Text = "Tmax= —";
            }
            // underline
            var draftBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 1), Child = tbDraft, Padding = new Thickness(0, 0, 0, 1), Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            draftLine.Children.Add(draftBorder);
            leftStack.Children.Add(draftLine);

            var wdLine = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
            wdLine.Children.Add(new TextBlock { Text = "Water Density", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center });
            string wdStr = _vm.WaterDensity.HasValue ? _vm.WaterDensity.Value.ToString("F3", CultureInfo.InvariantCulture).Replace('.', ',') : "—";
            var wdBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 1), MinWidth = 40, Margin = new Thickness(10, 0, 0, 0), Padding = new Thickness(0, 0, 0, 1) };
            wdBorder.Child = new TextBlock { Text = wdStr, FontSize = 9, Foreground = Brushes.Black, TextAlignment = TextAlignment.Center };
            wdLine.Children.Add(wdBorder);
            leftStack.Children.Add(wdLine);

            Grid.SetColumn(leftStack, 0);
            grid.Children.Add(leftStack);

            // Center: BALLAST total
            double ballastTotal = _vm.BallastTanks.Where(b => b.WaterVolume.HasValue).Sum(b => b.WaterVolume.Value);
            var ballastPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(20, 0, 20, 0) };
            ballastPanel.Children.Add(new TextBlock { Text = "BALLAST:", FontSize = 8, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center });
            ballastPanel.Children.Add(new TextBlock { Text = $"{ballastTotal.ToString("F0", CultureInfo.InvariantCulture)} m3", FontSize = 8, Foreground = Brushes.Black, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
            Grid.SetColumn(ballastPanel, 1);
            grid.Children.Add(ballastPanel);

            // Right placeholder
            return new Border { Child = grid, Background = Brushes.White, Padding = new Thickness(2, 0, 2, 0) };
        }

        private UIElement BuildLegend()
        {
            // Group cargo tanks by grade where weight>0
            var active = _vm.CargoTanks.Where(t => !string.IsNullOrEmpty(t.SelectedProductName) && t.SelectedProductName != "Empty" && t.Weight.HasValue && t.Weight.Value > 0.001).ToList();
            var groups = active.GroupBy(t => t.SelectedProductName).ToList();
            // Also need to include Water grade if present but maybe weight zero? In image Water has 31k still active so included
            // Sort order: as in Products order or as appears? We'll keep Products order then any remaining

            // Build outer grid
            var outer = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            // no header, just rows
            int rowCount = groups.Count() + 1; // +1 for total
            for (int i = 0; i < rowCount; i++) outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); // grade
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) }); // density
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) }); // temp
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); // weight
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // volume

            int r = 0;
            foreach (var g in groups)
            {
                var prod = _vm.Products.FirstOrDefault(p => p.Name == g.Key);
                Color c = prod != null ? prod.Color : Colors.LightGray;
                // special water
                if (g.Key.ToLower().Contains("water")) c = Color.FromRgb(0xD9, 0xE9, 0xF5);
                Brush bg = new SolidColorBrush(c);
                Color fg = PerceivedBrightness(c) > 130 ? Colors.Black : Colors.White;
                Brush fgBrush = new SolidColorBrush(fg);

                double totalW = g.Sum(t => t.Weight ?? 0);
                double totalV = g.Sum(t => t.Volume ?? 0);
                double avgTemp = totalV > 0 ? g.Sum(t => (t.Temperature ?? 15) * (t.Volume ?? 0)) / totalV : g.Average(t => t.Temperature ?? 15);

                // Create 5 cells per row with background
                var cells = new[]
                {
                    CreateLegendCell(g.Key, bg, fgBrush, TextAlignment.Left, true),
                    CreateLegendCell($"Density (15C): { (prod!=null? prod.Density15.ToString("F4", CultureInfo.InvariantCulture).Replace('.', ',') : "-")}", bg, fgBrush, TextAlignment.Left, false),
                    CreateLegendCell($"Temp: {avgTemp.ToString("F0", CultureInfo.InvariantCulture)} °C", bg, fgBrush, TextAlignment.Left, false),
                    CreateLegendCell($"{totalW.ToString("F3", CultureInfo.InvariantCulture).Replace('.', ',')} mt VAC", bg, fgBrush, TextAlignment.Right, true),
                    CreateLegendCell($"{totalV.ToString("F3", CultureInfo.InvariantCulture).Replace('.', ',')} m3", bg, fgBrush, TextAlignment.Right, false),
                };
                for (int ci = 0; ci < 5; ci++)
                {
                    Grid.SetRow(cells[ci], r);
                    Grid.SetColumn(cells[ci], ci);
                    outer.Children.Add(cells[ci]);
                }
                r++;
            }

            // Total row
            double totW = groups.SelectMany(g => g).Sum(t => t.Weight ?? 0);
            // Include also water tanks that might be grouped already, but ballast not in cargo
            // For total volume, sum same
            double totV = groups.SelectMany(g => g).Sum(t => t.Volume ?? 0);
            Brush totalBg = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));
            // If no groups, totW/V already 0
            var totalCells = new[]
            {
                CreateLegendCell("", totalBg, Brushes.Black, TextAlignment.Left, false),
                CreateLegendCell("", totalBg, Brushes.Black, TextAlignment.Left, false),
                CreateLegendCell("Total:", totalBg, Brushes.Black, TextAlignment.Right, true),
                CreateLegendCell($"{totW.ToString("F3", CultureInfo.InvariantCulture).Replace('.', ',')} mt VAC", totalBg, Brushes.Black, TextAlignment.Right, true),
                CreateLegendCell($"{totV.ToString("F3", CultureInfo.InvariantCulture).Replace('.', ',')} m3", totalBg, Brushes.Black, TextAlignment.Right, true),
            };
            for (int ci = 0; ci < 5; ci++)
            {
                Grid.SetRow(totalCells[ci], r);
                Grid.SetColumn(totalCells[ci], ci);
                outer.Children.Add(totalCells[ci]);
            }

            // Рамка вокруг таблицы убрана
            return outer;
        }

        private Border CreateLegendCell(string text, Brush bg, Brush fg, TextAlignment align, bool bold)
        {
            var b = new Border { Background = bg, BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5), Padding = new Thickness(3, 2, 3, 2) };
            var tb = new TextBlock { Text = text, Foreground = fg, FontSize = 7.5, TextAlignment = align, VerticalAlignment = VerticalAlignment.Center, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal };
            b.Child = tb;
            return b;
        }

        private Tank FindCargo(string name) => _vm.CargoTanks.FirstOrDefault(t => t.TankNo == name);
        private BallastTank FindBallast(string name) => _vm.BallastTanks.FirstOrDefault(b => b.TankNo == name);

        private string BuildFpkText(BallastTank fpk)
        {
            if (fpk != null && fpk.WaterVolume.HasValue && fpk.WaterVolume.Value > 0.001)
                return $"FPK\n{fpk.WaterVolume.Value.ToString("F0", CultureInfo.InvariantCulture)} m3";
            return "FPK\n0 m3";
        }

        private int PerceivedBrightness(Color c) => (int)(c.R * 0.299 + c.G * 0.587 + c.B * 0.114);
    }
}
