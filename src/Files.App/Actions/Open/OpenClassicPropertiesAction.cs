// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenClassicPropertiesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.OpenClassicProperties.GetLocalizedResource();

		public string Description
			=> Strings.OpenClassicPropertiesDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Properties");

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.AltShift);

		public bool IsExecutable =>
			context.PageType is not ContentPageTypes.Home &&
			(context.HasSelection && context.SelectedItems.Count == 1 ||
			!context.HasSelection && context.PageType is not ContentPageTypes.SearchResults);

		public OpenClassicPropertiesAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.HasSelection && context?.SelectedItem?.ItemPath is not null)
				ExecuteShellCommand(context.SelectedItem.ItemPath);
			else if (context?.Folder?.ItemPath is not null)
				ExecuteShellCommand(context.Folder.ItemPath);

			return Task.CompletedTask;
		}

		private unsafe void ExecuteShellCommand(string itemPath)
		{
			SHELLEXECUTEINFOW info = default;
			info.cbSize = (uint)Marshal.SizeOf(info);
			info.nShow = 5; // SW_SHOW
			info.fMask = 0x0000000C; // SEE_MASK_INVOKEIDLIST

			// Prevent Main Window from coming to from when sidebar menu is opened.

			MainWindow.Instance.SetCanWindowToFront(false);

			fixed (char* cVerb = "properties", lpFile = itemPath)
			{
				info.lpVerb = cVerb;
				info.lpFile = lpFile;

				try
				{
					PInvoke.ShellExecuteEx(ref info);
				}
				finally
				{
					MainWindow.Instance.SetCanWindowToFront(true);
				}
			}
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
