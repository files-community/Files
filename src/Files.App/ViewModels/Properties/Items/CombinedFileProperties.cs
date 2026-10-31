// Copyright(c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Dispatching;

namespace Files.App.ViewModels.Properties
{
	internal sealed class CombinedFileProperties : CombinedProperties, IFileProperties
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
			var queries = await Task.WhenAll(List.AsParallel().Select(async item =>
			{
				var itemPath = item.GetRequiredPath();
				var fileResult = await FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFileFromPathAsync(itemPath));
				if (fileResult.Result is not { } file)
				{
					// Could not access file, can't show any other property
					return null;
				}

				var list = await FileProperty.RetrieveAndInitializePropertiesAsync(file);

				var latitudeProperty = list.Find(x => x.Property == "System.GPS.LatitudeDecimal")
					?? throw new InvalidOperationException("The file property definitions do not contain the latitude property.");
				var longitudeProperty = list.Find(x => x.Property == "System.GPS.LongitudeDecimal")
					?? throw new InvalidOperationException("The file property definitions do not contain the longitude property.");
				var addressItem = list.Find(x => x.ID == "address")
					?? throw new InvalidOperationException("The file property definitions do not contain the address property.");

				addressItem.Value = await LocationHelpers.GetAddressFromCoordinatesAsync(
					(double?)latitudeProperty.Value,
					(double?)longitudeProperty.Value);


				return list
					.Where(fileProp => !(fileProp.Value is null && fileProp.IsReadOnly))
					.GroupBy(fileProp => fileProp.SectionResource!)
					.Select(group => new FilePropertySection(group) { Key = group.Key })
					.Where(section => !section.All(fileProp => fileProp.Value is null)
						|| FileProperty.IsSectionApplicableForEmpty(section.Key, item.FileExtension));
			}));

			if (queries.Any(query => query is null))
				return;

			var validQueries = queries.WhereNotNull().ToArray();
			if (validQueries.Length == 0)
				throw new InvalidOperationException("No selected files were available for property aggregation.");

			// Display only the sections that all files have
			var keys = validQueries.Select(query => query.Select(section => section.Key)).Aggregate((x, y) => x.Intersect(y));
			var sections = validQueries[0].Where(section => keys.Contains(section.Key)).OrderBy(group => group.Priority).ToArray();

			foreach (var group in sections)
			{
				var props = validQueries.SelectMany(query => query.First(section => section.Key == group.Key));
				foreach (FileProperty prop in group)
				{
					if (prop.Property == "System.Media.Duration")
					{
						ulong totalDuration = 0;
						props.Where(x => x.Property == prop.Property).ForEach(x =>
							totalDuration += (ulong)(x.Value
								?? throw new InvalidOperationException("A media duration property does not have a value.")));
						prop.Value = totalDuration;
					}
					else if (props.Where(x => x.Property == prop.Property).Any(x => !Equals(x.Value, prop.Value)))
					{
						// Has multiple values
						prop.Value = prop.IsReadOnly ? Strings.MultipleValues.GetLocalizedResource() : null;
						prop.PlaceholderText = Strings.MultipleValues.GetLocalizedResource();
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
				// Couldn't access the file to save properties
				var itemPath = item.GetRequiredPath();
				var fileResult = await FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFileFromPathAsync(itemPath));
				if (fileResult.Result is not { } file)
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
						var propertyName = prop.Property
							?? throw new InvalidOperationException("An editable file property does not have an identifier.");
						var newDict = new Dictionary<string, object?>
						{
							{ propertyName, prop.Value }
						};

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
				var itemPath = item.GetRequiredPath();
				var fileResult = await FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFileFromPathAsync(itemPath));
				if (fileResult.Result is not { } file)
					return;

				files.Add(file);
			}

			foreach (var group in ViewModel.PropertySections)
			{
				foreach (FileProperty prop in group)
				{
					if (!prop.IsReadOnly)
					{
						var propertyName = prop.Property
							?? throw new InvalidOperationException("An editable file property does not have an identifier.");
						var newDict = new Dictionary<string, object?>
						{
							{ propertyName, null }
						};

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
