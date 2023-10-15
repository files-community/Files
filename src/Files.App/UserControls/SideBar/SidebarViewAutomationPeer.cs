// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Files.App.UserControls.SideBar
{
	class SideBarViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
	{
		public bool CanSelectMultiple
			=> false;

		public bool IsSelectionRequired
			=> true;

		private new SideBarView Owner { get; init; }

		public SideBarViewAutomationPeer(SideBarView owner) : base(owner)
		{
			Owner = owner;
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Selection)
				return this;

			return base.GetPatternCore(patternInterface);
		}

		public IRawElementProviderSimple[] GetSelection()
		{
			if (Owner.SelectedItemContainer != null)
			{
				return new IRawElementProviderSimple[]
					{
						ProviderFromPeer(CreatePeerForElement(Owner.SelectedItemContainer))
					};
			}

			return Array.Empty<IRawElementProviderSimple>();
		}
	}
}
