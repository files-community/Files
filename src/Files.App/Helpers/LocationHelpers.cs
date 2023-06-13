// Copyright(c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text.Json;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.Storage;

namespace Files.App.Helpers
{
	public static class LocationHelpers
	{
		public static async Task<string> GetAddressFromCoordinatesAsync(double? Lat, double? Lon)
		{
			if (!Lat.HasValue || !Lon.HasValue)
				return null;

			if (string.IsNullOrEmpty(MapService.ServiceToken))
			{
				try
				{
					StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Resources/BingMapsKey.txt"));
					var lines = await FileIO.ReadTextAsync(file);
					using var obj = JsonDocument.Parse(lines);
					MapService.ServiceToken = obj.RootElement.GetProperty("key").GetString();
				}
				catch (Exception)
				{
					return null;
				}
			}

			BasicGeoposition location = new BasicGeoposition();
			location.Latitude = Lat.Value;
			location.Longitude = Lon.Value;
			Geopoint pointToReverseGeocode = new Geopoint(location);

			// Reverse geocode the specified geographic location.

			var result = await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode);
			return result?.Locations?.FirstOrDefault()?.DisplayName;
		}
	}
}
