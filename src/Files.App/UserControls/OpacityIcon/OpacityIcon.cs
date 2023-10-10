// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public class OpacityIcon : Control
	{
		internal const string NormalState = "Normal";
		internal const string SelectedState = "Selected";
		internal const string DisabledState = "Disabled";

		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.Register(
				nameof(IsSelected),
				typeof(bool),
				typeof(OpacityIcon),
				new PropertyMetadata(null, (d, e) => ((OpacityIcon)d).OnIsEnabledChanged(d, e)));

		public bool IsSelected
		{
			get => (bool)GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public OpacityIcon()
		{
			this.DefaultStyleKey = typeof(OpacityIcon);
		}

		/// <inheritdoc />
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			EnableInteraction();

			IsEnabledChanged -= OnIsEnabledChanged;
			IsEnabledChanged += OnIsEnabledChanged;

			if (!IsEnabled)
				VisualStateManager.GoToState(this, DisabledState, false);
			else if (IsEnabled && IsSelected)
				VisualStateManager.GoToState(this, SelectedState, true);
		}

		private void EnableInteraction()
		{
			DisableInteraction();
		}

		private void DisableInteraction()
		{
		}

		private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (!IsEnabled)
				VisualStateManager.GoToState(this, DisabledState, true);
			else if (IsSelected)
				VisualStateManager.GoToState(this, SelectedState, true);
			else
				VisualStateManager.GoToState(this, NormalState, true);
		}
	}
}
