// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Storage;

namespace Files.App.UserControls.StatusCenter
{
	public sealed partial class StatusCenter : UserControl
	{
		public StatusCenterViewModel ViewModel;

		const string WidthKey = "StatusCenterWidth";
		const string HeightKey = "StatusCenterHeight";

		public StatusCenter()
		{
			InitializeComponent();
			ViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

			Loaded += (s, e) =>
			{
				var (w, h) = LoadStatusCenterSize();
				if (w.HasValue && h.HasValue)
				{
					RootGrid.Width = Math.Max(w.Value, RootGrid.MinWidth);
					RootGrid.Height = Math.Max(h.Value, RootGrid.MinHeight);
				}

				ResizeGrip.DragDelta += (s1, e1) =>
				{
					double currentW = double.IsNaN(RootGrid.Width) ? RootGrid.ActualWidth : RootGrid.Width;
					double currentH = double.IsNaN(RootGrid.Height) ? RootGrid.ActualHeight : RootGrid.Height;

					double newW = Math.Max(currentW - e1.HorizontalChange, RootGrid.MinWidth);
					double newH = Math.Max(currentH + e1.VerticalChange, RootGrid.MinHeight);

					RootGrid.Width = newW;
					RootGrid.Height = newH;
				};

				ResizeGrip.DragCompleted += (s2, e2) =>
				{
					SaveStatusCenterSize(RootGrid.Width, RootGrid.Height);
				};
			};
		}

		private void CloseAllItemsButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.RemoveAllCompletedItems();
		}

		private void CloseItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is StatusCenterItem item)
				ViewModel.RemoveItem(item);
		}

		private void ExpandCollapseChevronItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is StatusCenterItem item)
			{
				var buttonAnimatedIcon = button.FindDescendant<AnimatedIcon>();

				if (buttonAnimatedIcon is not null)
					AnimatedIcon.SetState(buttonAnimatedIcon, item.IsExpanded ? "NormalOff" : "NormalOn");

				item.IsExpanded = !item.IsExpanded;
			}
		}

		void SaveStatusCenterSize(double width, double height)
		{
			var local = ApplicationData.Current.LocalSettings;
			local.Values[WidthKey] = width;
			local.Values[HeightKey] = height;
		}

		(double? width, double? height) LoadStatusCenterSize()
		{
			var local = ApplicationData.Current.LocalSettings;
			double? w = local.Values.TryGetValue(WidthKey, out var vw) && vw is double dw ? dw : null;
			double? h = local.Values.TryGetValue(HeightKey, out var vh) && vh is double dh ? dh : null;
			return (w, h);
		}
	}
}
