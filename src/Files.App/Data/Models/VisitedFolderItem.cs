// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Text.Json.Serialization;
using Files.App.Controls;

namespace Files.App.Data.Models
{
	public sealed class VisitedFolderItem : IOmnibarTextMemberPathProvider
	{
		[JsonPropertyName("path")]
		public string FolderPath { get; set; } = string.Empty;

		[JsonPropertyName("name")]
		public string FolderName { get; set; } = string.Empty;

		[JsonPropertyName("count")]
		public int VisitCount { get; set; } = 1;

		[JsonPropertyName("last_visited")]
		public DateTime LastVisitedUtc { get; set; } = DateTime.UtcNow;

		[JsonIgnore]
		public double FrecencyScore => CalculateFrecency(VisitCount, LastVisitedUtc);

		public static double CalculateFrecency(int count, DateTime lastVisited)
		{
			var deltaHours = (DateTime.UtcNow - lastVisited).TotalHours;

			// Recency weighting (frecency algorithm)
			double weight = deltaHours switch
			{
				< 4 => 100.0,
				< 24 => 80.0,
				< 72 => 60.0,
				< 168 => 40.0, // 1 week
				< 720 => 20.0, // 1 month
				_ => 10.0
			};

			return count * weight;
		}

		public string? GetTextMemberPath(string textMemberPath)
		{
			return textMemberPath switch
			{
				nameof(FolderPath) => FolderPath,
				nameof(FolderName) => FolderName,
				_ => FolderPath
			};
		}
	}
}
