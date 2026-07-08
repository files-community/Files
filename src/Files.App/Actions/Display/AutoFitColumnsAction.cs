// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Views.Layouts;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class AutoFitColumnsAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.AutoFitColumns.GetLocalizedResource();

		public string Description
			=> Strings.AutoFitColumnsDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Layout;

		public bool IsExecutable
			=> context.HasItem && context.ShellPage?.SlimContentPage is DetailsLayoutPage;

		public AutoFitColumnsAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage?.SlimContentPage is DetailsLayoutPage detailsPage)
				detailsPage.AutoFitColumns();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageLayoutType) or nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
