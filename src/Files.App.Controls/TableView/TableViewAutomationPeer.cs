// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Files.App.Controls;

public sealed partial class TableViewAutomationPeer : FrameworkElementAutomationPeer, IGridProvider
{
	public TableViewAutomationPeer(TableView owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(TableView);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		return patternInterface is PatternInterface.Grid ? this : base.GetPatternCore(patternInterface);
	}

	public int ColumnCount => ((TableView)Owner).AutomationColumnCount;

	public int RowCount => ((TableView)Owner).AutomationRowCount;

	public IRawElementProviderSimple? GetItem(int row, int column)
	{
		var cell = ((TableView)Owner).GetOrRealizeCell(row, column);
		var peer = cell is null ? null : CreatePeerForElement(cell);
		return peer is null ? null : ProviderFromPeer(peer);
	}
}

public sealed partial class TableViewCellAutomationPeer : FrameworkElementAutomationPeer, IGridItemProvider, IValueProvider
{
	public TableViewCellAutomationPeer(TableViewCell owner) : base(owner)
	{
	}

	private TableViewCell Cell => (TableViewCell)Owner;

	private TableView? TableView => Cell.Column?.GetOwner();

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		return patternInterface is PatternInterface.GridItem or PatternInterface.Value
			? this
			: base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(TableViewCell);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;

	protected override string GetNameCore()
	{
		var header = Cell.Column?.Header;
		var value = Cell.GetAutomationValue();
		return string.IsNullOrEmpty(header) ? value : $"{header}, {value}";
	}

	public int Column => Cell.Column is null ? -1 : TableView?.GetColumnIndex(Cell.Column) ?? -1;

	public int ColumnSpan => 1;

	public IRawElementProviderSimple? ContainingGrid
	{
		get
		{
			var peer = TableView is null ? null : CreatePeerForElement(TableView);
			return peer is null ? null : ProviderFromPeer(peer);
		}
	}

	public int Row => TableView?.GetRowIndex(Cell) ?? -1;

	public int RowSpan => 1;

	public bool IsReadOnly => Cell.Column?.IsEffectivelyReadOnly is not false;

	public string Value => Cell.GetAutomationValue();

	public void SetValue(string value)
	{
		if (IsReadOnly || !Cell.SetAutomationValue(value))
			throw new InvalidOperationException("The cell value could not be updated.");
	}
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
