// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Automation.Peers;

namespace Files.App.UserControls.Settings
{
	public class SettingsCardAutomationPeer : FrameworkElementAutomationPeer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SettingsCard"/> class.
		/// </summary>
		/// <param name="owner">SettingsCard</param>
		public SettingsCardAutomationPeer(SettingsCard owner) : base(owner)
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
			return classNameCore;
		}
	}
}
