using Files.Interacts;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace Files.UserControls
{
    public class RectangleSelection
    {
        private FrameworkElement uiElement;
        private Rectangle selectionRectangle;
        private SelectionChangedEventHandler selectionChanged;

        private Point originDragPoint;
        private Dictionary<object, System.Drawing.Rectangle> itemsPosition;
        private IList<DataGridRow> dataGridRows;

        public RectangleSelection(FrameworkElement uiElement, Rectangle selectionRectangle, SelectionChangedEventHandler selectionChanged = null)
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
            var currentPoint = e.GetCurrentPoint(uiElement);
            if (currentPoint.Properties.IsLeftButtonPressed)
            {
                var verticalOffset = scrollViewer?.VerticalOffset ?? scrollBar.Value;
                var originDragPointShifted = new Point(originDragPoint.X, originDragPoint.Y - verticalOffset);
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
                var rect = new System.Drawing.Rectangle((int)Canvas.GetLeft(selectionRectangle), (int)Math.Min(originDragPoint.Y, currentPoint.Position.Y + verticalOffset), (int)selectionRectangle.Width, (int)Math.Abs(originDragPoint.Y - (currentPoint.Position.Y + verticalOffset)));
                if (uiElement is ListViewBase)
                {
                    var listViewBase = uiElement as ListViewBase;
                    foreach (var item in listViewBase.Items.Except(itemsPosition.Keys))
                    {
                        var listViewItem = (FrameworkElement)listViewBase.ContainerFromItem(item);
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
                            if (!listViewBase.SelectedItems.Contains(item.Key))
                            {
                                listViewBase.SelectedItems.Add(item.Key);
                            }
                        }
                        else
                        {
                            listViewBase.SelectedItems.Remove(item.Key);
                        }
                    }
                }
                else //if (uiElement is DataGrid)
                {
                    var dataGrid = uiElement as DataGrid;
                    foreach (var row in dataGridRows)
                    {
                        var gt = row.TransformToVisual(uiElement);
                        var itemStartPoint = gt.TransformPoint(new Point(0, verticalOffset));
                        var itemRect = new System.Drawing.Rectangle((int)itemStartPoint.X, (int)itemStartPoint.Y, (int)row.ActualWidth, (int)row.ActualHeight);
                        itemsPosition[row.DataContext] = itemRect;
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
                }
                if (currentPoint.Position.Y > uiElement.ActualHeight - 20)
                {
                    var scrollIncrement = Math.Min(currentPoint.Position.Y - (uiElement.ActualHeight - 20), 40);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ChangeView(null, verticalOffset + scrollIncrement, null, false);
                    }
                    else // if (scrollBar != null)
                    {
                        scrollBar.Value = verticalOffset + scrollIncrement;
                    }
                }
                else if (currentPoint.Position.Y < 20)
                {
                    var scrollIncrement = Math.Min(20 - currentPoint.Position.Y, 40);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ChangeView(null, verticalOffset - scrollIncrement, null, false);
                    }
                    else // if (scrollBar != null)
                    {
                        scrollBar.Value = verticalOffset - scrollIncrement;
                    }
                }
            }
        }

        private void RectangleSelection_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            itemsPosition.Clear();
            if (uiElement is DataGrid)
            {
                dataGridRows.Clear();
                Interaction.FindChildren<DataGridRow>(dataGridRows, uiElement);
            }
            originDragPoint = new Point(e.GetCurrentPoint(uiElement).Position.X, e.GetCurrentPoint(uiElement).Position.Y);
            var verticalOffset = scrollViewer?.VerticalOffset ?? scrollBar.Value;
            originDragPoint.Y = originDragPoint.Y + verticalOffset;
            uiElement.PointerMoved -= RectangleSelection_PointerMoved;
            uiElement.PointerMoved += RectangleSelection_PointerMoved;
            if (uiElement is ListViewBase)
            {
                var listViewBase = uiElement as ListViewBase;
                if (selectionChanged != null)
                {
                    listViewBase.SelectionChanged -= selectionChanged;
                }
                listViewBase.CapturePointer(e.Pointer);
                listViewBase.SelectedItems.Clear();
            }
            else //if (uiElement is DataGrid)
            {
                var dataGrid = uiElement as DataGrid;
                if (selectionChanged != null)
                {
                    dataGrid.SelectionChanged -= selectionChanged;
                }
                dataGrid.CapturePointer(e.Pointer);
                dataGrid.SelectedItems.Clear();
            }
        }

        private void RectangleSelection_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Canvas.SetLeft(selectionRectangle, 0);
            Canvas.SetTop(selectionRectangle, 0);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
            uiElement.PointerMoved -= RectangleSelection_PointerMoved;
            uiElement.ReleasePointerCapture(e.Pointer);
            if (uiElement is ListViewBase)
            {
                var listViewBase = uiElement as ListViewBase;
                if (selectionChanged != null)
                {
                    listViewBase.SelectionChanged -= selectionChanged;
                    listViewBase.SelectionChanged += selectionChanged;
                    selectionChanged(sender, null);
                }
            }
            else //if (uiElement is DataGrid)
            {
                var dataGrid = uiElement as DataGrid;
                if (selectionChanged != null)
                {
                    dataGrid.SelectionChanged -= selectionChanged;
                    dataGrid.SelectionChanged += selectionChanged;
                    selectionChanged(sender, null);
                }
            }
        }

        private ScrollViewer scrollViewer;
        private ScrollBar scrollBar;

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
                if (uiElement is DataGrid)
                {
                    (uiElement as DataGrid).LoadingRow += RectangleSelection_LoadingRow;
                    (uiElement as DataGrid).UnloadingRow += RectangleSelection_UnloadingRow;
                    scrollBar = Interaction.FindChild<ScrollBar>(uiElement);
                }
                else
                {
                    scrollViewer = Interaction.FindChild<ScrollViewer>(uiElement);
                }
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
}
