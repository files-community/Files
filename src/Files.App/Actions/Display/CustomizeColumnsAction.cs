// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Views.Layouts;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CustomizeColumnsAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.CustomizeColumns.GetLocalizedResource();

		public string Description
			=> Strings.CustomizeColumnsDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Layout;

		public bool IsExecutable
			=> context.ShellPage?.SlimContentPage is DetailsLayoutPage;

		public CustomizeColumnsAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return context.ShellPage?.SlimContentPage is DetailsLayoutPage detailsPage
				? detailsPage.CustomizeColumnsAsync()
				: Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.ShellPage) or nameof(IContentPageContext.PageLayoutType))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
