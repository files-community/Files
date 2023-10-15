// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Files.App.UserControls.SideBar
{
	public class SideBarItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, IExpandCollapseProvider, ISelectionItemProvider
	{
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

		private new SideBarItem Owner { get; init; }

		public SideBarItemAutomationPeer(SideBarItem owner) : base(owner)
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
			return GetOwnerCollection().IndexOf(Owner.DataContext) + 1;
		}

		private IList GetOwnerCollection()
		{
			if (Owner.FindAscendant<SideBarItem>() is SideBarItem parent && parent.Item?.Children is IList list)
			{
				return list;
			}
			if (Owner?.Owner is not null && Owner.Owner.ViewModel.SidebarItems is IList items)
			{
				return items;
			}
			return new List<object>();
		}
	}
}
