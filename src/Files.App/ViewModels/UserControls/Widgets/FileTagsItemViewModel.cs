// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Files.Core.Storage;
using Files.Core.Storage.Extensions;
using Files.Shared.Utils;

namespace Files.App.ViewModels.Widgets
{
	public sealed partial class FileTagsItemViewModel : WidgetCardItem
	{
		private readonly IStorable _associatedStorable;

		// A workaround for lack of MVVM-compliant navigation support.
		// This workaround must be kept until further refactor of navigation code is completed.
		private readonly Func<string, Task> _openAction;

		[ObservableProperty]
		private IImage? _Icon;

		[ObservableProperty]
		private string _Name;

		private string _Path;
		public override string Path
		{
			get => _Path;
			set => SetProperty(ref _Path, value); 
		}

		public bool IsFolder => _associatedStorable is IFolder;

		public FileTagsItemViewModel(IStorable associatedStorable, Func<string, Task> openAction, IImage? icon)
		{
			_associatedStorable = associatedStorable;
			_openAction = openAction;
			_Icon = icon;
			_Name = associatedStorable.Name;
			_Path = associatedStorable.TryGetPath();
			Item = this;
		}

		[RelayCommand]
		private Task ClickAsync(CancellationToken cancellationToken)
		{
			return _openAction(_associatedStorable.Id);
		}
	}
}
