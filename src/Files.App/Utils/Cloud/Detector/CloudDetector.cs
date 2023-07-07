// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Utils.Cloud;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for cloud detection.
	/// </summary>
	public class CloudDetector : ICloudDetector
	{
		public async Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync()
		{
			var tasks = new List<Task<IEnumerable<ICloudProvider>>>();

			foreach (var detector in EnumerateDetectors())
				tasks.Add(detector.DetectCloudProvidersAsync());

			await Task.WhenAll(tasks);

			return tasks
				.SelectMany(task => task.Result)
				.OrderBy(task => task.ID.ToString())
				.ThenBy(task => task.Name)
				.Distinct();
		}

		private static IEnumerable<ICloudDetector> EnumerateDetectors()
		{
			yield return new GoogleDriveCloudDetector();
			yield return new DropBoxCloudDetector();
			yield return new BoxCloudDetector();
			yield return new GenericCloudDetector();
			yield return new SynologyDriveCloudDetector();
		}
	}
}
