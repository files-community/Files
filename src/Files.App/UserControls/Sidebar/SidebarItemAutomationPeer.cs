using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Files.App.UserControls.Sidebar
{
	public class SidebarItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, IExpandCollapseProvider, ISelectionItemProvider
	{
		private new SidebarItem Owner { get; init; }
		public ExpandCollapseState ExpandCollapseState
		{
			get
			{
				if (Owner.HasChildren)
					return Owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
				return ExpandCollapseState.LeafNode;
			}
		}

		public bool IsSelected => Owner.IsSelected;

		public IRawElementProviderSimple SelectionContainer => ProviderFromPeer(CreatePeerForElement(Owner.Owner));

		public SidebarItemAutomationPeer(SidebarItem owner) : base(owner)
		{
			this.Owner = owner;
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.ListItem;
		}

		protected override string GetNameCore()
		{
			return Owner.Item?.Text ?? "";
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Invoke || patternInterface == PatternInterface.SelectionItem)
			{
				return this;
			}
			else if (patternInterface == PatternInterface.ExpandCollapse)
			{
				if (Owner.CollapseEnabled)
				{
					return this;
				}
			}
			return base.GetPatternCore(patternInterface);
		}

		public void Collapse()
		{
			if (Owner.CollapseEnabled)
			{
				Owner.IsExpanded = false;
			}
		}

		public void Expand()
		{

			if (Owner.CollapseEnabled)
			{
				Owner.IsExpanded = true;
			}
		}

		public void Invoke()
		{
			Owner.RaiseItemInvoked();
		}

		public void AddToSelection()
		{
			Owner.Select();
		}

		public void RemoveFromSelection()
		{
			// Intentionally left blank
		}

		public void Select()
		{
			Owner.Select();
		}

		protected override int GetSizeOfSetCore()
		{
			return GetOwnerCollection().Count;
		}

		protected override int GetPositionInSetCore()
		{
			return GetOwnerCollection().IndexOf((INavigationControlItem)Owner.DataContext) + 1;
		}

		private IList<INavigationControlItem> GetOwnerCollection()
		{
			if (Owner.FindAscendant<SidebarItem>() is SidebarItem parent)
			{
				return parent.Item!.ChildItems!;
			}
			return Owner.Owner.ViewModel.SidebarItems;
		}
	}
}
