// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Files.App.UserControls
{
	public class SettingsExpanderAutomationPeer : FrameworkElementAutomationPeer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SettingsExpander"/> class.
		/// </summary>
		/// <param name="owner">SettingsExpander</param>
		public SettingsExpanderAutomationPeer(SettingsExpander owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Gets the control type for the element that is associated with the UI Automation peer.
		/// </summary>
		/// <returns>The control type.</returns>
		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Group;
		}

		/// <summary>
		/// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType,
		/// differentiates the control represented by this AutomationPeer.
		/// </summary>
		/// <returns>The string that contains the name.</returns>
		protected override string GetClassNameCore()
		{
			string classNameCore = Owner.GetType().Name;
#if DEBUG_AUTOMATION
            System.Diagnostics.Debug.WriteLine("SettingsCardAutomationPeer.GetClassNameCore returns " + classNameCore);
#endif
			return classNameCore;
		}

		/// <summary>
		/// Raises the property changed event for this AutomationPeer for the provided identifier.
		/// Narrator does not announce this due to: https://github.com/microsoft/microsoft-ui-xaml/issues/3469
		/// </summary>
		/// <param name="newValue">New Expanded state</param>
		public void RaiseExpandedChangedEvent(bool newValue)
		{
			ExpandCollapseState newState = (newValue == true) ?
			  ExpandCollapseState.Expanded :
			  ExpandCollapseState.Collapsed;

			ExpandCollapseState oldState = (newState == ExpandCollapseState.Expanded) ?
			  ExpandCollapseState.Collapsed :
			  ExpandCollapseState.Expanded;

#if !HAS_UNO
			RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, oldState, newState);
#endif
		}
	}
}
