// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;

namespace Files.App.Controls
{
	/// <summary>
	/// The Blade is used as a child in the BladeView
	/// </summary>
	[TemplatePart(Name = "CloseButton", Type = typeof(Button))]
	[TemplatePart(Name = "EnlargeButton", Type = typeof(Button))]
	public partial class BladeItem : Expander
	{
		private Button _closeButton;
		private Button _enlargeButton;
		private double _normalModeWidth;
		private bool _loaded = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="BladeItem"/> class.
		/// </summary>
		public BladeItem()
		{
			DefaultStyleKey = typeof(BladeItem);

			SizeChanged += OnSizeChanged;
			Expanding += OnExpanding;
			Collapsed += OnCollapsed;
		}

		/// <summary>
		/// Override default OnApplyTemplate to capture child controls
		/// </summary>
		protected override void OnApplyTemplate()
		{
			_loaded = true;
			base.OnApplyTemplate();

			_closeButton = GetTemplateChild("CloseButton") as Button;
			_enlargeButton = GetTemplateChild("EnlargeButton") as Button;

			if (_closeButton == null)
			{
				return;
			}

			_closeButton.Click -= CloseButton_Click;
			_closeButton.Click += CloseButton_Click;

			if (_enlargeButton == null)
			{
				return;
			}

			_enlargeButton.Click -= EnlargeButton_Click;
			_enlargeButton.Click += EnlargeButton_Click;
		}

		/// <inheritdoc/>
		private void OnExpanding(Expander sender, ExpanderExpandingEventArgs args)
		{
			if (_loaded)
			{
				Width = _normalModeWidth;
				VisualStateManager.GoToState(this, "Expanded", true);
				var name = "WCT_BladeView_ExpandButton_Collapsed".GetLocalized("CommunityToolkit.WinUI.UI.Controls.Layout/Resources");
				if (_enlargeButton != null)
				{
					AutomationProperties.SetName(_enlargeButton, name);
				}
			}
		}

		/// <inheritdoc/>
		private void OnCollapsed(Expander sender, ExpanderCollapsedEventArgs args)
		{
			if (_loaded)
			{
				Width = double.NaN;
				VisualStateManager.GoToState(this, "Collapsed", true);
				var name = "WCT_BladeView_ExpandButton_Expanded".GetLocalized("CommunityToolkit.WinUI.UI.Controls.Layout/Resources");
				if (_enlargeButton != null)
				{
					AutomationProperties.SetName(_enlargeButton, name);
				}
			}
		}

		/// <summary>
		/// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
		/// </summary>
		/// <returns>An automation peer for this <see cref="BladeItem"/>.</returns>
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new BladeItemAutomationPeer(this);
		}

		private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
		{
			if (IsExpanded)
			{
				_normalModeWidth = Width;
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			IsOpen = false;
		}

		private void EnlargeButton_Click(object sender, RoutedEventArgs e)
		{
			IsExpanded = !IsExpanded;
		}
	}
}
