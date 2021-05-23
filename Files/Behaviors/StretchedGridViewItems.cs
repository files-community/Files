using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Behaviors
{
    // Adapted from https://gist.github.com/WamWooWam/6b19b8355e3e3a8d605ac015c67f6093
    public class StretchedGridViewItems
    {
        /// <summary>
        /// This property specifies the minium width for child items in an items wrap grid panel. 
        /// Setting this property o an non zero value will enable dynamic sizing of items so that
        /// when items are wrapped the items control is always filled out horizontally
        /// i.e. the width of items are increased to fill the empty space.
        /// </summary>
        public static readonly DependencyProperty MinItemWidthProperty =
            DependencyProperty.RegisterAttached("MinItemWidth", typeof(double),
            typeof(StretchedGridViewItems), new PropertyMetadata(0.0d, OnMinItemWidthChanged));

        /// <summary>
        /// Only applicable when MinItemWidth is non zero. Typically the logic behind 
        /// MinItemWidth will only trigger if the number of items is more than or equal to 
        /// what a single row will accomodate. This property specifies that the layout logic
        /// is also performed when there are less items than what a single row will accomodate.
        /// </summary>
        public static readonly DependencyProperty FillBeforeWrapProperty =
           DependencyProperty.RegisterAttached("FillBeforeWrap", typeof(bool),
           typeof(StretchedGridViewItems), new PropertyMetadata(false));

        /// <summary>
        /// Returns the value of the FillBeforeWrap
        /// </summary>
        /// <param name="obj">The dependency-object whichs value should be returned</param> 
        /// <returns>The value of the property</returns>
        public static bool GetFillBeforeWrap(DependencyObject obj)
        {
            return (bool)obj.GetValue(FillBeforeWrapProperty);
        }

        /// <summary>
        /// Sets the value of the FillBeforeWrap
        /// </summary>
        /// <param name="obj">The dependency-object whichs value should be set</param>
        /// <param name="value">The value which should be assigned to the property.</param>
        public static void SetFillBeforeWrap(DependencyObject obj, bool value)
        {
            obj.SetValue(FillBeforeWrapProperty, value);
        }

        /// <summary>
        /// Returns the value of the MinItemWidthProperty
        /// </summary>
        /// <param name="obj">The dependency-object whichs value should be returned</param> 
        /// <returns>The value of the property</returns>
        public static double GetMinItemWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(MinItemWidthProperty);
        }

        /// <summary>
        /// Sets the value of the MinItemWidthProperty
        /// </summary>
        /// <param name="obj">The dependency-object whichs value should be set</param>
        /// <param name="value">The value which should be assigned to the property.</param>
        public static void SetMinItemWidth(DependencyObject obj, double value)
        {
            obj.SetValue(MinItemWidthProperty, value);
        }

        private static void OnMinItemWidthChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            if (s is ListViewBase f)
            {
                f.SizeChanged -= OnListViewSizeChanged;

                if (((double)e.NewValue) > 0)
                {
                    f.SizeChanged += OnListViewSizeChanged;
                }
            }
        }

        public static void ResizeItems(DependencyObject obj)
        {
            OnListViewSizeChanged(obj, null);
        }

        private static void OnListViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Unbox the sender.
            var itemsControl = sender as ListViewBase;

            //If the items panel is a wrap grid.
            if (itemsControl.ItemsPanelRoot is ItemsWrapGrid itemsPanel)
            {
                //Get total size
                var total = (e?.NewSize.Width ?? itemsControl.ActualWidth) - (itemsPanel.Margin.Left + itemsPanel.Margin.Right + itemsControl.Padding.Left + itemsControl.Padding.Right);

                //Minimum item size.
                var itemMinSize = Math.Min(total, (double)itemsControl.GetValue(MinItemWidthProperty));

                //How many items can be fit whole.
                var canBeFit = Math.Floor(total / itemMinSize);

                //I could add logic that if the total items 
                //are less then the number of items that 
                //would fit then devide the total size by
                //the number of items rather than the number
                //of items that would actually fit.
                if ((bool)itemsControl.GetValue(FillBeforeWrapProperty) &&
                    itemsControl.Items.Count > 0 &&
                    itemsControl.Items.Count < canBeFit)
                {
                    canBeFit = itemsControl.Items.Count;
                }

                // Set the items Panel item width appropriately.
                // Note you will need your container to stretch
                // along with the items panel or it will look 
                // strange. 
                // <GridView.ItemContainerStyle>
                //     <Style TargetType="GridViewItem">
                //         <Setter Property="HorizontalContentAlignment" Value="Stretch" />
                //         <Setter Property="HorizontalAlignment" Value="Stretch" />
                //     </Style>
                // </GridView.ItemContainerStyle>
                itemsPanel.ItemWidth = total / canBeFit;
            }
        }
    }
}
