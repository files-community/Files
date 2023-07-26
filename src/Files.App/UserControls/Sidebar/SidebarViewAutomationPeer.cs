using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.UserControls.Sidebar
{
	class SidebarViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
	{
		private new SidebarView Owner { get; init; }

		public bool CanSelectMultiple => false;

		public bool IsSelectionRequired => true;

		public SidebarViewAutomationPeer(SidebarView owner) : base(owner)
		{
			Owner = owner;
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Selection)
			{
				return this;
			}
			return base.GetPatternCore(patternInterface);
		}

		public IRawElementProviderSimple[] GetSelection()
		{
			if (Owner.SelectedItemContainer != null)
				return new IRawElementProviderSimple[]
				{
				ProviderFromPeer(CreatePeerForElement(Owner.SelectedItemContainer))
				};
			return new IRawElementProviderSimple[] { };
		}
	}
}
