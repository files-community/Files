// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Xaml.Automation.Peers;

namespace Files.App.Controls
{
	/// <summary>
	/// The Blade is used as a child in the BladeView
	/// </summary>
	[TemplatePart(Name = "CloseButton", Type = typeof(Button))]
	public partial class BladeItem : ContentControl
	{
		private Button _closeButton;
		/// <summary>
		/// Initializes a new instance of the <see cref="BladeItem"/> class.
		/// </summary>
		public BladeItem()
		{
			DefaultStyleKey = typeof(BladeItem);
		}

		/// <summary>
		/// Override default OnApplyTemplate to capture child controls
		/// </summary>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_closeButton = GetTemplateChild("CloseButton") as Button;

			if (_closeButton == null)
			{
				return;
			}

			_closeButton.Click -= CloseButton_Click;
			_closeButton.Click += CloseButton_Click;
		}
		/// <summary>
		/// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
		/// </summary>
		/// <returns>An automation peer for this <see cref="BladeItem"/>.</returns>
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new BladeItemAutomationPeer(this);
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			IsOpen = false;
		}
	}
}
