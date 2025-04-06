// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Controls
{
	/// <summary>
	/// Defines a framework element automation peer for the <see cref="BladeView"/> control.
	/// </summary>
	public partial class BladeViewAutomationPeer : ItemsControlAutomationPeer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BladeViewAutomationPeer"/> class.
		/// </summary>
		/// <param name="owner">
		/// The <see cref="BladeView" /> that is associated with this <see cref="T:Microsoft.UI.Xaml.Automation.Peers.BladeViewAutomationPeer" />.
		/// </param>
		public BladeViewAutomationPeer(BladeView owner)
			: base(owner)
		{
		}

		private BladeView OwningBladeView
		{
			get
			{
				return Owner as BladeView;
			}
		}

		/// <summary>
		/// Gets the control type for the element that is associated with the UI Automation peer.
		/// </summary>
		/// <returns>The control type.</returns>
		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.List;
		}

		/// <summary>
		/// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType,
		/// differentiates the control represented by this AutomationPeer.
		/// </summary>
		/// <returns>The string that contains the name.</returns>
		protected override string GetClassNameCore()
		{
			return Owner.GetType().Name;
		}

		/// <summary>
		/// Called by GetName.
		/// </summary>
		/// <returns>
		/// Returns the first of these that is not null or empty:
		/// - Value returned by the base implementation
		/// - Name of the owning BladeView
		/// - BladeView class name
		/// </returns>
		protected override string GetNameCore()
		{
			string name = AutomationProperties.GetName(this.OwningBladeView);
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}

			name = this.OwningBladeView.Name;
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}

			name = base.GetNameCore();
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}

			return string.Empty;
		}

		/// <summary>
		/// Gets the collection of elements that are represented in the UI Automation tree as immediate
		/// child elements of the automation peer.
		/// </summary>
		/// <returns>The children elements.</returns>
		protected override IList<AutomationPeer> GetChildrenCore()
		{
			BladeView owner = OwningBladeView;

			ItemCollection items = owner.Items;
			if (items.Count <= 0)
			{
				return null;
			}

			List<AutomationPeer> peers = new List<AutomationPeer>(items.Count);
			for (int i = 0; i < items.Count; i++)
			{
				if (owner.ContainerFromIndex(i) is BladeItem element)
				{
					peers.Add(FromElement(element) ?? CreatePeerForElement(element));
				}
			}

			return peers;
		}
	}
}
