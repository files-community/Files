// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class RemoveTagsAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.RemoveTags.GetLocalizedResource();

		public string Description
			=> Strings.RemoveTagsDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public override bool IsExecutable =>
			base.IsExecutable &&
			context.HasSelection &&
			context.PageType is not ContentPageTypes.RecycleBin and not ContentPageTypes.Home and not ContentPageTypes.None &&
			context.SelectedItems.Any(item => item.FileTags is { Length: > 0 });

		public RemoveTagsAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (await FileTagsHelper.RemoveTagsAsync(context.SelectedItems) &&
				context.ShellPage is not null)
			{
				await context.ShellPage.ShellViewModel.RefreshTagGroups();
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
