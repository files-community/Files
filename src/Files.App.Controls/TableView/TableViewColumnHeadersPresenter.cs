// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public sealed class TableViewColumnHeadersPresenter : Control
	{
		private const string TemplatePartName_ColumnsPanel = "PART_ColumnsPanel";

		internal event EventHandler? TemplateApplied;

		internal ReorderableItemsControl? ColumnsItemsControl { get; private set; }

		public TableViewColumnHeadersPresenter()
		{
			DefaultStyleKey = typeof(TableViewColumnHeadersPresenter);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			ColumnsItemsControl = GetTemplateChild(TemplatePartName_ColumnsPanel) as ReorderableItemsControl
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ColumnsPanel} in the given {nameof(TableViewColumnHeadersPresenter)}'s style.");
			TemplateApplied?.Invoke(this, EventArgs.Empty);
		}
	}
}
