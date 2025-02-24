// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CommunityToolkit.WinUI;
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
	/// Defines a framework element automation peer for the <see cref="BladeItem"/>.
	/// </summary>
	public partial class BladeItemAutomationPeer : FrameworkElementAutomationPeer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BladeItemAutomationPeer"/> class.
		/// </summary>
		/// <param name="owner">
		/// The <see cref="BladeItem" /> that is associated with this <see cref="T:Microsoft.UI.Xaml.Automation.Peers.BladeItemAutomationPeer" />.
		/// </param>
		public BladeItemAutomationPeer(BladeItem owner)
			: base(owner)
		{
		}

		private BladeItem OwnerBladeItem
		{
			get { return this.Owner as BladeItem; }
		}

		/// <summary>
		/// Gets the control type for the element that is associated with the UI Automation peer.
		/// </summary>
		/// <returns>The control type.</returns>
		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.ListItem;
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
		/// - Name of the owning BladeItem
		/// - BladeItem class name
		/// </returns>
		protected override string GetNameCore()
		{
			string name = AutomationProperties.GetName(this.OwnerBladeItem);
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}

			name = this.OwnerBladeItem.Name;
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}

			TextBlock textBlock = this.OwnerBladeItem.FindDescendant<TextBlock>();
			if (textBlock != null)
			{
				return textBlock.Text;
			}

			name = base.GetNameCore();
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}

			return string.Empty;
		}

		/// <summary>
		/// Returns the size of the set where the element that is associated with the automation peer is located.
		/// </summary>
		/// <returns>
		/// The size of the set.
		/// </returns>
		protected override int GetSizeOfSetCore()
		{
			int sizeOfSet = base.GetSizeOfSetCore();

			if (sizeOfSet != -1)
			{
				return sizeOfSet;
			}

			BladeItem owner = this.OwnerBladeItem;
			BladeView parent = owner.ParentBladeView;
			sizeOfSet = parent.Items.Count;

			return sizeOfSet;
		}

		/// <summary>
		/// Returns the ordinal position in the set for the element that is associated with the automation peer.
		/// </summary>
		/// <returns>
		/// The ordinal position in the set.
		/// </returns>
		protected override int GetPositionInSetCore()
		{
			int positionInSet = base.GetPositionInSetCore();

			if (positionInSet != -1)
			{
				return positionInSet;
			}

			BladeItem owner = this.OwnerBladeItem;
			BladeView parent = owner.ParentBladeView;
			positionInSet = parent.IndexFromContainer(owner);

			return positionInSet;
		}
	}
}
