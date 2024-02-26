// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage.Extensions;
using Files.Shared.Utils;
using System.Windows.Input;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetFileTagCardItem : WidgetCardItem
	{
		// Fields

		private readonly IStorable _associatedStorable;

		// A workaround for lack of MVVM-compliant navigation support.
		// This workaround must be kept until further refactor of navigation code is completed.
		private readonly Func<string, Task> _openAction;

		// Properties

		public bool IsFolder
			=> _associatedStorable is IFolder;

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
		public override string Path
		{
			get => _Path;
			set => SetProperty(ref _Path, value);
		}

		// Commands

		public ICommand ClickCommand { get; }

		public WidgetFileTagCardItem(IStorable associatedStorable, Func<string, Task> openAction, IImage? icon)
		{
			_associatedStorable = associatedStorable;
			_openAction = openAction;
			_Icon = icon;
			_Name = associatedStorable.Name;
			_Path = associatedStorable.TryGetPath();
			Item = this;

			ClickCommand = new AsyncRelayCommand(ClickAsync);
		}

		private Task ClickAsync()
		{
			return _openAction(_associatedStorable.Id);
		}
	}
}
