using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace CargoPlanApp.Behaviors
{
    /// <summary>
    /// Единое Excel-подобное поведение для DataGrid.
    ///
    /// Модель навигации (hotfix 6.4, "Safe mode"):
    ///  - Behavior НЕ управляет свойством <see cref="DataGridCell.IsEditing"/> вручную.
    ///    Причина: ручная установка IsEditing входит в конфликт с собственным
    ///    DataGrid.EndEdit, который вызывается при смене текущей ячейки/фокуса, что
    ///    приводило к бесконечной пересборке шаблона ячейки и залипанию UI-потока.
    ///  - Ячейки с постоянно видимым редактором (CellTemplate с TextBox/ComboBox)
    ///    получают фокус прямо в редактор — ввод работает сразу.
    ///  - Ячейки без видимого редактора (DataGridTextColumn) редактируются штатным
    ///    способом WPF: F2 / двойной клик вызывает DataGrid.BeginEdit(), либо ввод
    ///    символа автоматически начинает редактирование.
    ///  - В режиме редактирования стрелки двигают курсор ВНУТРИ редактора;
    ///    Enter/Tab фиксируют значение и перемещают ячейку (вниз / вправо);
    ///    Escape отменяет редактирование.
    ///  - В режиме навигации стрелки перемещают текущую ячейку, Tab/Shift+Tab — по
    ///    редактируемым ячейкам, Enter — вниз.
    ///
    /// Подключается через attached-свойство: behaviors:ExcelNavigationBehavior.IsEnabled="True".
    /// </summary>
    public static class ExcelNavigationBehavior
    {
        private sealed class GridState
        {
            public KeyEventHandler? KeyDownHandler;
            public MouseButtonEventHandler? DoubleClickHandler;
        }

        private static readonly ConditionalWeakTable<DataGrid, GridState> _states = new ConditionalWeakTable<DataGrid, GridState>();

        private static GridState State(DataGrid grid)
        {
            if (!_states.TryGetValue(grid, out var state))
            {
                state = new GridState();
                _states.Add(grid, state);
            }
            return state;
        }

        #region Attached property

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ExcelNavigationBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid grid) return;

            var state = State(grid);
            bool enabled = e.NewValue is true;

            if (enabled)
            {
                if (state.KeyDownHandler != null) return; // уже подключено

                state.KeyDownHandler = OnPreviewKeyDown;
                state.DoubleClickHandler = OnMouseDoubleClick;

                grid.AddHandler(UIElement.PreviewKeyDownEvent, state.KeyDownHandler, true);
                grid.AddHandler(Control.MouseDoubleClickEvent, state.DoubleClickHandler, true);
            }
            else
            {
                if (state.KeyDownHandler == null) return;

                grid.RemoveHandler(UIElement.PreviewKeyDownEvent, state.KeyDownHandler!);
                grid.RemoveHandler(Control.MouseDoubleClickEvent, state.DoubleClickHandler!);

                state.KeyDownHandler = null;
                state.DoubleClickHandler = null;
            }
        }

        #endregion

        /// <summary>Ставит фокус в первую редактируемую ячейку и входит в редактирование.</summary>
        public static void FocusFirstCell(DataGrid grid)
        {
            if (grid == null || grid.Items.Count == 0) return;
            var ordered = Ordered(grid);
            if (ordered.Count == 0) return;
            int col = ordered.FindIndex(c => !c.IsReadOnly);
            if (col < 0) col = 0;
            TrySetCell(grid, 0, col, enterEdit: true);
        }

        #region Event handlers

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var grid = (DataGrid)sender;
            var source = e.OriginalSource as DependencyObject;
            var cell = source as DataGridCell ?? FindAncestor<DataGridCell>(source);
            if (cell == null) return;

            bool editing = IsEditorFocused(cell);
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (e.Key == Key.Escape)
            {
                if (editing)
                {
                    CancelEdit(grid, cell);
                    e.Handled = true;
                }
                return;
            }

            if (e.Key == Key.F2)
            {
                if (!editing)
                {
                    StartEdit(grid, cell);
                    e.Handled = true;
                }
                return;
            }

            if (editing)
            {
                // В режиме редактирования стрелки обрабатывает сам редактор (курсор внутри текста).
                if (e.Key == Key.Enter)
                {
                    CommitAndMove(grid, cell, rowDelta: 1, colDelta: 0);
                    e.Handled = true;
                }
                else if (e.Key == Key.Tab)
                {
                    CommitAndMove(grid, cell, rowDelta: 0, colDelta: shift ? -1 : 1);
                    e.Handled = true;
                }
                return;
            }

            // Режим навигации.
            switch (e.Key)
            {
                case Key.Left:
                    MoveHorizontal(grid, cell, -1);
                    e.Handled = true;
                    break;
                case Key.Right:
                    MoveHorizontal(grid, cell, 1);
                    e.Handled = true;
                    break;
                case Key.Up:
                    MoveVertical(grid, cell, -1);
                    e.Handled = true;
                    break;
                case Key.Down:
                    MoveVertical(grid, cell, 1);
                    e.Handled = true;
                    break;
                case Key.Tab:
                    if (shift) MoveToPrevEditableCell(grid, cell);
                    else MoveToNextEditableCell(grid, cell);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    MoveVertical(grid, cell, 1);
                    e.Handled = true;
                    break;
            }
        }

        private static void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = (DataGrid)sender;
            var source = e.OriginalSource as DependencyObject;
            var cell = source as DataGridCell ?? FindAncestor<DataGridCell>(source);
            if (cell != null && !cell.IsReadOnly)
                StartEdit(grid, cell);
        }

        #endregion

        #region Edit / cancel

        /// <summary>
        /// Явный вход в редактирование (F2 / двойной клик).
        /// Для ячейки с постоянно видимым редактором просто фокусируем редактор
        /// (без ручного IsEditing). Для ячейки без видимого редактора (DataGridTextColumn)
        /// запускаем штатный DataGrid.BeginEdit().
        /// </summary>
        private static void StartEdit(DataGrid grid, DataGridCell cell)
        {
            if (cell == null || cell.IsReadOnly || cell.IsEditing) return;

            var editor = FindFocusableEditor(cell);
            if (editor != null)
            {
                FocusEditor(cell, editor);
                return;
            }

            grid.CurrentCell = new DataGridCellInfo(cell.DataContext, cell.Column);
            try { grid.BeginEdit(); }
            catch { }
        }

        /// <summary>Планирует перевод фокуса в видимый редактор ячейки (без IsEditing).</summary>
        private static void FocusCellEditor(DataGridCell cell)
        {
            if (cell == null || cell.IsReadOnly) return;
            cell.Dispatcher.BeginInvoke(new Action(() => FocusEditor(cell)), DispatcherPriority.Loaded);
        }

        private static void FocusEditor(DataGridCell cell)
        {
            var editor = FindFocusableEditor(cell);
            if (editor == null)
            {
                cell.Focus();
                return;
            }
            FocusEditor(cell, editor);
        }

        private static void FocusEditor(DataGridCell cell, Control editor)
        {
            editor.Focus();
            if (editor is TextBox textBox)
                textBox.SelectAll();
        }

        private static Control? FindFocusableEditor(DataGridCell cell)
            => FindDescendants<Control>(cell).FirstOrDefault(c => c.Focusable);

        private static bool IsEditorFocused(DataGridCell cell)
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            if (focused == null || ReferenceEquals(focused, cell)) return false;

            var ownerCell = focused as DataGridCell ?? FindAncestor<DataGridCell>(focused);
            return ReferenceEquals(ownerCell, cell) && focused is Control;
        }

        private static void CancelEdit(DataGrid grid, DataGridCell cell)
        {
            // Возвращаем содержимое редактора к значению источника (для привязок по умолчанию - LostFocus).
            foreach (var textBox in FindDescendants<TextBox>(cell))
                textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();

            try
            {
                grid.CancelEdit(DataGridEditingUnit.Cell);
                grid.CancelEdit(DataGridEditingUnit.Row);
            }
            catch { }

            // Возврат в режим навигации: фокус на ячейке (без ручного IsEditing).
            cell.Focus();
        }

        private static void CommitAndMove(DataGrid grid, DataGridCell cell, int rowDelta, int colDelta)
        {
            CommitCurrent(grid);
            MoveRelative(grid, cell, rowDelta, colDelta, enterEdit: true);
        }

        #endregion

        #region Navigation

        private static List<DataGridColumn> Ordered(DataGrid grid)
            => grid.Columns.OrderBy(c => c.DisplayIndex).ToList();

        private static void MoveHorizontal(DataGrid grid, DataGridCell cell, int delta)
            => MoveRelative(grid, cell, rowDelta: 0, colDelta: delta, enterEdit: false);

        private static void MoveVertical(DataGrid grid, DataGridCell cell, int delta)
            => MoveRelative(grid, cell, rowDelta: delta, colDelta: 0, enterEdit: false);

        private static void MoveRelative(DataGrid grid, DataGridCell cell, int rowDelta, int colDelta, bool enterEdit)
        {
            var ordered = Ordered(grid);
            int row = grid.Items.IndexOf(cell.DataContext);
            int col = ordered.IndexOf(cell.Column);
            if (row < 0 || col < 0 || ordered.Count == 0) return;

            int newRow = row;
            int newCol = col;

            if (colDelta != 0)
            {
                newCol = col + colDelta;
                if (newCol < 0) { newRow--; newCol = ordered.Count - 1; }
                else if (newCol >= ordered.Count) { newRow++; newCol = 0; }
            }

            if (rowDelta != 0)
                newRow = row + rowDelta;

            TrySetCell(grid, WrapRow(grid, newRow), WrapCol(ordered, newCol), enterEdit);
        }

        private static void MoveToNextEditableCell(DataGrid grid, DataGridCell cell)
        {
            var ordered = Ordered(grid);
            int row = grid.Items.IndexOf(cell.DataContext);
            int col = ordered.IndexOf(cell.Column);
            if (row < 0 || col < 0) return;

            for (int step = 1; step <= grid.Items.Count * ordered.Count; step++)
            {
                int flat = row * ordered.Count + col + step;
                int r = (flat / ordered.Count) % grid.Items.Count;
                int c = flat % ordered.Count;
                if (!ordered[c].IsReadOnly)
                {
                    TrySetCell(grid, r, c, enterEdit: false);
                    return;
                }
            }
        }

        private static void MoveToPrevEditableCell(DataGrid grid, DataGridCell cell)
        {
            var ordered = Ordered(grid);
            int row = grid.Items.IndexOf(cell.DataContext);
            int col = ordered.IndexOf(cell.Column);
            if (row < 0 || col < 0) return;

            int total = grid.Items.Count * ordered.Count;
            for (int step = 1; step <= total; step++)
            {
                int flat = (row * ordered.Count + col - step) % total;
                if (flat < 0) flat += total;
                int r = flat / ordered.Count;
                int c = flat % ordered.Count;
                if (!ordered[c].IsReadOnly)
                {
                    TrySetCell(grid, r, c, enterEdit: false);
                    return;
                }
            }
        }

        private static int WrapRow(DataGrid grid, int row)
        {
            if (grid.Items.Count == 0) return 0;
            if (row < 0) row = grid.Items.Count - 1;
            if (row >= grid.Items.Count) row = 0;
            return row;
        }

        private static int WrapCol(List<DataGridColumn> ordered, int col)
        {
            if (ordered.Count == 0) return 0;
            if (col < 0) col = ordered.Count - 1;
            if (col >= ordered.Count) col = 0;
            return col;
        }

        private static void TrySetCell(DataGrid grid, int rowIndex, int colIndex, bool enterEdit)
        {
            var ordered = Ordered(grid);
            if (ordered.Count == 0 || grid.Items.Count == 0) return;
            if (rowIndex < 0 || rowIndex >= grid.Items.Count || colIndex < 0 || colIndex >= ordered.Count) return;

            CommitCurrent(grid);

            var item = grid.Items[rowIndex];
            var column = ordered[colIndex];

            grid.SelectedCells.Clear();
            grid.CurrentCell = new DataGridCellInfo(item, column);
            grid.ScrollIntoView(item, column);

            grid.Dispatcher.BeginInvoke(new Action(() =>
            {
                var cell = GetCell(grid, item, column);
                if (cell == null)
                {
                    grid.Focus();
                    return;
                }

                cell.Focus();

                if (enterEdit && !cell.IsReadOnly)
                    FocusCellEditor(cell);
            }), DispatcherPriority.Loaded);
        }

        private static void CommitCurrent(DataGrid grid)
        {
            try
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }
        }

        #endregion

        #region Visual tree helpers

        private static DataGridCell? GetCell(DataGrid grid, object item, DataGridColumn column)
        {
            var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
            if (row == null)
            {
                grid.ScrollIntoView(item, column);
                grid.UpdateLayout();
                row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row == null) return null;
            }

            return FindDescendants<DataGridCell>(row).FirstOrDefault(c => c.Column == column);
        }

        private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
        {
            var current = node;
            while (current != null)
            {
                if (current is T match) return match;
                current = current is Visual || current is Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }
            return null;
        }

        private static IEnumerable<T> FindDescendants<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match) yield return match;
                foreach (var sub in FindDescendants<T>(child)) yield return sub;
            }
        }

        #endregion
    }
}
