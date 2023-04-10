// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Models;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using Files.Backend.Helpers;
using Files.App.UserControls.Widgets;

namespace Files.App.ViewModels.Widgets
{
	public sealed partial class FileTagsItemViewModel : WidgetCardItem
	{
		private readonly ILocatableStorable _associatedStorable;
		private readonly Func<string, Task> _openAction;	// A workaround for lack of MVVM-compliant navigation support.
															// This workaround must be kept until further refactor of navigation code is completed

		[ObservableProperty]
		private IImageModel? _Icon;

		[ObservableProperty]
		private string _Name;

		private string path;
		public override string Path
		{
			get => path;
			set => SetProperty(ref path, value); 
		}

		public bool IsFolder => _associatedStorable is ILocatableFolder;

		public FileTagsItemViewModel(ILocatableStorable associatedStorable, Func<string, Task> openAction, IImageModel? icon)
		{
			_associatedStorable = associatedStorable;
			_openAction = openAction;
			_Icon = icon;
			_Name = PathHelpers.FormatName(associatedStorable.Path);
			Path = associatedStorable.TryGetPath();
			Item = this;
		}

		[RelayCommand]
		private Task ClickAsync(CancellationToken cancellationToken)
		{
			return _openAction(_associatedStorable.Path);
		}
	}
}
