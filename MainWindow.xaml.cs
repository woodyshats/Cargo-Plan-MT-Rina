using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CargoPlanApp.Behaviors;
using CargoPlanApp.ViewModels;
using CargoPlanApp.Views;

namespace CargoPlanApp
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public static List<string> PlanTypes { get; } = new List<string> { "Preliminary Cargo Plan", "Arrival Cargo Plan", "Departure Cargo Plan", "Current Cargo Plan" };

        private CargoPlanView _cargoPlanView;

        private enum AppSection
        {
            Table,
            CargoPlan
        }

        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;

            ((MainViewModel)DataContext).OnPrintRequested += PrintCargoPlan;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null && vm.HasUnsavedChanges)
            {
                var result = MessageBox.Show("Do you want to save changes?", "Exit", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    vm.Save();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = SystemParameters.WorkArea.Left;
            this.Top = SystemParameters.WorkArea.Top;
            this.Width = SystemParameters.WorkArea.Width;
            this.Height = SystemParameters.WorkArea.Height;
            this.WindowState = WindowState.Normal;

            _cargoPlanView = CargoPlanViewControl;
            ShowSection(AppSection.Table);

            // Excel-навигация: сразу ставим фокус в первую редактируемую ячейку
            Dispatcher.BeginInvoke(new Action(() => ExcelNavigationBehavior.FocusFirstCell(CargoDataGrid)), DispatcherPriority.Loaded);
        }

        // ============ NavigationView (боковое меню) ============

        private static void ExecuteCommand(ICommand? command)
        {
            if (command != null && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }

        private void ShowSection(AppSection section)
        {
            if (TableSection != null)
            {
                TableSection.Visibility = section == AppSection.Table ? Visibility.Visible : Visibility.Collapsed;
            }

            if (CargoPlanSection != null)
            {
                CargoPlanSection.Visibility = section == AppSection.CargoPlan ? Visibility.Visible : Visibility.Collapsed;
            }

            // Пункты навигации не имеют TargetPageType, поэтому SelectedItem не меняется —
            // подсветку активного раздела поддерживаем вручную.
            if (NavTableItem != null)
            {
                NavTableItem.IsActive = section == AppSection.Table;
            }

            if (NavCargoPlanItem != null)
            {
                NavCargoPlanItem.IsActive = section == AppSection.CargoPlan;
            }
        }

        private void NavTableItem_Click(object sender, RoutedEventArgs e) => ShowSection(AppSection.Table);

        private void NavCargoPlanItem_Click(object sender, RoutedEventArgs e) => ShowSection(AppSection.CargoPlan);

        private void NavNew_Click(object sender, RoutedEventArgs e) => ExecuteCommand((DataContext as MainViewModel)?.NewCommand);

        private void NavOpen_Click(object sender, RoutedEventArgs e) => ExecuteCommand((DataContext as MainViewModel)?.OpenCommand);

        private void NavSave_Click(object sender, RoutedEventArgs e) => ExecuteCommand((DataContext as MainViewModel)?.SaveCommand);

        private void NavSaveAs_Click(object sender, RoutedEventArgs e) => ExecuteCommand((DataContext as MainViewModel)?.SaveAsCommand);

        private void NavPrint_Click(object sender, RoutedEventArgs e) => ExecuteCommand((DataContext as MainViewModel)?.PrintCommand);

        private void NavProducts_Click(object sender, RoutedEventArgs e) => ExecuteCommand((DataContext as MainViewModel)?.ManageProductsCommand);

        private void NavMaxVolumes_Click(object sender, RoutedEventArgs e) => ExecuteCommand((DataContext as MainViewModel)?.SetMaxVolumesCommand);

        private void PrintCargoPlan()
        {
            var vm = (MainViewModel)DataContext;
            if (vm == null) return;

            FlowDocument flowDoc = new FlowDocument();
            flowDoc.PageWidth = 1122;
            flowDoc.PageHeight = 793;
            flowDoc.PagePadding = new Thickness(15);
            flowDoc.ColumnWidth = double.PositiveInfinity;
            flowDoc.FontFamily = new FontFamily("Segoe UI");
            flowDoc.FontSize = 10;
            flowDoc.Foreground = Brushes.Black;
            flowDoc.Background = Brushes.White;

            // Красивая версия cargo plan для печати (как на образце)
            double planWidth = 1050;
            var printView = new CargoPlanPrintView();
            printView.Width = planWidth;
            printView.DataContext = vm;
            // Явная перестройка с корректной шириной (DataContext уже с Width)
            printView.Rebuild();
            printView.Measure(new Size(planWidth, double.PositiveInfinity));
            double desiredHeight = printView.DesiredSize.Height;
            if (double.IsNaN(desiredHeight) || double.IsInfinity(desiredHeight) || desiredHeight < 400) desiredHeight = 650;
            if (desiredHeight > 1200) desiredHeight = 1200;
            printView.Arrange(new Rect(0, 0, planWidth, desiredHeight));
            printView.UpdateLayout();

            // Рендер без повторного InvalidateMeasure (чтобы не триггерить Build во время layout)
            var planBitmap = RenderToBitmapNoReMeasure(printView, planWidth, desiredHeight, Colors.White);
            Image planImage = new Image { Source = planBitmap, Width = planWidth, Height = desiredHeight, Stretch = Stretch.Uniform };
            BlockUIContainer planContainer = new BlockUIContainer(planImage);
            planContainer.Margin = new Thickness(0);
            flowDoc.Blocks.Add(planContainer);

            printView.DataContext = null;
            GC.Collect();

            PrintPreviewWindow previewWindow = new PrintPreviewWindow(flowDoc);
            previewWindow.Owner = this;
            previewWindow.ShowDialog();
        }

        private void AddTableCell(TableRow row, string text, FontWeight fontWeight)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text)));
            cell.BorderBrush = Brushes.Gray;
            cell.BorderThickness = new Thickness(0.5);
            cell.Padding = new Thickness(4);
            cell.FontWeight = fontWeight;
            cell.Foreground = Brushes.Black;
            cell.Background = Brushes.White;
            row.Cells.Add(cell);
        }

        private BitmapSource RenderVisualToBitmap(UIElement visual, double width, double height, Color backgroundColor)
        {
            visual.InvalidateMeasure();
            visual.InvalidateArrange();
            visual.Measure(new Size(width, height));
            visual.Arrange(new Rect(0, 0, width, height));
            visual.UpdateLayout();

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual backgroundVisual = new DrawingVisual();
            using (DrawingContext dc = backgroundVisual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(backgroundColor), null, new Rect(0, 0, width, height));
            }
            rtb.Render(backgroundVisual);
            rtb.Render(visual);

            return rtb;
        }

        private BitmapSource RenderToBitmapNoReMeasure(UIElement visual, double width, double height, Color backgroundColor)
        {
            // Visual уже измерен и зааранжен - просто рендерим без повторного Measure (исключает reentrancy Build)
            int w = Math.Max(1, (int)Math.Ceiling(width));
            int h = Math.Max(1, (int)Math.Ceiling(height));
            RenderTargetBitmap rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual backgroundVisual = new DrawingVisual();
            using (DrawingContext dc = backgroundVisual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(backgroundColor), null, new Rect(0, 0, width, height));
            }
            rtb.Render(backgroundVisual);
            rtb.Render(visual);
            return rtb;
        }

    }
}