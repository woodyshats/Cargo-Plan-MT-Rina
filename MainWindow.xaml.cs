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

            // Excel-навигация: сразу ставим фокус в первую редактируемую ячейку
            Dispatcher.BeginInvoke(new Action(() => FocusFirstEditableCell()), DispatcherPriority.Loaded);
        }

        private void FocusFirstEditableCell()
        {
            var grid = CargoDataGrid;
            if (grid == null) grid = FindVisualChild<DataGrid>(this);
            if (grid == null || grid.Items.Count == 0) return;
            // Убедимся, что активна вкладка Table
            if (MainTabControl != null) MainTabControl.SelectedIndex = 0;

            var ordered = grid.Columns.OrderBy(c => c.DisplayIndex).ToList();
            var firstEditable = ordered.FirstOrDefault(c => !c.IsReadOnly);
            if (firstEditable == null) return;
            var firstItem = grid.Items[0];
            TrySetCell(grid, 0, ordered.IndexOf(firstEditable));
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        private static DataGridCell GetDataGridCell(DataGrid grid, object item, DataGridColumn column)
        {
            var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(item);
            if (row == null)
            {
                grid.ScrollIntoView(item, column);
                grid.UpdateLayout();
                row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(item);
                if (row == null) return null;
            }
            // Найти ячейку через визуальное дерево (без DataGridCellsPresenter, чтобы избежать зависимости от Primitives)
            foreach (var cell in FindVisualChildren<DataGridCell>(row))
            {
                if (cell.Column == column) return cell;
            }
            return null;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) yield return t;
                foreach (var sub in FindVisualChildren<T>(child)) yield return sub;
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
            // Ищем первый фокусируемый редактор внутри ячейки (TextBox / ComboBox)
            var editor = cell.FindChild<Control>(false);
            if (editor == null) editor = cell.FindChild<UIElement>(false) as Control;
            if (editor != null)
            {
                editor.Focus();
                if (editor is TextBox tb)
                {
                    tb.SelectAll();
                }
                else if (editor is ComboBox cb)
                {
                    // фокус на ComboBox, текст выделится при открытии
                    cb.Focus();
                    var tbInCb = cb.FindChild<TextBox>(true);
                    tbInCb?.SelectAll();
                }
                return;
            }
            var child = cell.FindChild<UIElement>(false);
            if (child != null)
            {
                child.Focus();
                if (child is TextBox tb2) tb2.SelectAll();
            }
        }

        private void OnDataGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.OriginalSource is DependencyObject source)) return;
            DataGridCell cell = FindParent<DataGridCell>(source);
            if (cell == null) return;
            DataGrid grid = FindParent<DataGrid>(cell);
            if (grid == null || grid.SelectionUnit != DataGridSelectionUnit.Cell) return;

            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (e.Key == Key.Tab)
            {
                e.Handled = true;
                if (shift) MoveToPrevEditableCell(grid, cell);
                else MoveToNextEditableCell(grid, cell);
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                // Enter = вниз, Shift+Enter = вверх (как в Excel)
                MoveVertical(grid, cell, shift ? -1 : 1, allowEditOnTarget: true);
            }
            else if (e.Key == Key.Right)
            {
                e.Handled = true;
                MoveHorizontal(grid, cell, 1);
            }
            else if (e.Key == Key.Left)
            {
                e.Handled = true;
                MoveHorizontal(grid, cell, -1);
            }
            else if (e.Key == Key.Down)
            {
                e.Handled = true;
                MoveVertical(grid, cell, 1, allowEditOnTarget: false);
            }
            else if (e.Key == Key.Up)
            {
                e.Handled = true;
                MoveVertical(grid, cell, -1, allowEditOnTarget: false);
            }
        }

        // ============ Excel-навигация helpers ============

        private List<DataGridColumn> GetOrderedColumns(DataGrid grid)
            => grid.Columns.OrderBy(c => c.DisplayIndex).ToList();

        private void CommitCurrentCell(DataGrid grid)
        {
            try
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }
        }

        private void TrySetCell(DataGrid grid, int rowIndex, int orderedColIndex)
        {
            var ordered = GetOrderedColumns(grid);
            if (orderedColIndex < 0 || orderedColIndex >= ordered.Count) return;
            if (rowIndex < 0) rowIndex = 0;
            if (rowIndex >= grid.Items.Count) rowIndex = grid.Items.Count - 1;

            CommitCurrentCell(grid);

            var item = grid.Items[rowIndex];
            var column = ordered[orderedColIndex];

            grid.SelectedCells.Clear();
            grid.CurrentCell = new DataGridCellInfo(item, column);
            grid.ScrollIntoView(item, column);
            // Фокус и авто-вход в редактирование — без дополнительного клика
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var targetCell = GetDataGridCell(grid, item, column);
                if (targetCell != null)
                {
                    targetCell.Focus();
                    if (!targetCell.IsReadOnly)
                    {
                        targetCell.IsEditing = true;
                        FocusEditor(targetCell);
                    }
                }
                else
                {
                    grid.Focus();
                    if (!column.IsReadOnly) grid.BeginEdit();
                }
            }), DispatcherPriority.Loaded);
        }

        private void MoveHorizontal(DataGrid grid, DataGridCell currentCell, int delta)
        {
            var ordered = GetOrderedColumns(grid);
            int row = grid.Items.IndexOf(currentCell.DataContext);
            if (row < 0) return;
            int colPos = ordered.IndexOf(currentCell.Column);
            if (colPos < 0) return;

            int newCol = colPos + delta;
            int newRow = row;
            if (newCol < 0)
            {
                newRow = row - 1;
                newCol = ordered.Count - 1;
            }
            else if (newCol >= ordered.Count)
            {
                newRow = row + 1;
                newCol = 0;
            }
            // Wrap по строкам как в Excel
            if (newRow < 0) newRow = grid.Items.Count - 1;
            if (newRow >= grid.Items.Count) newRow = 0;

            TrySetCell(grid, newRow, newCol);
        }

        private void MoveVertical(DataGrid grid, DataGridCell currentCell, int delta, bool allowEditOnTarget)
        {
            var ordered = GetOrderedColumns(grid);
            int row = grid.Items.IndexOf(currentCell.DataContext);
            if (row < 0) return;
            int colPos = ordered.IndexOf(currentCell.Column);
            if (colPos < 0) return;

            int newRow = row + delta;
            // Wrap как в Excel (Enter с последней строки — на первую)
            if (newRow < 0) newRow = grid.Items.Count - 1;
            if (newRow >= grid.Items.Count) newRow = 0;

            // Для Enter — остаёмся в той же колонке, даже если она read-only (просто выделяем)
            // Если нужно пропускать read-only для Enter, можно найти ближайшую редактируемую в столбце ниже
            if (allowEditOnTarget)
            {
                // Если целевая колонка read-only, просто выделяем её (как Excel на заблокированной ячейке)
                TrySetCell(grid, newRow, colPos);
                return;
            }

            TrySetCell(grid, newRow, colPos);
        }

        private void MoveToNextEditableCell(DataGrid grid, DataGridCell currentCell)
        {
            var ordered = GetOrderedColumns(grid);
            int row = grid.Items.IndexOf(currentCell.DataContext);
            if (row < 0) return;
            int colPos = ordered.IndexOf(currentCell.Column);
            if (colPos < 0) return;

            int rowCount = grid.Items.Count;
            int colCount = ordered.Count;

            // Поиск следующей редактируемой по строкам слева-направо (Excel Tab)
            for (int r = row; r < rowCount; r++)
            {
                int startC = (r == row) ? colPos + 1 : 0;
                for (int c = startC; c < colCount; c++)
                {
                    if (ordered[c].IsReadOnly) continue;
                    TrySetCell(grid, r, c);
                    return;
                }
            }
            // Wrap к началу
            for (int r = 0; r <= row; r++)
            {
                int endC = (r == row) ? colPos : colCount - 1;
                for (int c = 0; c <= endC; c++)
                {
                    if (ordered[c].IsReadOnly) continue;
                    // уже проверено для r==row и c>colPos, пропускаем
                    if (r == row && c >= colPos) continue;
                    TrySetCell(grid, r, c);
                    return;
                }
            }
        }

        private void MoveToPrevEditableCell(DataGrid grid, DataGridCell currentCell)
        {
            var ordered = GetOrderedColumns(grid);
            int row = grid.Items.IndexOf(currentCell.DataContext);
            if (row < 0) return;
            int colPos = ordered.IndexOf(currentCell.Column);
            if (colPos < 0) return;

            int rowCount = grid.Items.Count;
            int colCount = ordered.Count;

            for (int r = row; r >= 0; r--)
            {
                int startC = (r == row) ? colPos - 1 : colCount - 1;
                for (int c = startC; c >= 0; c--)
                {
                    if (ordered[c].IsReadOnly) continue;
                    TrySetCell(grid, r, c);
                    return;
                }
            }
            // Wrap к концу
            for (int r = rowCount - 1; r >= row; r--)
            {
                int startC = (r == row) ? colCount - 1 : colCount - 1;
                int endC = (r == row) ? colPos : -1;
                for (int c = startC; c > endC; c--)
                {
                    if (ordered[c].IsReadOnly) continue;
                    if (r == row && c >= colPos) continue;
                    TrySetCell(grid, r, c);
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