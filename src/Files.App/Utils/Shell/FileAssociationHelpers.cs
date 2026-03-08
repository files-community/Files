// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Utils.Shell
{
	public static class FileAssociationHelpers
	{
		// Known identifiers used by classic and AppX registrations for Microsoft Photos.
		private static readonly string[] PhotosAssociationMarkers =
		[
			"Microsoft.Windows.Photos",
			"ms-photos",
			"AppX43hnxtbyyps62jhe9sqpdzxn1790zetc"
		];

		// PROGID and APPID are enough for modern default-app detection and cheaper than probing command/exe every time.
		private static readonly ASSOCSTR[] PhotosAssociationSources =
		[
			ASSOCSTR.ASSOCSTR_PROGID,
			ASSOCSTR.ASSOCSTR_APPID,
		];

		/// <summary>
		/// Returns true when the specified extension is currently associated with Microsoft Photos.
		/// </summary>
		/// <param name="fileExtension">File extension with or without leading dot (e.g. ".jpg" or "jpg").</param>
		public static bool IsMicrosoftPhotosDefaultAssociation(string? fileExtension)
		{
			if (string.IsNullOrWhiteSpace(fileExtension))
				return false;

			string normalizedExtension = fileExtension.StartsWith(".", StringComparison.Ordinal)
				? fileExtension
				: $".{fileExtension}";

			return IsMicrosoftPhotosDefaultAssociationCore(normalizedExtension);
		}

		private static bool IsMicrosoftPhotosDefaultAssociationCore(string fileExtension)
		{

			foreach (var source in PhotosAssociationSources)
			{
				if (TryGetAssociationString(fileExtension, source, out string value) && IsPhotosAssociationValue(value))
					return true;
			}

			return false;
		}

		private static bool IsPhotosAssociationValue(string associationValue)
			=> PhotosAssociationMarkers.Any(marker => associationValue.Contains(marker, StringComparison.OrdinalIgnoreCase));

		private static unsafe bool TryGetAssociationString(string fileExtension, ASSOCSTR association, out string value)
		{
			value = string.Empty;

			try
			{
				fixed (char* pszAssoc = fileExtension)
				{
					PWSTR pwszAssoc = new(pszAssoc);
					uint cchOutput = 0;

					// First call retrieves the required buffer length.
					_ = PInvoke.AssocQueryString(
						ASSOCF.ASSOCF_INIT_IGNOREUNKNOWN,
						association,
						pwszAssoc,
						default,
						default,
						&cchOutput);

					if (cchOutput <= 1)
						return false;

					char[] outputBuffer = new char[cchOutput];
					fixed (char* pszOutput = outputBuffer)
					{
						PWSTR pwszOutput = new(pszOutput);

						// Second call fills the buffer with the resolved association string.
						var result = PInvoke.AssocQueryString(
							ASSOCF.ASSOCF_INIT_IGNOREUNKNOWN,
							association,
							pwszAssoc,
							default,
							pwszOutput,
							&cchOutput);

						if (result.Failed)
							return false;

						value = pwszOutput.ToString();
						return !string.IsNullOrWhiteSpace(value);
					}
				}
			}
			catch
			{
				return false;
			}
		}
	}
}
