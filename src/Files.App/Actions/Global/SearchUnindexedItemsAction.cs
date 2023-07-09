// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class SearchUnindexedItemsAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "SearchUnindexedItemsButton/Content".GetLocalizedResource();

		public string Description
			=> "SearchUnindexedItemsDescription".GetLocalizedResource();

		public bool IsExecutable
			=> context.ShowSearchUnindexedItemsMessage;

		public SearchUnindexedItemsAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage!.SubmitSearch(context.ShellPage!.InstanceViewModel.CurrentSearchQuery, true);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShowSearchUnindexedItemsMessage):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
