using Files.Interacts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace Files.UserControls
{
    public class RectangleSelection
    {
        private ListViewBase listViewBase;
        private Rectangle selectionRectangle;
        private SelectionChangedEventHandler selectionChanged;

        private Point originDragPoint;
        private Dictionary<object, System.Drawing.Rectangle> itemsPosition;

        public RectangleSelection(ListViewBase listViewBase, Rectangle selectionRectangle, SelectionChangedEventHandler selectionChanged = null)
        {
            this.listViewBase = listViewBase;
            this.selectionRectangle = selectionRectangle;
            this.selectionChanged = selectionChanged;
            this.InitEvents();
        }

        private void ListViewBase_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var listViewBase = sender as ListViewBase;
            var currentPoint = e.GetCurrentPoint(listViewBase);
            if (currentPoint.Properties.IsLeftButtonPressed && scrollViewer != null)
            {
                var originDragPointShifted = new Point(originDragPoint.X, originDragPoint.Y - scrollViewer.VerticalOffset);
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
                var rect = new System.Drawing.Rectangle((int)Canvas.GetLeft(selectionRectangle), (int)Math.Min(originDragPoint.Y, currentPoint.Position.Y + scrollViewer.VerticalOffset), (int)selectionRectangle.Width, (int)Math.Abs(originDragPoint.Y - (currentPoint.Position.Y + scrollViewer.VerticalOffset)));
                foreach (var item in listViewBase.Items.Except(itemsPosition.Keys))
                {
                    var gridViewItem = (FrameworkElement)listViewBase.ContainerFromItem(item);
                    if (gridViewItem == null) continue;
                    var gt = gridViewItem.TransformToVisual(listViewBase);
                    var itemStartPoint = gt.TransformPoint(new Point(0, scrollViewer.VerticalOffset));
                    var itemRect = new System.Drawing.Rectangle((int)itemStartPoint.X, (int)itemStartPoint.Y, (int)gridViewItem.ActualWidth, (int)gridViewItem.ActualHeight);
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
                if (currentPoint.Position.Y > listViewBase.ActualHeight - 20)
                {
                    var scrollIncrement = Math.Min(currentPoint.Position.Y - (listViewBase.ActualHeight - 20), 40);
                    scrollViewer.ChangeView(null, scrollViewer.VerticalOffset + scrollIncrement, null, false);
                }
                else if (currentPoint.Position.Y < 20)
                {
                    var scrollIncrement = Math.Min(20 - currentPoint.Position.Y, 40);
                    scrollViewer.ChangeView(null, scrollViewer.VerticalOffset - scrollIncrement, null, false);
                }
            }
        }

        private void ListViewBase_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var listViewBase = sender as ListViewBase;
            itemsPosition = new Dictionary<object, System.Drawing.Rectangle>();
            originDragPoint = new Point(e.GetCurrentPoint(listViewBase).Position.X, e.GetCurrentPoint(listViewBase).Position.Y);
            if (scrollViewer != null)
            {
                originDragPoint.Y = originDragPoint.Y + scrollViewer.VerticalOffset;
            }
            listViewBase.PointerMoved -= ListViewBase_PointerMoved;
            listViewBase.PointerMoved += ListViewBase_PointerMoved;
            if (selectionChanged != null)
            {
                listViewBase.SelectionChanged -= selectionChanged;
            }
            listViewBase.CapturePointer(e.Pointer);
            listViewBase.SelectedItems.Clear();
        }

        private void ListViewBase_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var listViewBase = sender as ListViewBase;
            Canvas.SetLeft(selectionRectangle, 0);
            Canvas.SetTop(selectionRectangle, 0);
            selectionRectangle.Width = 0;
            selectionRectangle.Height = 0;
            listViewBase.PointerMoved -= ListViewBase_PointerMoved;
            listViewBase.ReleasePointerCapture(e.Pointer);
            if (selectionChanged != null)
            {
                listViewBase.SelectionChanged -= selectionChanged;
                listViewBase.SelectionChanged += selectionChanged;
                selectionChanged(sender, null);
            }
        }

        private ScrollViewer scrollViewer;

        private void InitEvents()
        {
            if (!listViewBase.IsLoaded)
            {
                listViewBase.Loaded += (s, e) => InitEvents();
            }
            else
            {
                scrollViewer = Interaction.FindChild<ScrollViewer>(listViewBase);
                listViewBase.PointerPressed += ListViewBase_PointerPressed;
                listViewBase.PointerReleased += ListViewBase_PointerReleased;
                listViewBase.PointerCaptureLost += ListViewBase_PointerReleased;
                listViewBase.PointerCanceled += ListViewBase_PointerReleased;
            }
        }
    }
}
