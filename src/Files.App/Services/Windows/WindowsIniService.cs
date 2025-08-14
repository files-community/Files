// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsIniService"/>
	public sealed class WindowsIniService : IWindowsIniService
	{
		/// <inheritdoc/>
		public List<IniSectionDataItem> GetData(string filePath)
		{
			if (!SystemIO.File.Exists(filePath))
				return [];

			var lines = Enumerable.Empty<string>().ToList();

			try
			{
				lines = SystemIO.File.ReadLines(filePath)
					.Where(line => !line.StartsWith(';') && !string.IsNullOrEmpty(line))
					.ToList();
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException || ex is SystemIO.FileNotFoundException || ex is SystemIO.IOException)
			{
				return [];
			}

			// Get section line indexes directly to handle duplicate section names
			List<int> sectionLineIndexes = [];
			for (int i = 0; i < lines.Count; i++)
			{
				if (lines[i].StartsWith('[') && lines[i].EndsWith(']'))
					sectionLineIndexes.Add(i);
			}

			List<IniSectionDataItem> dataItems = [];

			for (int index = 0; index < sectionLineIndexes.Count; index++)
			{
				var sectionIndex = sectionLineIndexes[index];

				var count =
					index + 1 == sectionLineIndexes.Count
						? lines.Count - sectionIndex - 1
						: sectionLineIndexes[index + 1] - sectionIndex - 1;

				if (count <= 0)
					continue;

				var range = lines.GetRange(sectionIndex + 1, count);

				var sectionName = lines[sectionLineIndexes[index]].TrimStart('[').TrimEnd(']');

				// Read data
				var parameters = range
					// Split the lines into key and value
					.Select(line => line.Split('='))
					// Validate
					.Where(parts => parts.Length == 2)
					// Group by key to avoid duplicates
					.GroupBy(parts => parts[0].Trim())
					// Gather as dictionary
					.ToDictionary(partsGroup => partsGroup.Key, partsGroup => partsGroup.Last()[1].Trim());

				dataItems.Add(new()
				{
					SectionName = sectionName,
					Parameters = parameters,
				});
			}

			return dataItems;
		}
	}
}
