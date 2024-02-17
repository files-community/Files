// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage.Extensions;
using Files.Shared.Utils;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Windows.Input;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetFileTagCardItem : ObservableObject, IWidgetCardItem
	{
		// Dependency injections

		public IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		// Properties

		public IStorable Item { get; set; }

		public bool IsFolder
			=> Item is IFolder;

		private IImage? _Icon;
		public IImage? Icon
		{
			get => _Icon;
			set => SetProperty(ref _Icon, value);
		}

		private string? _Name;
		public string? Name
		{
			get => _Name;
			set => SetProperty(ref _Name, value);
		}

		private string _Path;
		public string Path
		{
			get => _Path;
			set => SetProperty(ref _Path, value);
		}

		public BitmapImage Thumbnail
			=> throw new NotImplementedException();

		// Commands

		public ICommand ClickCommand { get; }

		public WidgetFileTagCardItem(IStorable associatedStorable, IImage? icon)
		{
			Item = associatedStorable;
			_Icon = icon;
			_Name = associatedStorable.Name;
			_Path = associatedStorable.TryGetPath();

			ClickCommand = new AsyncRelayCommand(ClickAsync);
		}

		public Task LoadCardThumbnailAsync()
		{
			throw new NotImplementedException();
		}

		private Task ClickAsync()
		{
			return NavigationHelpers.OpenPath(Item.Id, ContentPageContext.ShellPage!);
		}
	}
}
