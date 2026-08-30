using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CargoPlanApp.ViewModels;
using CargoPlanApp.Views;

namespace CargoPlanApp
{
    public partial class MainWindow : Window
    {
        public static List<string> PlanTypes { get; } = new List<string> { "Preliminary Cargo Plan", "Arrival Cargo Plan", "Departure Cargo Plan", "Current Cargo Plan" };

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_THICKFRAME = 0x40000;

        private CargoPlanView _cargoPlanView;

        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
            this.AddHandler(DataGrid.PreviewKeyDownEvent, new KeyEventHandler(OnDataGridPreviewKeyDown), true);
            SourceInitialized += MainWindow_SourceInitialized;

            ((MainViewModel)DataContext).OnPrintRequested += PrintCargoPlan;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(hwnd, GWL_STYLE);
            style &= ~WS_THICKFRAME;
            SetWindowLong(hwnd, GWL_STYLE, style);
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

            var tabControl = MainTabControl;
            if (tabControl != null && tabControl.Items.Count > 1)
            {
                var cargoTab = tabControl.Items[1] as TabItem;
                if (cargoTab != null)
                {
                    var grid = cargoTab.Content as Grid;
                    if (grid != null && grid.Children.Count > 1)
                        _cargoPlanView = grid.Children[1] as CargoPlanView;
                }
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { /* заблокировано */ }
            else DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

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

        private void DataGridCell_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridCell cell && !cell.IsEditing && !cell.IsReadOnly)
            {
                cell.IsEditing = true;
                Dispatcher.BeginInvoke(new Action(() => FocusEditor(cell)), DispatcherPriority.Loaded);
            }
        }

        private void CellEditControl_GotFocus(object sender, RoutedEventArgs e)
        {
            DependencyObject dep = sender as DependencyObject;
            while (dep != null && !(dep is DataGridCell))
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridCell cell && !cell.IsEditing && !cell.IsReadOnly)
            {
                cell.IsEditing = true;
                if (sender is UIElement element)
                    element.Focus();
            }
        }

        private void FocusEditor(DataGridCell cell)
        {
            var child = cell.FindChild<UIElement>(false);
            if (child != null)
            {
                child.Focus();
                if (child is TextBox tb)
                    tb.SelectAll();
            }
        }

        private void OnDataGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source)
            {
                DataGridCell cell = FindParent<DataGridCell>(source);
                if (cell != null)
                {
                    DataGrid grid = FindParent<DataGrid>(cell);
                    if (grid != null && grid.SelectionUnit == DataGridSelectionUnit.Cell)
                    {
                        if (e.Key == Key.Enter)
                        {
                            e.Handled = true;
                            MoveToNextEditableCell(grid, cell);
                        }
                        else if (e.Key == Key.Tab)
                        {
                            e.Handled = true;
                            MoveToNextEditableCell(grid, cell);
                        }
                    }
                }
            }
        }

        private void MoveToNextEditableCell(DataGrid grid, DataGridCell currentCell)
        {
            int currentRowIndex = grid.Items.IndexOf(currentCell.DataContext);
            if (currentRowIndex < 0) return;

            int currentColIndex = currentCell.Column.DisplayIndex;
            int rowCount = grid.Items.Count;
            int colCount = grid.Columns.Count;

            for (int rowOffset = 0; rowOffset <= rowCount; rowOffset++)
            {
                int row = (currentRowIndex + rowOffset) % rowCount;
                int startCol = (rowOffset == 0) ? currentColIndex + 1 : 0;

                for (int col = startCol; col < colCount; col++)
                {
                    var column = grid.Columns[col];
                    if (column.IsReadOnly) continue;

                    var item = grid.Items[row];
                    if (item == null) continue;

                    DataGridCellInfo cellInfo = new DataGridCellInfo(item, column);
                    grid.CurrentCell = cellInfo;
                    grid.BeginEdit();
                    return;
                }
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
                parent = VisualTreeHelper.GetParent(parent);
            return parent as T;
        }
    }

    internal static class VisualTreeExtensions
    {
        public static T FindChild<T>(this DependencyObject parent, bool includeSelf = true) where T : DependencyObject
        {
            if (parent == null) return null;
            if (includeSelf && parent is T typedSelf)
                return typedSelf;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                T found = FindChild<T>(child, true);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}