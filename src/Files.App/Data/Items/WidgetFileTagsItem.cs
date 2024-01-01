// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.Extensions;
using Files.Shared.Utils;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetFileTagsItem : WidgetCardItem
	{
		// Fields

		private readonly IStorable _associatedStorable;

		// NOTE:
		//  A workaround for lack of MVVM-compliant navigation support.
		//  This workaround must be kept until further refactor of navigation code is completed.
		private readonly Func<string, Task> _openAction;

		// Properties

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
		public new string Path
		{
			get => _Path;
			set => SetProperty(ref _Path, value); 
		}

		public bool IsFolder => _associatedStorable is IFolder;

		public WidgetFileTagsItem(IStorable associatedStorable, Func<string, Task> openAction, IImage? icon)
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
