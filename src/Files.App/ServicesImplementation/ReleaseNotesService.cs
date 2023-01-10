using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Files.App.Extensions;
using Files.Backend.Services;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Services.Store;
using WinRT.Interop;

namespace Files.App.ServicesImplementation
{
	internal sealed class ReleaseNotesService : ObservableObject, IReleaseNotesService
	{
		private string? _releaseNotes;
		public string? ReleaseNotes
		{
			get => _releaseNotes;
			private set => SetProperty(ref _releaseNotes, value);
		}

		private bool isReleaseNotesAvailable;
		public bool IsReleaseNotesAvailable
		{
			get => isReleaseNotesAvailable;
			set => SetProperty(ref isReleaseNotesAvailable, value);
		}

		public async Task DownloadReleaseNotes()
		{
			///
		}
	}
}
