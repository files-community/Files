using Files.Core.Storage;
using Files.Core.Storage.LocatableStorage;
using TagLib;

namespace Files.App.Helpers
{
	internal class MediaFileHelper
	{
		/// <summary>
		/// Changes the album cover for a given <paramref name="storable"/>.
		/// </summary>
		/// <param name="storable">The storable object to change the album cover.</param>
		/// <param name="albumCover">The album cover to use.</param>
		public static bool ChangeAlbumCover(IStorable storable, Picture albumCover)
		{
			if (storable is not ILocatableStorable locatableStorable)
				return false;

			File mediaFile = File.Create(locatableStorable.Path);
			IPicture[] pictures = [albumCover];
			mediaFile.Tag.Pictures = pictures;
			mediaFile.Save();
			mediaFile.Dispose();

			return true;
		}
	}
}