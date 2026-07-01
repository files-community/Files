// Copyright(c) Files Community
// Licensed under the MIT License.

using Windows.Graphics.Imaging;

namespace Files.App.Helpers
{
	public static class LocationHelpers
	{
		// (XMP namespace:tag, IPTC IIM tag id) for the four location fields,
		// in display order: Sublocation, City, Province/State, Country.
		private static readonly (string Xmp, ushort Iim)[] LocationFields =
		{
			("Iptc4xmpCore:Location", 92),
			("photoshop:City",        90),
			("photoshop:State",       95),
			("photoshop:Country",     101),
		};

		/// <summary>
		/// Reads IPTC location fields (Sublocation, City, Province/State, Country) embedded in an
		/// image file and returns them joined as a comma-separated string, or null if none are
		/// present. XMP fields are preferred over the legacy IPTC IIM fields.
		/// </summary>
		public static async Task<string> GetAddressFromImageMetadataAsync(BaseStorageFile file)
		{
			try
			{
				using var stream = await file.OpenReadAsync();
				var decoder = await BitmapDecoder.CreateAsync(stream);

				var parts = new List<string>();
				foreach (var (xmp, iim) in LocationFields)
				{
					var value = await TryReadAsync(decoder,
						$"/xmp/{xmp}",
						$"/ifd/xmp/{xmp}",
						$"/app13/irb/8bimiptc/iptc/{{ushort={iim}}}",
						$"/ifd/iptc/{{ushort={iim}}}");
					if (!string.IsNullOrWhiteSpace(value))
						parts.Add(value);
				}
				return parts.Count == 0 ? null : string.Join(", ", parts);
			}
			catch (Exception)
			{
				// OpenReadAsync / BitmapDecoder.CreateAsync throw for locked, corrupt, or
				// unsupported-format files; treat as "no IPTC location available".
				return null;
			}
		}

		private static async Task<string> TryReadAsync(BitmapDecoder decoder, params string[] queries)
		{
			foreach (var query in queries)
			{
				try
				{
					var props = await decoder.BitmapProperties.GetPropertiesAsync(new[] { query });
					// XMP returns string, raw IPTC IIM returns byte[] (UTF-8 covers both ASCII
					// and the modern IIM CodedCharacterSet=ESC%G case).
					var text = props.TryGetValue(query, out var v) ? v?.Value switch
					{
						string s => s,
						byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
						_ => null,
					} : null;
					if (!string.IsNullOrWhiteSpace(text))
						return text;
				}
				catch (Exception)
				{
					// GetPropertiesAsync throws WINCODEC_ERR_PROPERTYNOTFOUND when the path
					// isn't present in this file; try the next candidate.
				}
			}
			return null;
		}
	}
}
