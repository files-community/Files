// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CopyItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Copy".GetLocalizedResource();

		public string Description => "CopyItemDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconCopy");

		public HotKey HotKey { get; } = new(Keys.C, KeyModifiers.Ctrl);

		public bool IsExecutable => context.HasSelection;

		public CopyItemAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (context.ShellPage is not null)
				return UIFilesystemHelpers.CopyItem(context.ShellPage);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
