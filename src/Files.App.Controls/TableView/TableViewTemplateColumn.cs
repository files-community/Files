// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Automation;

namespace Files.App.Controls
{
	public partial class TableViewTemplateColumn : TableViewColumn
	{
		public static readonly DependencyProperty CellTemplateProperty =
			DependencyProperty.Register(
				nameof(CellTemplate),
				typeof(DataTemplate),
				typeof(TableViewTemplateColumn),
				new PropertyMetadata(null, OnCellTemplatePropertyChanged));

		public static readonly DependencyProperty CellTemplateSelectorProperty =
			DependencyProperty.Register(
				nameof(CellTemplateSelector),
				typeof(DataTemplateSelector),
				typeof(TableViewTemplateColumn),
				new PropertyMetadata(null, OnCellTemplatePropertyChanged));

		public static readonly DependencyProperty CellEditingTemplateProperty =
			DependencyProperty.Register(
				nameof(CellEditingTemplate),
				typeof(DataTemplate),
				typeof(TableViewTemplateColumn),
				new PropertyMetadata(null, OnCellTemplatePropertyChanged));

		public static readonly DependencyProperty CellEditingTemplateSelectorProperty =
			DependencyProperty.Register(
				nameof(CellEditingTemplateSelector),
				typeof(DataTemplateSelector),
				typeof(TableViewTemplateColumn),
				new PropertyMetadata(null, OnCellTemplatePropertyChanged));

		public DataTemplate? CellTemplate
		{
			get => (DataTemplate?)GetValue(CellTemplateProperty);
			set => SetValue(CellTemplateProperty, value);
		}

		public DataTemplateSelector? CellTemplateSelector
		{
			get => (DataTemplateSelector?)GetValue(CellTemplateSelectorProperty);
			set => SetValue(CellTemplateSelectorProperty, value);
		}

		public DataTemplate? CellEditingTemplate
		{
			get => (DataTemplate?)GetValue(CellEditingTemplateProperty);
			set => SetValue(CellEditingTemplateProperty, value);
		}

		public DataTemplateSelector? CellEditingTemplateSelector
		{
			get => (DataTemplateSelector?)GetValue(CellEditingTemplateSelectorProperty);
			set => SetValue(CellEditingTemplateSelectorProperty, value);
		}

		public TableViewTemplateColumn()
		{
			DefaultStyleKey = typeof(TableViewTemplateColumn);
		}

		public override FrameworkElement GenerateElement(object dataItem)
		{
			var presenter = CreatePresenter(dataItem, ResolveTemplate(dataItem, false));
			UpdateAutomationName(presenter, dataItem);
			return presenter;
		}

		public override FrameworkElement GenerateEditingElement(object dataItem)
		{
			var context = new TableViewTemplateCellEditingContext(dataItem, GetPropertyValue<object?>(dataItem));
			return CreatePresenter(context, ResolveTemplate(dataItem, true));
		}

		protected internal override bool UpdateElement(FrameworkElement element, object dataItem)
		{
			if (element is not ContentPresenter presenter ||
				presenter.Content is TableViewTemplateCellEditingContext)
			{
				return false;
			}

			presenter.Content = dataItem;
			presenter.ContentTemplate = ResolveTemplate(dataItem, false);
			UpdateAutomationName(presenter, dataItem);
			return true;
		}

		protected internal override bool CanEdit(object dataItem)
		{
			return !string.IsNullOrEmpty(Binding) &&
				(CellEditingTemplate is not null || CellEditingTemplateSelector is not null) &&
				dataItem is ITableViewCellValueProvider and ITableViewCellValueEditor;
		}

		protected internal override void PrepareCellForEdit(TableViewCell cell, FrameworkElement editingElement)
		{
			if (editingElement is not ContentPresenter presenter)
				return;

			TableViewCellEditingBehavior.Prepare(presenter);
		}

		protected internal override TableViewCellEditResult CommitCellEdit(TableViewCell cell)
		{
			if (cell.EditingElement is not ContentPresenter
				{
					Content: TableViewTemplateCellEditingContext context,
				} presenter ||
				cell.Data is null)
			{
				return TableViewCellEditResult.Failure();
			}

			var result = SetPropertyValue(cell.Data, context.Value);
			if (result.Succeeded)
			{
				TableViewCellEditingBehavior.Unhook(presenter);
			}
			else
			{
				TableViewCellEditingBehavior.Refocus(presenter);
			}

			return result;
		}

		protected internal override void CancelCellEdit(TableViewCell cell)
		{
			TableViewCellEditingBehavior.Unhook(cell.EditingElement);
		}

		private static ContentPresenter CreatePresenter(object content, DataTemplate template)
		{
			return new()
			{
				Content = content,
				ContentTemplate = template,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Stretch,
			};
		}

		private DataTemplate ResolveTemplate(object dataItem, bool isEditing)
		{
			var selector = isEditing ? CellEditingTemplateSelector : CellTemplateSelector;
			var template = selector?.SelectTemplate(dataItem, this) ??
				(isEditing ? CellEditingTemplate : CellTemplate);

			return template ?? throw new InvalidOperationException(
				$"{(isEditing ? nameof(CellEditingTemplate) : nameof(CellTemplate))} must resolve to a {nameof(DataTemplate)}.");
		}

		private void UpdateAutomationName(ContentPresenter presenter, object dataItem)
		{
			if (!string.IsNullOrEmpty(Binding) && dataItem is ITableViewCellValueProvider)
				AutomationProperties.SetName(presenter, GetPropertyValue<object?>(dataItem)?.ToString() ?? string.Empty);
		}

		private static void OnCellTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not TableViewTemplateColumn column)
				return;

			column.GetOwner()?.CancelEdit(column, TableViewEditEndingReason.ColumnOperation);
			column.ResetAutoDesiredWidth();
			column.NotifyPropertyChanged(
				TableViewNotificationTarget.VisibleRows |
				TableViewNotificationTarget.ColumnLayout);
		}
	}
}
