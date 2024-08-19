// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Files.App.Actions
{
	internal sealed class OpenClassicPropertiesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "OpenClassicProperties".GetLocalizedResource();

		public string Description
			=> "OpenClassicPropertiesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Properties");

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.AltShift);

		public bool IsExecutable =>
			context.PageType is not ContentPageTypes.Home &&
			!(context.PageType is ContentPageTypes.SearchResults &&
			!context.HasSelection);

		public OpenClassicPropertiesAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public unsafe Task ExecuteAsync(object? parameter = null)
		{
			if (context.HasSelection && context.SelectedItems is not null)
			{
				foreach (var item in context.SelectedItems)
				{
					SHELLEXECUTEINFOW info = default;
					info.cbSize = (uint)Marshal.SizeOf(info);

					var verb = "properties";
					fixed (char* cVerb = verb)
					{
						info.lpVerb = cVerb;
					}


					fixed (char* lpFile = item.ItemPath)
					{
						info.lpFile = lpFile;
					}

					info.nShow = 5; // SW_SHOW
					info.fMask = 0x0000000C;

					PInvoke.ShellExecuteEx(ref info);
				}
			}
			else if (context?.Folder?.ItemPath is not null)
			{
				SHELLEXECUTEINFOW info = default;
				info.cbSize = (uint)Marshal.SizeOf(info);

				var verb = "properties";
				fixed (char* cVerb = verb)
				{
					info.lpVerb = cVerb;
				}

				fixed (char* lpFile = context.Folder.ItemPath)
				{
					info.lpFile = lpFile;
				}

				info.nShow = 5; // SW_SHOW
				info.fMask = 0x0000000C;

				PInvoke.ShellExecuteEx(ref info);
			}

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
