// Copyright(c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Dispatching;

namespace Files.App.ViewModels.Properties
{
	internal class CombinedFileProperties : CombinedProperties, IFileProperties
	{
		public CombinedFileProperties(
			SelectedItemsPropertiesViewModel viewModel,
			CancellationTokenSource tokenSource,
			DispatcherQueue coreDispatcher,
			List<ListedItem> listedItems,
			IShellPage instance)
			: base(viewModel, tokenSource, coreDispatcher, listedItems, instance) { }

		public async Task GetSystemFilePropertiesAsync()
		{
			var queries = await Task.WhenAll(List.AsParallel().Select(async item => {
				BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath));
				if (file is null)
				{
					// Could not access file, can't show any other property
					return null;
				}

				var list = await FileProperty.RetrieveAndInitializePropertiesAsync(file);

				list.Find(x => x.ID == "address").Value =
					await LocationHelpers.GetAddressFromCoordinatesAsync((double?)list.Find(
						x => x.Property == "System.GPS.LatitudeDecimal").Value,
						(double?)list.Find(x => x.Property == "System.GPS.LongitudeDecimal").Value);

				// Find Encoding Bitrate property and convert it to kbps
				var encodingBitrate = list.Find(x => x.Property == "System.Audio.EncodingBitrate");
				if (encodingBitrate?.Value is not null)
				{
					var sizes = new string[] { "Bps", "KBps", "MBps", "GBps" };
					var order = (int)Math.Floor(Math.Log((uint)encodingBitrate.Value, 1024));
					var readableSpeed = (uint)encodingBitrate.Value / Math.Pow(1024, order);
					encodingBitrate.Value = $"{readableSpeed:0.##} {sizes[order]}";
				}

				return list
					.Where(fileProp => !(fileProp.Value is null && fileProp.IsReadOnly))
					.GroupBy(fileProp => fileProp.SectionResource)
					.Select(group => new FilePropertySection(group) { Key = group.Key })
					.Where(section => !section.All(fileProp => fileProp.Value is null));
			}));

			if (queries.Any(query => query is null))
				return;

			// Display only the sections that all files have
			var keys = queries.Select(query => query!.Select(section => section.Key)).Aggregate((x, y) => x.Intersect(y));
			var sections = queries[0]!.Where(section => keys.Contains(section.Key)).OrderBy(group => group.Priority).ToArray();

			foreach (var group in sections)
			{
				var props = queries.SelectMany(query => query!.First(section => section.Key == group.Key));
				foreach (FileProperty prop in group)
				{
					if (props.Where(x => x.Property == prop.Property).Any(x => !Equals(x.Value, prop.Value)))
					{
						// Has multiple values
						prop.Value = prop.IsReadOnly ? "MultipleValues".GetLocalizedResource() : null;
						prop.PlaceholderText = "MultipleValues".GetLocalizedResource();
					}
				}
			}

			ViewModel.PropertySections = new ObservableCollection<FilePropertySection>(sections);
		}

		public async Task SyncPropertyChangesAsync()
		{
			var files = new List<BaseStorageFile>();
			foreach (var item in List)
			{
				BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath));

				// Couldn't access the file to save properties
				if (file is null)
					return;

				files.Add(file);
			}

			var failedProperties = "";

			foreach (var group in ViewModel.PropertySections)
			{
				foreach (FileProperty prop in group)
				{
					if (!prop.IsReadOnly && prop.Modified)
					{
						var newDict = new Dictionary<string, object>();
						newDict.Add(prop.Property, prop.Value);

						foreach (var file in files)
						{
							try
							{
								if (file.Properties is not null)
								{
									await file.Properties.SavePropertiesAsync(newDict);
								}
							}
							catch
							{
								failedProperties += $"{file.Name}: {prop.Name}\n";
							}
						}
					}
				}
			}

			if (!string.IsNullOrWhiteSpace(failedProperties))
			{
				throw new Exception($"The following properties failed to save: {failedProperties}");
			}
		}

		public async Task ClearPropertiesAsync()
		{
			var failedProperties = new List<string>();
			var files = new List<BaseStorageFile>();
			foreach (var item in List)
			{
				BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath));

				if (file is null)
					return;

				files.Add(file);
			}

			foreach (var group in ViewModel.PropertySections)
			{
				foreach (FileProperty prop in group)
				{
					if (!prop.IsReadOnly)
					{
						var newDict = new Dictionary<string, object>();
						newDict.Add(prop.Property, null);

						foreach (var file in files)
						{
							try
							{
								if (file.Properties is not null)
								{
									await file.Properties.SavePropertiesAsync(newDict);
								}
							}
							catch
							{
								failedProperties.Add(prop.Name);
							}
						}
					}
				}
			}

			_ = GetSystemFilePropertiesAsync();
		}
	}
}
