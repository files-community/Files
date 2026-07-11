// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Controls
{
	public partial class TableViewCheckBoxColumn : TableViewBindableColumn
	{
		public TableViewCheckBoxColumn()
		{
			DefaultStyleKey = typeof(TableViewCheckBoxColumn);
		}

		public override FrameworkElement GenerateElement(object dataItem)
		{
			var checkBox = new CheckBox()
			{
				DataContext = dataItem,
				Style = ElementStyle,
				IsThreeState = IsThreeState,
				IsHitTestVisible = !IsReadOnly,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
			};

			ApplyBindings(checkBox);
			return checkBox;
		}

		protected internal override bool UpdateElement(FrameworkElement element, object dataItem)
		{
			if (element is not CheckBox checkBox)
				return false;

			checkBox.DataContext = dataItem;
			checkBox.Style = ElementStyle;
			checkBox.IsThreeState = IsThreeState;
			checkBox.IsHitTestVisible = !IsReadOnly;
			return true;
		}

		private void ApplyBindings(CheckBox checkBox)
		{
			if (!string.IsNullOrEmpty(Binding))
			{
				checkBox.SetBinding(
					ToggleButton.IsCheckedProperty,
					CreateBinding(Binding, IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay));
			}

			if (!string.IsNullOrEmpty(IsEnabledBinding))
				checkBox.SetBinding(IsEnabledProperty, CreateBinding(IsEnabledBinding, BindingMode.OneWay));
			else if (IsReadOnly)
				checkBox.IsEnabled = false;

			if (!string.IsNullOrEmpty(VisibilityBinding))
			{
				checkBox.SetBinding(
					VisibilityProperty,
					new Binding()
					{
						Path = new PropertyPath(VisibilityBinding),
						Mode = BindingMode.OneWay,
						Converter = VisibilityConverter,
					});
			}
		}

		private static Binding CreateBinding(string path, BindingMode mode)
		{
			return new()
			{
				Path = new PropertyPath(path),
				Mode = mode,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
			};
		}
	}
}
