// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI;

namespace Files.App.Controls
{
	// Content
	[ContentProperty(Name = nameof(Modes))]
	// Template parts
	[TemplatePart(Name = "PART_ModesHostGrid", Type = typeof(Grid))]
	// Visual states
	[TemplateVisualState(Name = "Focused", GroupName = "FocusStates")]
	[TemplateVisualState(Name = "Unfocused", GroupName = "FocusStates")]
	public partial class Omnibar : Control
	{
		private const string ModesHostGrid = "PART_ModesHostGrid";

		private Grid? _modesHostGrid;
		private bool _isFocused;

		public Omnibar()
		{
			DefaultStyleKey = typeof(Omnibar);

			Modes ??= [];
		}

		protected override void OnApplyTemplate()
		{
			_modesHostGrid = GetTemplateChild(ModesHostGrid) as Grid
				?? throw new MissingFieldException($"Could not find {ModesHostGrid} in the given {nameof(Omnibar)}'s style.");

			if (Modes is null)
				return;

			// Populate the modes1
			foreach (var mode in Modes)
			{
				// Insert a divider
				if (_modesHostGrid.Children.Count is not 0)
				{
					var divider = new Rectangle()
					{
						Fill = (SolidColorBrush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
						Height = 20,
						Margin = new(2,0,2,0),
						Width = 1,
					};

					_modesHostGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
					Grid.SetColumn(divider, _modesHostGrid.Children.Count);
					_modesHostGrid.Children.Add(divider);
				}

				// Insert the mode
				_modesHostGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
				Grid.SetColumn(mode, _modesHostGrid.Children.Count);
				_modesHostGrid.Children.Add(mode);
				mode.Host = _modesHostGrid;
			}

			GotFocus += Omnibar_GotFocus;
			LostFocus += Omnibar_LostFocus;

			UpdateVisualStates();

			base.OnApplyTemplate();
		}

		// Private methods

		private void UpdateVisualStates()
		{
			VisualStateManager.GoToState(
				this,
				_isFocused ? "Focused" : "Normal",
				true);
		}

		// Events

		private void Omnibar_GotFocus(object sender, RoutedEventArgs e)
		{
			_isFocused = true;
			UpdateVisualStates();
		}

		private void Omnibar_LostFocus(object sender, RoutedEventArgs e)
		{
			_isFocused = false;
			UpdateVisualStates();
		}
	}
}
