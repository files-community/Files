using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.Models;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using System;
using System.Threading;
using System.Threading.Tasks;
using Files.Backend.Helpers;

namespace Files.App.ViewModels.Widgets.FileTags
{
	public sealed partial class FileTagsItemViewModel : WidgetCardItem
	{
		#region Fields and Properties
		private readonly ILocatableStorable _associatedStorable;

		// A workaround for lack of MVVM-compliant navigation support.
		// This workaround must be kept until further refactor of navigation code is completed.
		private readonly Func<string, Task> _openAction;

		private IImageModel? _Icon;
		public string Icon
		{
			get => _Icon;
			set => SetProperty(ref _Icon, value);
		}

		private string _Name;
		public string Name
		{
			get => _Name;
			set => SetProperty(ref _Name, value);
		}

		private string path;
		public override string Path
		{
			get => path;
			set => SetProperty(ref path, value); 
		}

		public bool IsFolder
			=> _associatedStorable is ILocatableFolder;
		#endregion

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
