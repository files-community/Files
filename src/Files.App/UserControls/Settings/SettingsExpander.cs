// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Files.App.UserControls.Settings
{
	[TemplatePart(Name = PART_ItemsRepeater, Type = typeof(ItemsRepeater))]
	public partial class SettingsExpander : Control
	{
		private const string PART_ItemsRepeater = "PART_ItemsRepeater";

		private ItemsRepeater? _itemsRepeater;

		/// <summary>
		/// The SettingsExpander is a collapsable control to host multiple SettingsCards.
		/// </summary>
		public SettingsExpander()
		{
			this.DefaultStyleKey = typeof(SettingsExpander);
			Items = new List<object>();
		}

		/// <inheritdoc />
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			RegisterAutomation();

			if (_itemsRepeater != null)
			{
				_itemsRepeater.ElementPrepared -= this.ItemsRepeater_ElementPrepared;
			}

			_itemsRepeater = GetTemplateChild(PART_ItemsRepeater) as ItemsRepeater;

			if (_itemsRepeater != null)
			{
				_itemsRepeater.ElementPrepared += this.ItemsRepeater_ElementPrepared;

				// Update it's source based on our current items properties.
				OnItemsConnectedPropertyChanged(this, null!); // Can't get it to accept type here? (DependencyPropertyChangedEventArgs)EventArgs.Empty
			}
		}

		private void RegisterAutomation()
		{
			if (Header is string headerString && headerString != string.Empty)
			{
				if (!string.IsNullOrEmpty(headerString) && string.IsNullOrEmpty(AutomationProperties.GetName(this)))
				{
					AutomationProperties.SetName(this, headerString);
				}
			}
		}

		/// <summary>
		/// Creates AutomationPeer
		/// </summary>
		/// <returns>An automation peer for <see cref="SettingsExpander"/>.</returns>
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new SettingsExpanderAutomationPeer(this);
		}

		private void OnIsExpandedChanged(bool oldValue, bool newValue)
		{
			var peer = FrameworkElementAutomationPeer.FromElement(this) as SettingsExpanderAutomationPeer;
			peer?.RaiseExpandedChangedEvent(newValue);
		}
	}
}
