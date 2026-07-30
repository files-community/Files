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
				var fileResult = await FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath));
				if (fileResult.Result is not { } file)
				{
					// Could not access file, can't show any other property
					return null;
				}

				var list = await FileProperty.RetrieveAndInitializePropertiesAsync(file);

				var latitude = list.Find(x => x.Property == "System.GPS.LatitudeDecimal")?.Value as double?;
				var longitude = list.Find(x => x.Property == "System.GPS.LongitudeDecimal")?.Value as double?;
				var addressItem = list.Find(x => x.ID == "address");

				if (latitude.HasValue && longitude.HasValue && addressItem != null)
					addressItem.Value = await LocationHelpers.GetAddressFromCoordinatesAsync(latitude.Value, longitude.Value);


				return list
					.Where(fileProp => !(fileProp.Value is null && fileProp.IsReadOnly))
					.Select(fileProp => (Property: fileProp, Section: fileProp.SectionResource ?? string.Empty))
					.Where(property => property.Section.Length > 0)
					.GroupBy(property => property.Section, property => property.Property)
					.Select(group => new FilePropertySection(group) { Key = group.Key })
					.Where(section => !section.All(fileProp => fileProp.Value is null)
						|| FileProperty.IsSectionApplicableForEmpty(section.Key, item.FileExtension));
			}));

			if (queries.Any(query => query is null))
				return;

			var validQueries = queries.WhereNotNull().ToArray();
			if (validQueries.Length == 0)
				return;

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
						props
							.Where(x => x.Property == prop.Property)
							.Select(x => x.Value)
							.OfType<ulong>()
							.ForEach(value => totalDuration += value);
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
				var fileResult = await FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath));
				if (fileResult.Result is not { } file)
					return;

				files.Add(file);
			}

			var failedProperties = "";

			foreach (var group in ViewModel.PropertySections)
			{
				foreach (FileProperty prop in group)
				{
					var propertyName = prop.Property;
					if (!prop.IsReadOnly && prop.Modified && !string.IsNullOrEmpty(propertyName))
					{
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
									await file.Properties.SaveNullablePropertiesAsync(newDict);
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
				var fileResult = await FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath));
				if (fileResult.Result is not { } file)
					return;

				files.Add(file);
			}

			foreach (var group in ViewModel.PropertySections)
			{
				foreach (FileProperty prop in group)
				{
					var propertyName = prop.Property;
					if (!prop.IsReadOnly && !string.IsNullOrEmpty(propertyName))
					{
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
									await file.Properties.SaveNullablePropertiesAsync(newDict);
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
