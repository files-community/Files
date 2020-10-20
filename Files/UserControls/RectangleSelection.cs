using Files.Interacts;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace Files.UserControls
{
    public class RectangleSelection
    {
        protected Rectangle selectionRectangle;

        protected RectangleSelection() { }

        public static RectangleSelection Create(UIElement uiElement, Rectangle selectionRectangle, SelectionChangedEventHandler selectionChanged = null)
        {
            if (uiElement is ListViewBase)
            {
                return new RectangleSelection_ListViewBase(uiElement as ListViewBase, selectionRectangle, selectionChanged);
            }
            else if (uiElement is DataGrid)
            {
                return new RectangleSelection_DataGrid(uiElement as DataGrid, selectionRectangle, selectionChanged);
            }
            else
            {
                throw new ArgumentException("uiElement must derive from ListViewBase or DataGrid");
            }
        }

        protected void DrawRectangle(PointerPoint currentPoint, Point originDragPointShifted)
        {
            if (currentPoint.Position.X >= originDragPointShifted.X)
            {
                if (currentPoint.Position.Y <= originDragPointShifted.Y)
                {
                    Canvas.SetLeft(selectionRectangle, Math.Max(0, originDragPointShifted.X));
                    Canvas.SetTop(selectionRectangle, Math.Max(0, currentPoint.Position.Y));
                    selectionRectangle.Width = Math.Max(0, currentPoint.Position.X - Math.Max(0, originDragPointShifted.X));
                    selectionRectangle.Height = Math.Max(0, originDragPointShifted.Y - Math.Max(0, currentPoint.Position.Y));
                }
                else
                {
                    Canvas.SetLeft(selectionRectangle, Math.Max(0, originDragPointShifted.X));
                    Canvas.SetTop(selectionRectangle, Math.Max(0, originDragPointShifted.Y));
                    selectionRectangle.Width = Math.Max(0, currentPoint.Position.X - Math.Max(0, originDragPointShifted.X));
                    selectionRectangle.Height = Math.Max(0, currentPoint.Position.Y - Math.Max(0, originDragPointShifted.Y));
                }
            }
            else
            {
                if (currentPoint.Position.Y <= originDragPointShifted.Y)
                {
                    Canvas.SetLeft(selectionRectangle, Math.Max(0, currentPoint.Position.X));
                    Canvas.SetTop(selectionRectangle, Math.Max(0, currentPoint.Position.Y));
                    selectionRectangle.Width = Math.Max(0, originDragPointShifted.X - Math.Max(0, currentPoint.Position.X));
                    selectionRectangle.Height = Math.Max(0, originDragPointShifted.Y - Math.Max(0, currentPoint.Position.Y));
                }
                else
                {
                    Canvas.SetLeft(selectionRectangle, Math.Max(0, currentPoint.Position.X));
                    Canvas.SetTop(selectionRectangle, Math.Max(0, originDragPointShifted.Y));
                    selectionRectangle.Width = Math.Max(0, originDragPointShifted.X - Math.Max(0, currentPoint.Position.X));
                    selectionRectangle.Height = Math.Max(0, currentPoint.Position.Y - Math.Max(0, originDragPointShifted.Y));
                }
            }
        }
    }

    public class RectangleSelection_DataGrid : RectangleSelection
    {
        private DataGrid uiElement;
        private ScrollBar scrollBar;
        private SelectionChangedEventHandler selectionChanged;

        private Point originDragPoint;
        private Dictionary<object, System.Drawing.Rectangle> itemsPosition;
        private IList<DataGridRow> dataGridRows;

        private bool _hasSelectionStarted;
        private List<object> _prevSelectedItems;

        public RectangleSelection_DataGrid(DataGrid uiElement, Rectangle selectionRectangle, SelectionChangedEventHandler selectionChanged = null)
        {
            this.uiElement = uiElement;
            this.selectionRectangle = selectionRectangle;
            this.selectionChanged = selectionChanged;
            this.itemsPosition = new Dictionary<object, System.Drawing.Rectangle>();
            this.dataGridRows = new List<DataGridRow>();
            this.InitEvents();
        }

        private void RectangleSelection_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_hasSelectionStarted)
            {
                uiElement.SelectedItems.Clear();
                _hasSelectionStarted = false;
            }
            var currentPoint = e.GetCurrentPoint(uiElement);
            if (currentPoint.Properties.IsLeftButtonPressed && scrollBar != null)
            {
                var verticalOffset = scrollBar.Value - 38; // Magic number (header height? to be checked)
                var originDragPointShifted = new Point(originDragPoint.X, originDragPoint.Y - verticalOffset);
                base.DrawRectangle(currentPoint, originDragPointShifted);
                var rect = new System.Drawing.Rectangle((int)Canvas.GetLeft(selectionRectangle), (int)Math.Min(originDragPoint.Y, currentPoint.Position.Y + verticalOffset), (int)selectionRectangle.Width, (int)Math.Abs(originDragPoint.Y - (currentPoint.Position.Y + verticalOffset)));
                var dataGrid = uiElement as DataGrid;
                var dataGridRowsPosition = new Dictionary<DataGridRow, System.Drawing.Rectangle>();
                foreach (var row in dataGridRows)
                {
                    var gt = row.TransformToVisual(uiElement);
                    var itemStartPoint = gt.TransformPoint(new Point(0, verticalOffset));
                    var itemRect = new System.Drawing.Rectangle((int)itemStartPoint.X, (int)itemStartPoint.Y, (int)row.ActualWidth, (int)row.ActualHeight);
                    if (row.Visibility == Visibility.Visible)
                    {
                        itemsPosition[row.DataContext] = itemRect;
                        dataGridRowsPosition[row] = itemRect;
                    }
                }
                foreach (var item in itemsPosition)
                {
                    if (rect.IntersectsWith(item.Value))
                    {
                        if (!dataGrid.SelectedItems.Contains(item.Key))
                        {
                            dataGrid.SelectedItems.Add(item.Key);
                        }
                    }
                    else
                    {
                        dataGrid.SelectedItems.Remove(item.Key);
                    }
                }
                if (currentPoint.Position.Y > uiElement.ActualHeight - 20)
                {
                    var item = dataGridRowsPosition.OrderBy(x => x.Value.Y).SkipWhile(x => x.Value.Y <= verticalOffset + uiElement.ActualHeight).Select(x => x.Key).FirstOrDefault();
                    if (item == null)
                    {
                        var index = dataGridRowsPosition.OrderBy(x => x.Value.Y).Last().Key.GetIndex();
                        var source = (System.Collections.IList)(uiElement as DataGrid).ItemsSource;
                        (uiElement as DataGrid).ScrollIntoView(source[Math.Min(Math.Max(index + 1, 0), source.Count - 1)], null);
                    }
                    else
                    {
                        (uiElement as DataGrid).ScrollIntoView(item.DataContext, null);
                    }
                }
                else if (currentPoint.Position.Y < 20)
                {
                    var item = dataGridRowsPosition.OrderBy(x => x.Value.Y).TakeWhile(x => x.Value.Y + x.Value.Height <= scrollBar.Value).Select(x => x.Key).LastOrDefault();
                    if (item == null)
                    {
                        var index = dataGridRowsPosition.OrderBy(x => x.Value.Y).First().Key.GetIndex();
                        var source = (System.Collections.IList)(uiElement as DataGrid).ItemsSource;
                        (uiElement as DataGrid).ScrollIntoView(source[Math.Min(Math.Max(index - 1, 0), source.Count - 1)], null);
                    }
                    else
                    {
                        (uiElement as DataGrid).ScrollIntoView(item.DataContext, null);
                    }
                }
            }
        }

        private void RectangleSelection_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            itemsPosition.Clear();
            dataGridRows.Clear();
            Interaction.FindChildren<DataGridRow>(dataGridRows, uiElement);
            originDragPoint = new Point(e.GetCurrentPoint(uiElement).Position.X, e.GetCurrentPoint(uiElement).Position.Y);
            var verticalOffset = (scrollBar?.Value ?? 0) - 38; // Magic number (header height? to be checked)
            originDragPoint.Y = originDragPoint.Y + verticalOffset;
            uiElement.PointerMoved -= RectangleSelection_PointerMoved;
            uiElement.PointerMoved += RectangleSelection_PointerMoved;
            var dataGrid = uiElement as DataGrid;
            if (selectionChanged != null)
            {
                dataGrid.SelectionChanged -= selectionChanged;
            }
            dataGrid.CapturePointer(e.Pointer);
            _hasSelectionStarted = true;
            _prevSelectedItems = uiElement.SelectedItems.Cast<object>().ToList();
        }

        private void RectangleSelection_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Canvas.SetLeft(selectionRectangle, 0);
            Canvas.SetTop(selectionRectangle, 0);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
            uiElement.PointerMoved -= RectangleSelection_PointerMoved;
            uiElement.ReleasePointerCapture(e.Pointer);
            var dataGrid = uiElement as DataGrid;
            if (selectionChanged != null)
            {
                dataGrid.SelectionChanged -= selectionChanged;
                dataGrid.SelectionChanged += selectionChanged;
                if (_prevSelectedItems == null || !uiElement.SelectedItems.Cast<object>().ToList().SequenceEqual(_prevSelectedItems))
                {
                    selectionChanged(sender, null);
                }
            }
            _hasSelectionStarted = false;
        }

        private void InitEvents()
        {
            if (!uiElement.IsLoaded)
            {
                uiElement.Loaded += (s, e) => InitEvents();
            }
            else
            {
                uiElement.PointerPressed += RectangleSelection_PointerPressed;
                uiElement.PointerReleased += RectangleSelection_PointerReleased;
                uiElement.PointerCaptureLost += RectangleSelection_PointerReleased;
                uiElement.PointerCanceled += RectangleSelection_PointerReleased;
                (uiElement as DataGrid).LoadingRow += RectangleSelection_LoadingRow;
                (uiElement as DataGrid).UnloadingRow += RectangleSelection_UnloadingRow;
                scrollBar = Interaction.FindChild<ScrollBar>(uiElement);
            }
        }

        private void RectangleSelection_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            this.dataGridRows.Add(e.Row);
        }

        private void RectangleSelection_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            this.dataGridRows.Remove(e.Row);
        }
    }

    public class RectangleSelection_ListViewBase : RectangleSelection
    {
        private ListViewBase uiElement;
        private ScrollViewer scrollViewer;
        private SelectionChangedEventHandler selectionChanged;

        private Point originDragPoint;
        private Dictionary<object, System.Drawing.Rectangle> itemsPosition;

        public RectangleSelection_ListViewBase(ListViewBase uiElement, Rectangle selectionRectangle, SelectionChangedEventHandler selectionChanged = null)
        {
            this.uiElement = uiElement;
            this.selectionRectangle = selectionRectangle;
            this.selectionChanged = selectionChanged;
            this.itemsPosition = new Dictionary<object, System.Drawing.Rectangle>();
            this.InitEvents();
        }

        private void RectangleSelection_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(uiElement);
            if (currentPoint.Properties.IsLeftButtonPressed && scrollViewer != null)
            {
                var verticalOffset = scrollViewer.VerticalOffset;
                var originDragPointShifted = new Point(originDragPoint.X, originDragPoint.Y - verticalOffset);
                base.DrawRectangle(currentPoint, originDragPointShifted);
                var rect = new System.Drawing.Rectangle((int)Canvas.GetLeft(selectionRectangle), (int)Math.Min(originDragPoint.Y, currentPoint.Position.Y + verticalOffset), (int)selectionRectangle.Width, (int)Math.Abs(originDragPoint.Y - (currentPoint.Position.Y + verticalOffset)));
                foreach (var item in uiElement.Items.Except(itemsPosition.Keys))
                {
                    var listViewItem = (FrameworkElement)uiElement.ContainerFromItem(item);
                    if (listViewItem == null) continue;
                    var gt = listViewItem.TransformToVisual(uiElement);
                    var itemStartPoint = gt.TransformPoint(new Point(0, verticalOffset));
                    var itemRect = new System.Drawing.Rectangle((int)itemStartPoint.X, (int)itemStartPoint.Y, (int)listViewItem.ActualWidth, (int)listViewItem.ActualHeight);
                    itemsPosition[item] = itemRect;
                }
                foreach (var item in itemsPosition)
                {
                    if (rect.IntersectsWith(item.Value))
                    {
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
                if (currentPoint.Position.Y > uiElement.ActualHeight - 20)
                {
                    var scrollIncrement = Math.Min(currentPoint.Position.Y - (uiElement.ActualHeight - 20), 40);
                    scrollViewer.ChangeView(null, verticalOffset + scrollIncrement, null, false);
                }
                else if (currentPoint.Position.Y < 20)
                {
                    var scrollIncrement = Math.Min(20 - currentPoint.Position.Y, 40);
                    scrollViewer.ChangeView(null, verticalOffset - scrollIncrement, null, false);
                }
            }
        }

        private void RectangleSelection_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            itemsPosition.Clear();
            originDragPoint = new Point(e.GetCurrentPoint(uiElement).Position.X, e.GetCurrentPoint(uiElement).Position.Y);
            var verticalOffset = scrollViewer?.VerticalOffset ?? 0;
            originDragPoint.Y = originDragPoint.Y + verticalOffset;
            uiElement.PointerMoved -= RectangleSelection_PointerMoved;
            uiElement.PointerMoved += RectangleSelection_PointerMoved;
            if (selectionChanged != null)
            {
                uiElement.SelectionChanged -= selectionChanged;
            }
            uiElement.CapturePointer(e.Pointer);
            uiElement.SelectedItems.Clear();
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
                uiElement.SelectionChanged -= selectionChanged;
                uiElement.SelectionChanged += selectionChanged;
                selectionChanged(sender, null);
            }
        }

        private void InitEvents()
        {
            if (!uiElement.IsLoaded)
            {
                uiElement.Loaded += (s, e) => InitEvents();
            }
            else
            {
                uiElement.PointerPressed += RectangleSelection_PointerPressed;
                uiElement.PointerReleased += RectangleSelection_PointerReleased;
                uiElement.PointerCaptureLost += RectangleSelection_PointerReleased;
                uiElement.PointerCanceled += RectangleSelection_PointerReleased;
                scrollViewer = Interaction.FindChild<ScrollViewer>(uiElement);
            }
        }
    }
}
