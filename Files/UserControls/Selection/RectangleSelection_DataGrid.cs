using Files.Helpers.XamlHelpers;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace Files.UserControls.Selection
{
    public class RectangleSelection_DataGrid : RectangleSelection
    {
        private readonly DataGrid uiElement;
        private readonly MethodInfo uiElementSetCurrentCellCore;

        private ScrollBar scrollBar;
        private SelectionChangedEventHandler selectionChanged;

        private Point originDragPoint;
        private Dictionary<object, System.Drawing.Rectangle> itemsPosition;
        private IList<DataGridRow> dataGridRows;
        private List<object> prevSelectedItems;
        private ItemSelectionStrategy selectionStrategy;

        public RectangleSelection_DataGrid(DataGrid uiElement, Rectangle selectionRectangle, SelectionChangedEventHandler selectionChanged = null)
        {
            this.uiElement = uiElement;
            this.selectionRectangle = selectionRectangle;
            this.selectionChanged = selectionChanged;

            uiElementSetCurrentCellCore = typeof(DataGrid).GetMethod("SetCurrentCellCore", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(int), typeof(int) }, null);

            itemsPosition = new Dictionary<object, System.Drawing.Rectangle>();
            dataGridRows = new List<DataGridRow>();
            InitEvents(null, null);
        }

        private void RectangleSelection_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (scrollBar == null)
            {
                return;
            }

            var currentPoint = e.GetCurrentPoint(uiElement);
            var verticalOffset = scrollBar.Value - uiElement.ColumnHeaderHeight;
            if (selectionState == SelectionState.Starting)
            {
                if (!HasMovedMinimalDelta(originDragPoint.X, originDragPoint.Y - verticalOffset, currentPoint.Position.X, currentPoint.Position.Y))
                {
                    return;
                }

                if (uiElement.CurrentColumn != null)
                {
                    uiElement.CancelEdit();
                }
                selectionStrategy.StartSelection();
                OnSelectionStarted();
                selectionState = SelectionState.Active;
            }

            if (currentPoint.Properties.IsLeftButtonPressed)
            {
                var originDragPointShifted = new Point(originDragPoint.X, originDragPoint.Y - verticalOffset); // Initial drag point relative to the topleft corner
                base.DrawRectangle(currentPoint, originDragPointShifted, uiElement);
                // Selected area considering scrolled offset
                var rect = new System.Drawing.Rectangle((int)Canvas.GetLeft(selectionRectangle), (int)Math.Min(originDragPoint.Y, currentPoint.Position.Y + verticalOffset), (int)selectionRectangle.Width, (int)Math.Abs(originDragPoint.Y - (currentPoint.Position.Y + verticalOffset)));
                var dataGridRowsPosition = new Dictionary<DataGridRow, System.Drawing.Rectangle>();
                double actualWidth = -1;
                foreach (var row in dataGridRows)
                {
                    if (row.Visibility != Visibility.Visible)
                    {
                        continue; // Skip invalid/invisible rows
                    }

                    if (actualWidth < 0)
                    {
                        var temp = new List<DataGridCell>();
                        DependencyObjectHelpers.FindChildren<DataGridCell>(temp, row); // Find cells inside row
                        actualWidth = temp.Sum(x => x.ActualWidth); // row.ActualWidth reports incorrect width
                    }

                    var gt = row.TransformToVisual(uiElement);
                    var itemStartPoint = gt.TransformPoint(new Point(0, verticalOffset)); // Get item position relative to the top of the list (considering scrolled offset)
                    var itemRect = new System.Drawing.Rectangle((int)itemStartPoint.X, (int)itemStartPoint.Y, (int)actualWidth, (int)row.ActualHeight);
                    itemsPosition[row.DataContext] = itemRect; // Update item position
                    dataGridRowsPosition[row] = itemRect; // Update ui row position
                }

                foreach (var item in itemsPosition.ToList())
                {
                    try
                    {
                        if (rect.IntersectsWith(item.Value))
                        {
                            selectionStrategy.HandleIntersectionWithItem(item.Key);
                        }
                        else
                        {
                            selectionStrategy.HandleNoIntersectionWithItem(item.Key);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Item is not present in the ItemsSource
                        itemsPosition.Remove(item);
                    }
                }
                if (currentPoint.Position.Y > uiElement.ActualHeight - 20)
                {
                    // Scroll down the list if pointer is at the bottom
                    // Check if there is a loaded row outside the viewport
                    var item = dataGridRowsPosition.OrderBy(x => x.Value.Y).SkipWhile(x => x.Value.Y <= verticalOffset + uiElement.ActualHeight).Select(x => x.Key).FirstOrDefault();
                    if (item == null)
                    {
                        if (dataGridRowsPosition.Any())
                        {
                            // Last loaded item is fully visible, ge thet next one from bound item source
                            var index = dataGridRowsPosition.OrderBy(x => x.Value.Y).Last().Key.GetIndex();
                            var source = (System.Collections.IList)uiElement.ItemsSource;
                            uiElement.ScrollIntoView(source[Math.Min(Math.Max(index + 1, 0), source.Count - 1)], null);
                        }
                    }
                    else
                    {
                        uiElement.ScrollIntoView(item.DataContext, null);
                    }
                }
                else if (currentPoint.Position.Y < 20)
                {
                    // Scroll up the list if pointer is at the top
                    // Check if there is a loaded row outside the viewport
                    var item = dataGridRowsPosition.OrderBy(x => x.Value.Y).TakeWhile(x => x.Value.Y + x.Value.Height <= scrollBar.Value).Select(x => x.Key).LastOrDefault();
                    if (item == null)
                    {
                        if (dataGridRowsPosition.Any())
                        {
                            // First loaded item is fully visible, ge thet previous one from bound item source
                            var index = dataGridRowsPosition.OrderBy(x => x.Value.Y).First().Key.GetIndex();
                            var source = (System.Collections.IList)uiElement.ItemsSource;
                            uiElement.ScrollIntoView(source[Math.Min(Math.Max(index - 1, 0), source.Count - 1)], null);
                        }
                    }
                    else
                    {
                        uiElement.ScrollIntoView(item.DataContext, null);
                    }
                }
            }
        }

        private void RectangleSelection_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            itemsPosition.Clear();
            dataGridRows.Clear();
            DependencyObjectHelpers.FindChildren<DataGridRow>(dataGridRows, uiElement); // Find visible/loaded rows
            prevSelectedItems = uiElement.SelectedItems.Cast<object>().ToList(); // Save current selected items
            originDragPoint = new Point(e.GetCurrentPoint(uiElement).Position.X, e.GetCurrentPoint(uiElement).Position.Y); // Initial drag point relative to the topleft corner
            var verticalOffset = (scrollBar?.Value ?? 0) - uiElement.ColumnHeaderHeight;
            originDragPoint.Y += verticalOffset; // Initial drag point relative to the top of the list (considering scrolled offset)
            if (!e.GetCurrentPoint(uiElement).Properties.IsLeftButtonPressed || e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
            {
                // Trigger only on left click, do not trigger with touch
                return;
            }

            var clickedRow = DependencyObjectHelpers.FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (clickedRow != null && uiElement.SelectedItems.Contains(clickedRow.DataContext))
            {
                // If the item under the pointer is selected do not trigger selection rectangle
                return;
            }

            var selectedItems = new GenericItemsCollection<object>(uiElement.SelectedItems);
            selectionStrategy = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control) ?
                    new InvertPreviousItemSelectionStrategy(selectedItems, prevSelectedItems) :
                    e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift) ?
                        (ItemSelectionStrategy)new ExtendPreviousItemSelectionStrategy(selectedItems, prevSelectedItems) :
                        new IgnorePreviousItemSelectionStrategy(selectedItems);

            if (clickedRow == null)
            {
                // If user click outside, reset selection
                if (uiElement.CurrentColumn != null)
                {
                    uiElement.CancelEdit();
                }
                DeselectGridCell();
                selectionStrategy.HandleNoItemSelected();
            }

            uiElement.PointerMoved -= RectangleSelection_PointerMoved;
            uiElement.PointerMoved += RectangleSelection_PointerMoved;
            if (selectionChanged != null)
            {
                // Unsunscribe from SelectionChanged event for performance
                uiElement.SelectionChanged -= selectionChanged;
            }
            uiElement.CapturePointer(e.Pointer);
            selectionState = SelectionState.Starting;
        }

        private void DeselectGridCell()
        {
            uiElementSetCurrentCellCore.Invoke(uiElement, new object[] { -1, -1 });
        }

        private void RectangleSelection_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Canvas.SetLeft(selectionRectangle, 0);
            Canvas.SetTop(selectionRectangle, 0);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
            uiElement.PointerMoved -= RectangleSelection_PointerMoved;
            uiElement.ReleasePointerCapture(e.Pointer);
            if (selectionChanged != null)
            {
                // Restore SelectionChanged event
                uiElement.SelectionChanged -= selectionChanged;
                uiElement.SelectionChanged += selectionChanged;
                if (prevSelectedItems == null || !uiElement.SelectedItems.Cast<object>().ToList().SequenceEqual(prevSelectedItems))
                {
                    // Trigger SelectionChanged event if the selection has changed
                    selectionChanged(sender, null);
                }
            }
            if (selectionState == SelectionState.Active)
            {
                OnSelectionEnded();
            }

            selectionStrategy = null;
            selectionState = SelectionState.Inactive;
        }

        private void InitEvents(object sender, RoutedEventArgs e)
        {
            if (!uiElement.IsLoaded)
            {
                uiElement.Loaded += InitEvents;
            }
            else
            {
                uiElement.Loaded -= InitEvents;
                uiElement.PointerPressed += RectangleSelection_PointerPressed;
                uiElement.PointerReleased += RectangleSelection_PointerReleased;
                uiElement.PointerCaptureLost += RectangleSelection_PointerReleased;
                uiElement.PointerCanceled += RectangleSelection_PointerReleased;
                uiElement.LoadingRow += RectangleSelection_LoadingRow;
                uiElement.UnloadingRow += RectangleSelection_UnloadingRow;
                scrollBar = DependencyObjectHelpers.FindChild<ScrollBar>(uiElement);
            }
        }

        private void RectangleSelection_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            dataGridRows.Add(e.Row);
        }

        private void RectangleSelection_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            dataGridRows.Remove(e.Row);
        }
    }
}