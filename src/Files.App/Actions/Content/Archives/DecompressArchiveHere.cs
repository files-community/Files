// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class DecompressArchiveHere : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "ExtractHere".GetLocalizedResource();

		public string Description
			=> "DecompressArchiveHereDescription".GetLocalizedResource();

		public override bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			ArchiveHelpers.CanDecompress(context.SelectedItems) &&
			UIHelpers.CanShowDialog;

		public DecompressArchiveHere()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>()

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return ArchiveHelpers.DecompressArchiveHere(context.ShellPage);
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.ZipFolder &&
				context.PageType != ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					if (IsContextPageTypeAdaptedToCommand())
						OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
