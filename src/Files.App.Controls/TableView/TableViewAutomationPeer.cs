// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Files.App.Controls;

public sealed partial class TableViewAutomationPeer : FrameworkElementAutomationPeer
{
	public TableViewAutomationPeer(TableView owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(TableView);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;
}

public sealed partial class TableViewColumnAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
	public TableViewColumnAutomationPeer(TableViewColumn owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		return patternInterface is PatternInterface.Invoke ? this : base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(TableViewColumn);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.HeaderItem;

	protected override string GetNameCore()
	{
		var name = AutomationProperties.GetName(Owner);
		return !string.IsNullOrEmpty(name) ? name : ((TableViewColumn)Owner).Header ?? string.Empty;
	}

	public void Invoke()
	{
		((TableViewColumn)Owner).RequestSort();
	}
}
