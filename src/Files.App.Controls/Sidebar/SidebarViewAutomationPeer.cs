// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Files.App.Controls
{
	/// <summary>
	/// Automation peer for <see cref="SidebarView"/>.
	/// </summary>
	public sealed partial class SidebarViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
	{
		public bool CanSelectMultiple => false;
		public bool IsSelectionRequired => true;

		private new SidebarView Owner { get; init; }

		public SidebarViewAutomationPeer(SidebarView owner) : base(owner)
		{
			Owner = owner;
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			return patternInterface is PatternInterface.Selection
				? this
				: base.GetPatternCore(patternInterface);
		}

		public IRawElementProviderSimple[] GetSelection()
		{
			return Owner.SelectedItemContainer is null
				? []
				: [ProviderFromPeer(CreatePeerForElement(Owner.SelectedItemContainer))];
		}
	}
}
