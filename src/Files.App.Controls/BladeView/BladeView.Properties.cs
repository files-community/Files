// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Files.App.Controls
{
	/// <summary>
	/// A container that hosts <see cref="BladeItem"/> controls in a horizontal scrolling list
	/// Based on the Azure portal UI
	/// </summary>
	public partial class BladeView
	{
		/// <summary>
		/// Identifies the <see cref="ActiveBlades"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ActiveBladesProperty = DependencyProperty.Register(nameof(ActiveBlades), typeof(IList<BladeItem>), typeof(BladeView), new PropertyMetadata(null));

		/// <summary>
		/// Identifies the <see cref="BladeMode"/> attached property.
		/// </summary>
		public static readonly DependencyProperty BladeModeProperty = DependencyProperty.RegisterAttached(nameof(BladeMode), typeof(BladeMode), typeof(BladeView), new PropertyMetadata(BladeMode.Normal, OnBladeModeChanged));

		/// <summary>
		///  Identifies the <see cref="AutoCollapseCountThreshold"/> attached property.
		/// </summary>
		public static readonly DependencyProperty AutoCollapseCountThresholdProperty = DependencyProperty.RegisterAttached(nameof(AutoCollapseCountThreshold), typeof(int), typeof(BladeView), new PropertyMetadata(int.MaxValue, OnOpenBladesChanged));

		/// <summary>
		/// Gets or sets a collection of visible blades
		/// </summary>
		public IList<BladeItem> ActiveBlades
		{
			get { return (IList<BladeItem>)GetValue(ActiveBladesProperty); }
			set { SetValue(ActiveBladesProperty, value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether blade mode (ex: whether blades are full screen or not)
		/// </summary>
		public BladeMode BladeMode
		{
			get { return (BladeMode)GetValue(BladeModeProperty); }
			set { SetValue(BladeModeProperty, value); }
		}

		/// <summary>
		/// Gets or sets a value indicating what the overflow amount should be to start auto collapsing blade items
		/// </summary>
		/// <example>
		/// For example we put AutoCollapseCountThreshold = 2
		/// This means that each time a blade is added to the bladeview collection,
		/// we will validate the amount of added blades that have a title bar visible.
		/// If this number get's bigger than AutoCollapseCountThreshold, we will collapse all blades but the last one
		/// </example>
		/// <remarks>
		/// We don't touch blade items that have no title bar
		/// </remarks>
		public int AutoCollapseCountThreshold
		{
			get { return (int)GetValue(AutoCollapseCountThresholdProperty); }
			set { SetValue(AutoCollapseCountThresholdProperty, value); }
		}

		private static void OnBladeModeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			var bladeView = (BladeView)dependencyObject;

			if (bladeView.BladeMode == BladeMode.Fullscreen)
			{
				// Cache previous values of blade items properties (width & height)
				bladeView._cachedBladeItemSizes.Clear();

				if (bladeView.Items != null)
				{
					foreach (var item in bladeView.Items)
					{
						var bladeItem = bladeView.GetBladeItem(item);
						bladeView._cachedBladeItemSizes.Add(bladeItem, new Size(bladeItem.Width, bladeItem.Height));
					}
				}

				VisualStateManager.GoToState(bladeView, "FullScreen", false);
			}

			if (bladeView.BladeMode == BladeMode.Normal)
			{
				// Reset blade items properties & clear cache
				foreach (var kvBladeItemSize in bladeView._cachedBladeItemSizes)
				{
					kvBladeItemSize.Key.Width = kvBladeItemSize.Value.Width;
					kvBladeItemSize.Key.Height = kvBladeItemSize.Value.Height;
				}

				bladeView._cachedBladeItemSizes.Clear();

				VisualStateManager.GoToState(bladeView, "Normal", false);
			}

			// Execute change of blade item size
			bladeView.AdjustBladeItemSize();
		}

		private static void OnOpenBladesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			var bladeView = (BladeView)dependencyObject;
			bladeView.CycleBlades();
		}
	}
}
