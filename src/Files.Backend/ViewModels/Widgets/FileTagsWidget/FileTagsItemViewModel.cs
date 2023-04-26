// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.Models;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using System;
using System.Threading;
using System.Threading.Tasks;
using Files.Backend.Helpers;

namespace Files.Backend.ViewModels.Widgets.FileTagsWidget
{
	public sealed partial class FileTagsItemViewModel : ObservableObject
	{
		private readonly ILocatableStorable _associatedStorable;
		private readonly Func<string, Task> _openAction;	// A workaround for lack of MVVM-compliant navigation support.
															// This workaround must be kept until further refactor of navigation code is completed

		[ObservableProperty]
		private IImageModel? _Icon;

		[ObservableProperty]
		private string _Name;

		[ObservableProperty]
		private string? _Path;

		public FileTagsItemViewModel(ILocatableStorable associatedStorable, Func<string, Task> openAction, IImageModel? icon)
		{
			_associatedStorable = associatedStorable;
			_openAction = openAction;
			_Icon = icon;
			_Name = PathHelpers.FormatName(associatedStorable.Path);
			_Path = associatedStorable.TryGetPath();
		}

		[RelayCommand]
		private Task ClickAsync(CancellationToken cancellationToken)
		{
			return _openAction(_associatedStorable.Path);
		}
	}
}
