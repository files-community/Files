using Files.Interacts;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace Files.UserControls.Selection
{
    public class RectangleSelection_DataGrid : RectangleSelection
    {
        private DataGrid uiElement;
        private ScrollBar scrollBar;
        private SelectionChangedEventHandler selectionChanged;

        private Point originDragPoint;
        private Dictionary<object, System.Drawing.Rectangle> itemsPosition;
        private IList<DataGridRow> dataGridRows;
        private List<object> prevSelectedItems;

        public RectangleSelection_DataGrid(DataGrid uiElement, Rectangle selectionRectangle, SelectionChangedEventHandler selectionChanged = null)
        {
            this.uiElement = uiElement;
            this.selectionRectangle = selectionRectangle;
            this.selectionChanged = selectionChanged;
            itemsPosition = new Dictionary<object, System.Drawing.Rectangle>();
            dataGridRows = new List<DataGridRow>();
            InitEvents(null, null);
        }

        private void RectangleSelection_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (selectionState == SelectionState.Starting)
            {
                // Clear selected items once if the pointer is pressed and moved
                uiElement.SelectedItems.Clear();
                OnSelectionStarted();
                selectionState = SelectionState.Active;
            }
            var currentPoint = e.GetCurrentPoint(uiElement);
            if (currentPoint.Properties.IsLeftButtonPressed && scrollBar != null)
            {
                var verticalOffset = scrollBar.Value - 38; // Header height
                var originDragPointShifted = new Point(originDragPoint.X, originDragPoint.Y - verticalOffset); // Initial drag point relative to the topleft corner
                base.DrawRectangle(currentPoint, originDragPointShifted);
                // Selected area considering scrolled offset
                var rect = new System.Drawing.Rectangle((int)Canvas.GetLeft(selectionRectangle), (int)Math.Min(originDragPoint.Y, currentPoint.Position.Y + verticalOffset), (int)selectionRectangle.Width, (int)Math.Abs(originDragPoint.Y - (currentPoint.Position.Y + verticalOffset)));
                var dataGridRowsPosition = new Dictionary<DataGridRow, System.Drawing.Rectangle>();
                foreach (var row in dataGridRows)
                {
                    if (row.Visibility != Visibility.Visible)
                    {
                        continue; // Skip invalid/invisible rows
                    }

                    var gt = row.TransformToVisual(uiElement);
                    var itemStartPoint = gt.TransformPoint(new Point(0, verticalOffset)); // Get item position relative to the top of the list (considering scrolled offset)
                    var itemRect = new System.Drawing.Rectangle((int)itemStartPoint.X, (int)itemStartPoint.Y, (int)row.ActualWidth, (int)row.ActualHeight);
                    itemsPosition[row.DataContext] = itemRect; // Update item position
                    dataGridRowsPosition[row] = itemRect; // Update ui row position
                }
                foreach (var item in itemsPosition.ToList())
                {
                    try
                    {
                        // Update selected items
                        if (rect.IntersectsWith(item.Value))
                        {
                            // Selection rectangle intersects item, add to selected items
                            if (!uiElement.SelectedItems.Contains(item.Key))
                            {
                                uiElement.SelectedItems.Add(item.Key);
                            }
                        }
                        else
                        {
                            uiElement.SelectedItems.Remove(item.Key);
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
            Interaction.FindChildren<DataGridRow>(dataGridRows, uiElement); // Find visible/loaded rows
            prevSelectedItems = uiElement.SelectedItems.Cast<object>().ToList(); // Save current selected items
            originDragPoint = new Point(e.GetCurrentPoint(uiElement).Position.X, e.GetCurrentPoint(uiElement).Position.Y); // Initial drag point relative to the topleft corner
            var verticalOffset = (scrollBar?.Value ?? 0) - 38; // Header height
            originDragPoint.Y += verticalOffset; // Initial drag point relative to the top of the list (considering scrolled offset)
            if (!e.GetCurrentPoint(uiElement).Properties.IsLeftButtonPressed || e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
            {
                // Trigger only on left click, do not trigger with touch
                return;
            }
            var clickedRow = Interaction.FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (clickedRow == null)
            {
                // If user click outside, reset selection
                uiElement.SelectedItems.Clear();
            }
            else if (uiElement.SelectedItems.Contains(clickedRow.DataContext))
            {
                // If the item under the pointer is selected do not trigger selection rectangle
                return;
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
                scrollBar = Interaction.FindChild<ScrollBar>(uiElement);
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