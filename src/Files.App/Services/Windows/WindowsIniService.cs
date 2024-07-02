// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsIniService"/>
	public sealed class WindowsIniService : IWindowsIniService
	{
		/// <inheritdoc/>
		public List<IniSectionDataItem> GetData(string filePath)
		{
			var iniPath = SystemIO.Path.Combine(filePath);
			if (!SystemIO.File.Exists(iniPath))
				return [];

			var lines = Enumerable.Empty<string>().ToList();

			try
			{
				lines = SystemIO.File.ReadLines(iniPath)
					.Where(line => !line.StartsWith(';') && !string.IsNullOrEmpty(line))
					.ToList();
			}
			catch (UnauthorizedAccessException)
			{
				return [];
			}

			// Get sections
			var sections = lines
				.Where(line => line.StartsWith('[') && line.EndsWith(']'));

			// Get section line indexes
			List<int> sectionLineIndexes = [];
			foreach (var section in sections)
				sectionLineIndexes.Add(lines.IndexOf(section));

			List<IniSectionDataItem> dataItems = [];

			for (int index = 0; index < sectionLineIndexes.Count; index++)
			{
				var sectionIndex = sectionLineIndexes[index];

				var count =
					index + 1 == sectionLineIndexes.Count
						? (lines.Count - 1) - sectionIndex
						: sectionLineIndexes[index + 1] - sectionIndex;

				if (count == 0)
					continue;

				var range = lines.GetRange(sectionIndex + 1, count);

				var sectionName = lines[sectionLineIndexes[index]].TrimStart('[').TrimEnd(']');

				// Read data
				var parameters = range
					// Split the lines into key and value
					.Select(line => line.Split('='))
					// Validate
					.Where(parts => parts.Length == 2)
					// Gather as dictionary
					.ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

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
